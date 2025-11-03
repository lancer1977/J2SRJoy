using Joystick.Core;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.Threading.Channels;
using System.Windows;

namespace SignalRVirtualPad
{
    public partial class MainWindow : Window
    {
        private HubConnection? _client;
        private VirtualGamepadService? _pad;

        private CancellationTokenSource? _cts;                 // (Re)created on connect
        private IDisposable? _updateSub;                        // Hub handler subscription
        private Task? _pumpTask;                                // Background pump
        private readonly Channel<JoystickCommand> _inbound =    // Burst buffer
            Channel.CreateUnbounded<JoystickCommand>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        private volatile bool _running;                         // Pump state
        private DateTime _lastSeen = DateTime.MinValue;         // For idle -> neutral
        private bool _neutralApplied;                           // Avoid spamming neutral

        public MainWindow()
        {
            InitializeComponent();
        }

        // --- UI Handlers -----------------------------------------------------

        private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ConnectBtn.IsEnabled = false;
                SetStatus("Starting…");

                // Fresh CTS per connection attempt
                _cts?.Cancel();
                _cts?.Dispose();
                _cts = new CancellationTokenSource();
                var ct = _cts.Token;

                // Virtual pad
                _pad ??= new VirtualGamepadService()
                { 

                     
                };
                
                await _pad.EnsureConnectedAsync().ConfigureAwait(false);

                // Hub
                _client = new HubConnectionBuilder()
                    .WithAutomaticReconnect()
                    .WithUrl(HubUrlBox.Text)
                    .Build();

                // Register hub callbacks BEFORE StartAsync
                _updateSub = _client.On<JoystickSample[]>("JoystickUpdate", async samples =>
                {
                    if (samples == null || samples.Length == 0) return;
                    foreach (var s in samples)
                    {
                        var cmd = ToCommand(s);
                        _inbound.Writer.TryWrite(cmd);
                        _lastSeen = DateTime.UtcNow;
                        _neutralApplied = false; // we have activity again
                    }
                    await Task.CompletedTask;
                });

                _client.Reconnecting += error =>
                {
                    SetStatus("Reconnecting…");
                    return Task.CompletedTask;
                };
                _client.Reconnected += connectionId =>
                {
                    SetStatus("Connected (reconnected)");
                    return Task.CompletedTask;
                };
                _client.Closed += async error =>
                {
                    SetStatus("Disconnected");
                    await Task.CompletedTask;
                };

                await _client.StartAsync(ct).ConfigureAwait(false);

                // If your hub requires registration
                await _client.InvokeAsync("RegisterJoystick").ConfigureAwait(false);

                // Start background pump at a steady cadence
                StartPadPump(ct);
                Dispatcher.Invoke(() => { DisconnectBtn.IsEnabled = true; });
                SetStatus("Connected");
            }
            catch (OperationCanceledException)
            {
                SetStatus("Canceled");
                Dispatcher.Invoke(() => { ConnectBtn.IsEnabled = true; });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    ConnectBtn.IsEnabled = true;
                });
                
                SetStatus($"Error - {ex.Message}");
            }
        }

        private async void DisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisconnectBtn.IsEnabled = false;

                // Stop pump/hub and cleanup
                _updateSub?.Dispose();
                _updateSub = null;

                _cts?.Cancel();
                _cts?.Dispose();
                _cts = null;

                if (_client is not null)
                {
                    await _client.DisposeAsync();
                    _client = null;
                }

                _pad?.Dispose();
                _pad = null;

                _running = false;
                ConnectBtn.IsEnabled = true;
                SetStatus("Disconnected");
            }
            catch (Exception ex)
            {
                SetStatus($"Error - {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                _updateSub?.Dispose();
                _cts?.Cancel();

                _client?.DisposeAsync().AsTask().GetAwaiter().GetResult();
                _pad?.Dispose();
            }
            catch { /* swallow during shutdown */ }
            finally
            {
                _running = false;
                _cts?.Dispose();
                base.OnClosed(e);
            }
        }

        // --- Background Pump --------------------------------------------------

        private void StartPadPump(CancellationToken ct)
        {
            if (_running) return;
            _running = true;

            _pumpTask = Task.Run(async () =>
            {
                var tick = new PeriodicTimer(TimeSpan.FromMilliseconds(16)); // ~60 Hz
                try
                {
                    while (await tick.WaitForNextTickAsync(ct).ConfigureAwait(false))
                    {
                        // Drain bursts; keep only latest
                        JoystickCommand? latest = null;
                        while (_inbound.Reader.TryRead(out var cmd))
                            latest = cmd;

                        // Idle watchdog -> neutral
                        var idle = (DateTime.UtcNow - _lastSeen) > TimeSpan.FromMilliseconds(150);
                        if (idle && !_neutralApplied)
                        {
                            try
                            {
                                _pad?.Neutral();
                                _neutralApplied = true;
                                SetLastMessage("Neutral (idle timeout)");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Neutral error: {ex.Message}");
                            }
                        }

                        // Apply most recent command at our cadence
                        if (latest.HasValue)
                        {
                            try
                            {
                                _pad?.Apply(latest.Value);
                                SetLastMessage(latest.Value.ToString());
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Apply error: {ex.Message}");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // normal shutdown
                }
                finally
                {
                    tick.Dispose();
                }
            }, ct);
        }

        // --- Mapping & Helpers ------------------------------------------------

        private JoystickCommand ToCommand(JoystickSample last)
        {
            // Defensive guards
            if (last is null || last.Buttons is null || last.Buttons.Length == 0)
                return default;

            var btns = last.Buttons;

            // Small linear scan is fine here (tiny arrays). If needed, swap to HashSet<int>.
            static bool Has(int[] arr, int id)
            {
                for (int i = 0; i < arr.Length; i++)
                    if (arr[i] == id) return true;
                return false;
            }

            // Typical Gamepad API indices:
            // 0 = A / South, 1 = B / East (or Y depending on layout),
            // 12 = DPadUp, 13 = DPadDown, 14 = DPadLeft, 15 = DPadRight
            var cmd = new JoystickCommand
            {
                X = Has(btns, 0),
                Y = Has(btns, 1),
                Up = Has(btns, 12),
                Down = Has(btns, 13),
                Left = Has(btns, 14),
                Right = Has(btns, 15),
            };

            // Debug print once per sample batch line
            Debug.WriteLine($"Buttons: {string.Join(",", btns)} -> {cmd}");
            return cmd;
        }

        private void SetStatus(string text)
            => Dispatcher.Invoke(() => StatusText.Text = $"Status: {text}");

        private void SetLastMessage(string text)
            => Dispatcher.Invoke(() => LastMsgText.Text = $"Last message: {text}");
    }
}
