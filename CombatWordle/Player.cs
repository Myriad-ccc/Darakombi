namespace CombatWordle
{
    public class Player : Entity
    {
        public double Speed { get; private set; } = 10;
        public double DX { get; set; } = 0;
        public double DY { get; set; } = 0;

        public Player()
        {
            SetVisuals();
        }

        public Player(Point pos, Size size) : base(pos, size)
        {
            SetVisuals();
        }

        private void SetVisuals()
        {
            DefaultColor = Brushes.CornflowerBlue;
            DefaultBorderColor = Brushes.RoyalBlue;

            Visual.Background = DefaultColor;
            Visual.BorderBrush = DefaultBorderColor;
        }
    }
}
