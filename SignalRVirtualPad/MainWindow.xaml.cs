
using Joystick.Core;
using Microsoft.AspNetCore.SignalR.Client;
using System.Data.Common;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

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
            _client = new HubConnectionBuilder()
                .WithAutomaticReconnect()
                .WithUrl(HubUrlBox.Text).Build();
            await _client.StartAsync(_cts.Token);
            await _client.InvokeAsync("RegisterJoystick");
            _client.On<JoystickSample[]>("JoystickUpdate", OnJoystickUpdate);
            DisconnectBtn.IsEnabled = true;
            StatusText.Text = "Status: Connected";
        }
        catch (Exception ex)
        {
            ConnectBtn.IsEnabled = true;
            StatusText.Text = $"Status: Error - {ex.Message}";
        }
    }
    private JoystickCommand ToCommand(JoystickSample[] arg)
    {
        var command = new JoystickCommand();

        var last = arg.LastOrDefault();
        if (last == null)
        {
            Debug.WriteLine("No samples");
            _pad?.Neutral();
            return command;
        }
        command.Up = last.Direction == Directions.Up;
        command.Down = last.Direction == Directions.Down;
        command.Left = last.Direction == Directions.Left;
        command.Right = last.Direction == Directions.Right;
        command.X = last.Buttons.Any(x=>x == 0);
        command.Y = last.Buttons.Any(x => x == 0);
        return command;
    }
    private Task OnJoystickUpdate(JoystickSample[] arg)
    {
        var command = ToCommand(arg);
        OnInboundMessage(command);
        return Task.CompletedTask;
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

    private void OnInboundMessage(JoystickCommand? inboundCommand)
    {
        if (inboundCommand == null)
        {
            Dispatcher.Invoke(() => LastMsgText.Text = $"");
        }
        else
        {
            JoystickCommand cmd = inboundCommand.Value;
            Dispatcher.Invoke(() => LastMsgText.Text = $"Last message: {cmd.Up} {cmd.X} ");

            // Drive pad
            _pad!.Apply(cmd);
            
        }
  
    }

    protected override void OnClosed(EventArgs e)
    {
        _cts.Cancel();
        _client?.DisposeAsync();
        _pad?.Dispose();
        base.OnClosed(e);
    }
}
