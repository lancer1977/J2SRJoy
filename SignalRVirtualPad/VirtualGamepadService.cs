using Joystick.Host;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace SignalRVirtualPad
{
    public sealed class VirtualGamepadService : IDisposable
    {
        private ViGEmClient? _client;
        private IXbox360Controller? _x360;
        private readonly Timer _autoNeutralTimer;

        // Auto-neutral if no update arrives within this window.
        private readonly TimeSpan _neutralAfter = TimeSpan.FromMilliseconds(250);

        public VirtualGamepadService()
        {
            _autoNeutralTimer = new Timer(_ => Neutral(), null, Timeout.Infinite, Timeout.Infinite);
        }

        public Task EnsureConnectedAsync()
        {
            if (_x360 is not null) return Task.CompletedTask;

            _client = new ViGEmClient();
            _x360 = _client.CreateXbox360Controller();
            _x360.AutoSubmitReport = true;
            _x360.Connect();
            Neutral();
            return Task.CompletedTask;
        }

        public void Apply(JoystickCommand cmd)
        {
            if (_x360 is null) return;

            // Kick the neutral timer
            _autoNeutralTimer.Change(_neutralAfter, Timeout.InfiniteTimeSpan);
            _x360.SetButtonState(Xbox360Button.Up, cmd.Up);
            _x360.SetButtonState(Xbox360Button.Down, cmd.Down);
            _x360.SetButtonState(Xbox360Button.Left, cmd.Left);
            _x360.SetButtonState(Xbox360Button.Right, cmd.Right);
            _x360.SetButtonState(Xbox360Button.X, cmd.X);
            _x360.SetButtonState(Xbox360Button.Y, cmd.Y);
        }

        public void Neutral()
        {
            if (_x360 is null) return;
            _x360.SetAxisValue(Xbox360Axis.LeftThumbX, 0);
            _x360.SetAxisValue(Xbox360Axis.LeftThumbY, 0);
            _x360.SetButtonState(Xbox360Button.Up, false);
            _x360.SetButtonState(Xbox360Button.Down, false);
            _x360.SetButtonState(Xbox360Button.Left, false);
            _x360.SetButtonState(Xbox360Button.Right, false);
            _x360.SetButtonState(Xbox360Button.X, false);
            _x360.SetButtonState(Xbox360Button.Y, false);
        }

        private static (short X, short Y) DirectionToThumb(string? direction, double strength)
        {
            // XInput stick range: -32768..32767
            short Scale(double v) => (short)Math.Clamp(v * 32767.0, -32767, 32767);

            strength = Math.Clamp(strength, 0.0, 1.0);
            double s = strength;

            return direction?.ToLowerInvariant() switch
            {
                "up" => (0, Scale(+s)),
                "down" => (0, Scale(-s)),
                "left" => (Scale(-s), 0),
                "right" => (Scale(+s), 0),
                "upleft" or "up-left" => (Scale(-s * 0.707), Scale(+s * 0.707)),
                "upright" or "up-right" => (Scale(+s * 0.707), Scale(+s * 0.707)),
                "downleft" or "down-left" => (Scale(-s * 0.707), Scale(-s * 0.707)),
                "downright" or "down-right" => (Scale(+s * 0.707), Scale(-s * 0.707)),
                _ => (0, 0) // Neutral
            };
        }

 

        public void Dispose()
        {
            _autoNeutralTimer.Dispose();
            _x360?.Disconnect();
            //_x360?.Dispose();
            _client?.Dispose();
        }
    }
}
