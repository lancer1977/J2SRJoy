
using System.Windows;
using System.Windows.Threading;
using Joystick.Host;
using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRVirtualPad;

public partial class MainWindow : Window
{
    private HubConnection? _client;
    private VirtualGamepadService? _pad;
    private readonly CancellationTokenSource _cts = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void ConnectBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ConnectBtn.IsEnabled = false;
            StatusText.Text = "Status: Starting…";

            _pad ??= new VirtualGamepadService();
            await _pad.EnsureConnectedAsync();
            var builder = new HubConnectionBuilder()
                .WithAutomaticReconnect()
                .WithUrl(HubUrlBox.Text).Build();
            await builder.StartAsync(_cts.Token);
            DisconnectBtn.IsEnabled = true;
            StatusText.Text = "Status: Connected";
        }
        catch (Exception ex)
        {
            ConnectBtn.IsEnabled = true;
            StatusText.Text = $"Status: Error - {ex.Message}";
        }
    }

    private async void DisconnectBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DisconnectBtn.IsEnabled = false;
            _client?.DisposeAsync();
            _pad?.Dispose();

            ConnectBtn.IsEnabled = true;
            StatusText.Text = "Status: Disconnected";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Status: Error - {ex.Message}";
        }
    }

    private void OnStatusChanged(string s)
    {
        Dispatcher.Invoke(() => StatusText.Text = $"Status: {s}");
    }

    private void OnInboundMessage(JoystickCommand cmd)
    {
        Dispatcher.Invoke(() => LastMsgText.Text = $"Last message: {cmd.Up} {cmd.X} ");

        // Drive pad
        _pad!.Apply(cmd);
    }

    protected override void OnClosed(EventArgs e)
    {
        _cts.Cancel();
        _client?.DisposeAsync();
        _pad?.Dispose();
        base.OnClosed(e);
    }
}
