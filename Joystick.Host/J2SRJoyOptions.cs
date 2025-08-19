namespace Joystick.Host;
    public class J2SRJoyOptions
    {
        public int VoteLength { get; set; }
        public int MaxVotes { get; set; }
    }
    public enum Directions
    {
        None, Up, Down, Left, Right, UpLeft, UpRight, DownLeft, DownRight
    }
    public record JoystickVote
    {
        //public UserId UserID {get;set;}
        public Directions Direction { get; set; }
    }

    public record struct JoystickCommand(bool Up, bool Down, bool Left, bool Right, bool X, bool Y);


