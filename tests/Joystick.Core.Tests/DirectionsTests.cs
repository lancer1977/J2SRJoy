using Joystick.Core;
using Xunit;

namespace Joystick.Core.Tests;

public class DirectionsTests
{
    [Theory]
    [InlineData(Directions.None)]
    [InlineData(Directions.Up)]
    [InlineData(Directions.Down)]
    [InlineData(Directions.Left)]
    [InlineData(Directions.Right)]
    [InlineData(Directions.UpLeft)]
    [InlineData(Directions.UpRight)]
    [InlineData(Directions.DownLeft)]
    [InlineData(Directions.DownRight)]
    public void AllDirections_AreValidEnumValues(Directions direction)
    {
        // Verify all enum values are defined and accessible
        var value = (int)direction;
        Assert.True(value >= 0 && value <= 8, $"Direction {direction} has invalid value {value}");
    }

    [Fact]
    public void Enum_HasExpectedNumberOfValues()
    {
        var directions = Enum.GetValues<Directions>();
        Assert.Equal(9, directions.Length);
    }

    [Theory]
    [InlineData(Directions.Up, true, false, false, false)]
    [InlineData(Directions.Down, false, true, false, false)]
    [InlineData(Directions.Left, false, false, true, false)]
    [InlineData(Directions.Right, false, false, false, true)]
    [InlineData(Directions.UpLeft, true, false, true, false)]
    [InlineData(Directions.UpRight, true, false, false, true)]
    [InlineData(Directions.DownLeft, false, true, true, false)]
    [InlineData(Directions.DownRight, false, true, false, true)]
    [InlineData(Directions.None, false, false, false, false)]
    public void DirectionProperties_MatchExpectedValues(
        Directions direction,
        bool expectedUp, bool expectedDown, bool expectedLeft, bool expectedRight)
    {
        // Directions enum represents combined direction states
        // This test verifies the semantic meaning of each direction
        Assert.Equal(expectedUp, IsUp(direction));
        Assert.Equal(expectedDown, IsDown(direction));
        Assert.Equal(expectedLeft, IsLeft(direction));
        Assert.Equal(expectedRight, IsRight(direction));
    }

    private static bool IsUp(Directions d) => d is Directions.Up or Directions.UpLeft or Directions.UpRight;
    private static bool IsDown(Directions d) => d is Directions.Down or Directions.DownLeft or Directions.DownRight;
    private static bool IsLeft(Directions d) => d is Directions.Left or Directions.UpLeft or Directions.DownLeft;
    private static bool IsRight(Directions d) => d is Directions.Right or Directions.UpRight or Directions.DownRight;
}
