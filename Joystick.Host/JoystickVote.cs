using System.Text.Json.Serialization;

namespace Joystick.Core;

 
public interface JoystickSample
{
    [JsonPropertyName("ts")]
    public int TS { get; set; }

    [JsonPropertyName("gamepadId")]
    public string GamepadId { get; set; }
 
    [JsonPropertyName("direction")]
    public Directions Direction { get; set; } 
    [JsonPropertyName("buttons")]
    public int[] Buttons { get; set; } 


    [JsonPropertyName("axes")]
    public int[] Axes { get; set; }
}