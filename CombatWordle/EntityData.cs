namespace CombatWordle
{
    public class EntityData
    {
        public Entity Entity;
        public bool Visible;

        public Rect LastRect;

        public LoadStage CurrentLoadStage;
        public LoadStage TargetLoadStage;

        public int GX { get; set; } = -1;
        public int GY { get; set; } = -1;

        public Rect Rect => Entity.Rect;
        public Point Pos => Entity.WorldPos;
        public double X => Rect.X;
        public double Y => Rect.Y;
        public Size Size => Rect.Size;
        public double Width => Size.Width;
        public double Height => Size.Height;

        public EntityData(Entity entity)
        {
            Entity = entity;
            LastRect = entity.Rect;
        }
    }

    public enum LoadStage
    {
        Unregistered = 10,
        Registered = 20,
        Rendered = 30
    }
}