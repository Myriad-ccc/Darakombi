namespace CombatWordle
{
    public abstract class Entity
    {
        public Position WorldPos { get; set; } = new Position();

        public double Width { get; set; }
        public double Height { get; set; }

        public Border Visual = new();

        public double X => WorldPos.X;
        public double Y => WorldPos.Y;

        public double Area => Width * Height;
        public double Parameter => 2 * (Width + Height);

        public double Thickness => Math.Max(Math.Max(Visual.BorderThickness.Top, Visual.BorderThickness.Left), Math.Max(Visual.BorderThickness.Bottom, Visual.BorderThickness.Right));
        public double ActualWidth => Width - 2 * Thickness;
        public double ActualHeight => Height - 2 * Thickness;

        public Entity() { }

        public Entity(double width, double height)
        {
            Width = width;
            Height = height;

            Visual.Width = Width;
            Visual.Height = Height;
            Visual.BorderThickness = new Thickness(Area / (4 * Parameter));
        }

        public void UpdateDimensions(double width, double height)
        {
            Width = width;
            Height = height;
            Visual.Width = width;
            Visual.Height = height;
        }
    }
}
