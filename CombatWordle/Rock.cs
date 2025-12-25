namespace CombatWordle
{
    public class Rock : Entity
    {
        private static readonly Random random = new();

        public Rock()
        {
            UpdateDimensions(GetRandomSize());
            SetVisuals();
        }

        public Rock(Point pos) : base(pos)
        {
            UpdateDimensions(GetRandomSize());
            SetVisuals();
        }

        public Rock(Point pos, Size size) : base(pos, size)
        {
            SetVisuals();
        }

        private void SetVisuals()
        {
            CanCollide = true;

            DefaultColor = Brushes.Gray;
            DefaultBorderColor = Brushes.LightGray;

            Visual.Background = DefaultColor;
            Visual.BorderBrush = DefaultBorderColor;
        }

        private static Size GetRandomSize()
        {
            int w = 0, h = 0;
            while (w * h < 1000)
            {
                w = random.Next(20, 200);
                h = random.Next(5, 120);
            }
            return new(w, h);
        }
    }
}
