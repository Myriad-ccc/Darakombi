namespace Darakombi
{
    public class EntityData
    {
        public Entity Entity;
        public bool Visible;

        public Rect LastRect;

        public int GX { get; set; } = -1;
        public int GY { get; set; } = -1;

        public Rect Rect => Entity.Rect;
        public Point Pos => Entity.Pos;
        public double X => Rect.X;
        public double Y => Rect.Y;
        public Size Size => Rect.Size;
        public double Width => Size.Width;
        public double Height => Size.Height;

        public EntityData(Entity entity)
        {
            Entity = entity;
            LastRect = entity.Rect;
            Visible = false;
        }
    }
}