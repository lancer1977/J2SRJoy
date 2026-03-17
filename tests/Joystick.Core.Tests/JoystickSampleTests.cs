using System.Text.Json;
using Joystick.Core;
using Xunit;

namespace Joystick.Core.Tests;

public class JoystickSampleTests
{
    [Fact]
    public void Constructor_Defaults_InitializeExpectedValues()
    {
        var sample = new JoystickSample();

        Assert.Equal(string.Empty, sample.GamepadId);
        Assert.Equal(Directions.None, sample.Direction);
        Assert.NotNull(sample.Buttons);
        Assert.NotNull(sample.Axes);
        Assert.Empty(sample.Buttons);
        Assert.Empty(sample.Axes);
    }

    [Fact]
    public void Properties_CanBeAssignedAndReadBack()
    {
        var sample = new JoystickSample
        {
            GamepadId = "pad-1",
            Direction = Directions.UpRight,
            Buttons = [1, 4, 7],
            Axes = [0.25f, -0.75f]
        };

        Assert.Equal("pad-1", sample.GamepadId);
        Assert.Equal(Directions.UpRight, sample.Direction);
        Assert.Equal([1, 4, 7], sample.Buttons);
        Assert.Equal([0.25f, -0.75f], sample.Axes, new FloatArrayEqualityComparer());
    }

    [Fact]
    public void Properties_AcceptEmptyCollections_AsEdgeCase()
    {
        var sample = new JoystickSample
        {
            GamepadId = "edge-case",
            Direction = Directions.None,
            Buttons = [],
            Axes = []
        };

        Assert.Empty(sample.Buttons);
        Assert.Empty(sample.Axes);
    }

    [Fact]
    public void JsonSerialization_UsesExpectedPropertyNames()
    {
        var sample = new JoystickSample
        {
            GamepadId = "json-pad",
            Direction = Directions.DownLeft,
            Buttons = [2, 3],
            Axes = [-1f, 1f]
        };

        var json = JsonSerializer.Serialize(sample);

        Assert.Contains("\"gamepadId\"", json);
        Assert.Contains("\"direction\"", json);
        Assert.Contains("\"buttons\"", json);
        Assert.Contains("\"axes\"", json);
    }

    [Fact]
    public void JsonSerialization_RoundTrip_PreservesAllValues()
    {
        var original = new JoystickSample
        {
            GamepadId = "roundtrip-pad",
            Direction = Directions.DownRight,
            Buttons = [0, 5],
            Axes = [1f, -0.5f, 0.125f]
        };

        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<JoystickSample>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.GamepadId, restored!.GamepadId);
        Assert.Equal(original.Direction, restored.Direction);
        Assert.Equal(original.Buttons, restored.Buttons);
        Assert.Equal(original.Axes, restored.Axes, new FloatArrayEqualityComparer());
    }

    private class FloatArrayEqualityComparer : IEqualityComparer<float[]>
    {
        public bool Equals(float[]? x, float[]? y)
        {
            if (x == null && y == null) return true;
            if (x == null || y == null) return false;
            if (x.Length != y.Length) return false;
            return x.Zip(y).All(p => Math.Abs(p.First - p.Second) < 0.0001f);
        }

        public int GetHashCode(float[] obj) => obj.GetHashCode();
    }
}