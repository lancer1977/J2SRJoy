using System.Text.Json.Serialization;

namespace Joystick.Core;
  
public class JoystickSample
{ 

    [JsonPropertyName("gamepadId")]
    public string GamepadId { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public Directions Direction { get; set; }

    [JsonPropertyName("buttons")]
    public int[] Buttons { get; set; } = Array.Empty<int>();

    [JsonPropertyName("axes")]
    public float[] Axes { get; set; } = []; // was int[]
}