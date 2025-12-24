namespace CombatWordle
{
    public abstract class Entity
    {
        public Position WorldPos { get; set; } = new Position();

        public double Width { get; private set; }
        public double Height { get; private set; }

        public double Speed { get; private set; }
        public double DX { get; set; } = 0;
        public double DY { get; set; } = 0;

        public Border Visual;

        public Entity(double width = 64, double height = 64, double speed = 50)
        {
            Width = width;
            Height = height;
            Speed = speed;
            Visual = new Border()
            {
                Width = Width,
                Height = Height,
                Background = Brushes.CornflowerBlue,
                BorderThickness = new Thickness(2)
            };
        }
    }
}
