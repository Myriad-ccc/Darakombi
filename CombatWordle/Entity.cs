using System.Windows.Media.Media3D;

namespace CombatWordle
{
    public abstract class Entity
    {
        public Point WorldPos { get; set; } = new();

        public double Width { get; set; }
        public double Height { get; set; }

        public bool CanCollide { get; set; } = false;

        public Border Visual = new();
        public Brush DefaultColor { get; set; } = Brushes.Gray;
        public Brush DefaultBorderColor { get; set; } = Brushes.DarkGray;

        public double X => WorldPos.X;
        public double Y => WorldPos.Y;

        public Size Size => new(Width, Height);

        public double Area => Width * Height;
        public double Parameter => 2 * (Width + Height);

        public double Thickness => Math.Max(
            Math.Max(Visual.BorderThickness.Top, Visual.BorderThickness.Left), 
            Math.Max(Visual.BorderThickness.Bottom, Visual.BorderThickness.Right));
        public double ActualWidth => Width - 2 * Thickness;
        public double ActualHeight => Height - 2 * Thickness;

        public Entity() { }

        public Entity(Point pos)
        {
            WorldPos = pos;
        }

        public Entity(Size size)
        {
            UpdateDimensions(size);
        }

        public Entity(Point pos, Size size)
        {
            WorldPos = pos;
            UpdateDimensions(size);
        }

        public void UpdateDimensions(Size size)
        {
            Width = size.Width;
            Height = size.Height;
            Visual.Width = size.Width;
            Visual.Height = size.Height;
            Visual.BorderThickness = new Thickness(Area / (5 * Parameter));
        }
    }
}
