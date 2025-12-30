namespace CombatWordle
{
    public static class EntityHelper
    {
        public static Type[] EntityTypes = QOL.GetDerivedTypes<Entity>();
        public static Type GetRandomEntityType() => EntityTypes[Random.Shared.Next(EntityTypes.Length)];

        public static T Create<T>(Func<T> func) where T : Entity => func();
        //public static T Create<T>(Point pos) where T : Entity, new()
        //{
        //    var e = new T
        //    {
        //        Pos = pos
        //    };
        //    return e;
        //}
        //public static T Create<T>(Size size) where T : Entity, new()
        //{
        //    var e = new T
        //    {
        //        Width = size.Width,
        //        Height = size.Height
        //    };
        //    return e;
        //}
        //public static T Create<T>(Point pos, Size size) where T : Entity, new()
        //{
        //    var e = new T
        //    {
        //        Pos = pos,
        //        Width = size.Width,
        //        Height = size.Height
        //    };
        //    return e;
        //}

        //public static T Create<T>(Func<T, Point> positionLogic) where T : Entity, new()
        //{
        //    var e = new T();
        //    e.Pos = positionLogic(e);
        //    return e;
        //}
        //public static T Create<T>(Func<T, Size> sizeLogic) where T : Entity, new()
        //{
        //    var e = new T();
        //    var size = sizeLogic(e);
        //    e.Width = size.Width;
        //    e.Height = size.Height;
        //    return e;
        //}
        //public static T Create<T>(Func<T, Point> positionLogic, Func<T, Size> sizeLogic) where T : Entity, new()
        //{
        //    var e = new T();
        //    e.Pos = positionLogic(e);
        //    var size = sizeLogic(e);
        //    e.Width = size.Width;
        //    e.Height = size.Height;
        //    return e;
        //}
        //public static T Create<T>(Func<T, Size> sizeLogic, Func<T, Point> positionLogic) where T : Entity, new()
        //{
        //    var e = new T();
        //    var size = sizeLogic(e);
        //    e.Width = size.Width;
        //    e.Height = size.Height;
        //    e.Pos = positionLogic(e);
        //    return e;
        //}

        //public static Entity CreateRandom()
        //{
        //    var type = GetRandomEntityType();
        //    var e = (Entity)Activator.CreateInstance(type)!;
        //    return e;
        //}
    }

    public abstract class Entity : IEntityAttributes, IInitialize
    {
        public double Width { get; set; } = 0;
        public double Height { get; set; } = 0;
        public bool CanCollide { get; set; } = false;
        public CollisionType CollisionType { get; set; }

        public Border Visual;
        public Brush Color { get; set; } = Brushes.Gray;
        public Brush BorderColor { get; set; } = Brushes.DarkGray;

        public Point Pos { get; set; } = new();
        public Size Size => new(Width, Height);

        public Rect Rect => new(Pos, Size);
        public double X => Pos.X;
        public double Y => Pos.Y;

        public double Area => Width * Height;
        public double Parameter => 2 * (Width + Height);

        public Entity() => SetAttributes();

        public virtual void SetAttributes()
        {
            CanCollide = false;
            CollisionType = CollisionType.Ghost;

            Color = Brushes.Gray;
            BorderColor = Brushes.DarkGray;
        }

        public virtual void IInitialize() { }
    }

    public interface IEntityAttributes
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public bool CanCollide { get; set; }
        public CollisionType CollisionType { get; set; }
        public Brush Color { get; set; }
        public Brush BorderColor { get; set; }
        public void SetAttributes();
    }

    public interface IInitialize { }
}