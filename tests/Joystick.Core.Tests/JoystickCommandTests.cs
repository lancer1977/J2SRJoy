using System.Text.Json;
using Joystick.Core;
using Xunit;

namespace Joystick.Core.Tests;

public class JoystickCommandTests
{
    [Fact]
    public void TestInfrastructure_CanInstantiateCoreType()
    {
        // Verifies that JoystickCommand can be constructed - tests infrastructure setup
        var command = new JoystickCommand(false, false, false, false, false, false);

        // If we get here, construction succeeded - the type is instantiable
        Assert.True(true);
    }

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var command = new JoystickCommand(
            Up: true,
            Down: false,
            Left: true,
            Right: false,
            X: true,
            Y: false);

        Assert.True(command.Up);
        Assert.False(command.Down);
        Assert.True(command.Left);
        Assert.False(command.Right);
        Assert.True(command.X);
        Assert.False(command.Y);
    }

    [Fact]
    public void Constructor_AllFalse_IsValidEdgeCase()
    {
        var command = new JoystickCommand(false, false, false, false, false, false);

        Assert.False(command.Up);
        Assert.False(command.Down);
        Assert.False(command.Left);
        Assert.False(command.Right);
        Assert.False(command.X);
        Assert.False(command.Y);
    }

    [Fact]
    public void Constructor_AllTrue_IsValidEdgeCase()
    {
        var command = new JoystickCommand(true, true, true, true, true, true);

        Assert.True(command.Up);
        Assert.True(command.Down);
        Assert.True(command.Left);
        Assert.True(command.Right);
        Assert.True(command.X);
        Assert.True(command.Y);
    }

    [Fact]
    public void JsonSerialization_RoundTrip_PreservesProperties()
    {
        var original = new JoystickCommand(
            Up: true,
            Down: false,
            Left: false,
            Right: true,
            X: true,
            Y: false);

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<JoystickCommand>(json);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void JsonSerialization_ContainsExpectedPropertyNames()
    {
        var command = new JoystickCommand(true, false, true, false, true, false);

        var json = JsonSerializer.Serialize(command);

        Assert.Contains("\"Up\"", json);
        Assert.Contains("\"Down\"", json);
        Assert.Contains("\"Left\"", json);
        Assert.Contains("\"Right\"", json);
        Assert.Contains("\"X\"", json);
        Assert.Contains("\"Y\"", json);
    }
}