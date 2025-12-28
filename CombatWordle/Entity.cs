namespace CombatWordle
{
    public abstract class Entity
    {
        public Point WorldPos { get; set; } = new();

        public double Width { get; set; }
        public double Height { get; set; }

        public bool CanCollide { get; set; } = false;
        public CollisionType CollisionType { get; protected set; }

        public Border Visual;
        public Brush DefaultColor { get; protected set; } = Brushes.Gray;
        public Brush DefaultBorderColor { get; protected set; } = Brushes.DarkGray;

        public double X => WorldPos.X;
        public double Y => WorldPos.Y;

        public Size Size => new(Width, Height);

        public double Area => Width * Height;
        public double Parameter => 2 * (Width + Height);

        public Rect Rect => new(WorldPos, Size);

        public Entity() { }

        public Entity(Point pos)
        {
            WorldPos = pos;
        }

        public Entity(Size size)
        {
            SetSize(size);
        }

        public Entity(Point pos, Size size)
        {
            WorldPos = pos;
            SetSize(size);
        }

        public void SetSize(Size size)
        {
            Width = size.Width;
            Height = size.Height;
        }
    }
}
