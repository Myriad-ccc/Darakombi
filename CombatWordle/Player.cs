namespace CombatWordle
{
    public class Player : Entity
    {
        public double Speed { get; private set; }
        public double DX { get; set; } = 0;
        public double DY { get; set; } = 0;

        public Player(double width = 64, double height = 64, double speed = 10) : base(width, height)
        {
            Width = width;
            Height = height;
            Speed = speed;

            Visual.Background = Brushes.CornflowerBlue;
            Visual.BorderBrush = Brushes.RoyalBlue;
        }
    }
}
