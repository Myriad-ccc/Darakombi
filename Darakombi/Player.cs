namespace Darakombi
{
    [DebugWatch]
    public class Player : Entity, ILive
    {
        public double Speed { get; set; } = 500;
        public double DX { get; set; } = 0;
        public double DY { get; set; } = 0;
        public double VX { get; set; } = 0;
        public double VY { get; set; } = 0;

        public Player() : base() { DebugManager.Track(this); }
        public Player(Point pos) : base(pos) { DebugManager.Track(this); }
        public Player(Size size) : base(size) { DebugManager.Track(this); }
        public Player(Point pos, Size size) : base(pos, size) { DebugManager.Track(this); }

        public override void SetAttributes()
        {
            CanCollide = true;
            CollisionType = CollisionType.Live;

            Color = Brushes.CornflowerBlue;
            BorderColor = Brushes.RoyalBlue;

            DetectionRange = 200;
        }
    }
}