namespace CombatWordle
{
    //public class EntityData
    //{
    //    public Entity Entity { get; init; }
    //    public bool Visible;    

    //    public LoadStage CurrentLoadStage;
    //    public LoadStage TargetLoadStage;

    //    public Rect Rect => Entity.Rect;
    //    public double X => Rect.X;
    //    public double Y => Rect.Y;

    //    public double GridX { get; set; } = -1;
    //    public double GridY { get; set; } = -1;

    //    public EntityData(Entity entity)
    //    {
    //        Entity = entity;
    //    }
    //}

    //public readonly record struct EntityData(
    //    int ID,
    //    Rect Rect,
    //    bool Visible,
    //    LoadStage CurrentLoadStage, //!!!
    //    LoadStage TargetLoadStage,
    //    int Gx,
    //    int Gy
    //    );

    public class EntityData
    {
        public Entity Entity;
        public bool Visible;

        public LoadStage CurrentLoadStage;
        public LoadStage TargetLoadStage;

        public int GX;
        public int GY;

        public Rect Rect => Entity.Rect;
        public Point Pos => Entity.WorldPos;
        public double X => Rect.X;
        public double Y => Rect.Y;

        public EntityData(Entity entity)
        {
            Entity = entity;
        }
    }

    public enum LoadStage
    {
        Unregistered = 10,
        Registered = 20,
        Rendered = 30
    }
}