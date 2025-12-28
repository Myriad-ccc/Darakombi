namespace CombatWordle
{
    public class Player : Entity
    {
        public double Speed { get; private set; } = 500;
        public double DX { get; set; } = 0;
        public double DY { get; set; } = 0;

        public Player()
        {
            SetAttributes();
        }

        public Player(Point pos, Size size) : base(pos, size)
        {
            SetAttributes();
        }

        private void SetAttributes()
        {
            CanCollide = true;
            CollisionType = CollisionType.Live;

            DefaultColor = Brushes.CornflowerBlue;
            DefaultBorderColor = Brushes.RoyalBlue;
        }
    }
}