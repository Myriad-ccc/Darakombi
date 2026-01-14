using System.Windows.Shapes;

namespace Darakombi
{
    public static class EntityHelper
    {
        public static T Create<T>(Func<T> func) where T : Entity => func();
    }

    public abstract class Entity : IEntityAttributes
    {
        public double Width { get; set; } = 0;
        public double Height { get; set; } = 0;
        public bool CanCollide { get; set; } = false;
        public CollisionType CollisionType { get; set; }

        public Border Visual { get; set; }
        public Ellipse Overlay { get; set; }
        public bool HasOverlay { get; set; }
        public Brush Color { get; set; } = Brushes.Gray;
        public Brush BorderColor { get; set; } = Brushes.DarkGray;

        public double DetectionRange { get; set; }

        public Point Pos { get; set; } = new();
        [DebugWatch(f: "F1")]
        public double X => Pos.X;
        [DebugWatch(f: "F1")]
        public double Y => Pos.Y;

        public Size Size => new(Width, Height);
        public Rect Rect => new(Pos, Size);

        public Point Center => new(X + Width / 2, Y + Height / 2);

        public double Area => Width * Height;
        public double Parameter => 2 * (Width + Height);

        public Entity() => SetAttributes();
        public Entity(Point pos)
        {
            Pos = pos;
            SetAttributes();
        }

        public Entity(Size size)
        {
            Width = size.Width;
            Height = size.Height;
            SetAttributes();
        }

        public Entity(Point pos, Size size)
        {
            Pos = pos;
            Width = size.Width;
            Height = size.Height;
            SetAttributes();
        }

        public virtual void SetAttributes()
        {
            CanCollide = false;
            CollisionType = CollisionType.Ghost;

            Color = Brushes.Gray;
            BorderColor = Brushes.DarkGray;
        }

        public void CreateVisual()
        {
            Visual = new Border();
            UpdateVisual();
        }

        public void UpdateVisual()
        {
            Visual.Width = Width;
            Visual.Height = Height;
            Visual.BorderThickness = new(Area / (5 * Parameter));
            Visual.Background = Color;
            Visual.BorderBrush = BorderColor;
        }

        public void CreateOverlay()
        {
            Overlay = new();
            UpdateOverlay();
            HasOverlay = true;
        }

        public void CreateOverlay(double? opacity = null, Brush fill = null, Brush stroke = null, double? strokeThickness = null)
        {
            Overlay = new();
            UpdateOverlay(opacity, fill, stroke, strokeThickness);
            HasOverlay = true;
        }

        public void UpdateOverlay(double? opacity = null, Brush fill = null, Brush stroke = null, double? strokeThickness = null)
        {
            Overlay.IsHitTestVisible = false;
            Overlay.Fill = fill ?? Color;
            Overlay.Stroke = stroke ?? BorderColor;
            Overlay.StrokeThickness = strokeThickness ?? 1;
            Overlay.Opacity = opacity ?? 0.35;
            Overlay.Width = DetectionRange * 2;
            Overlay.Height = DetectionRange * 2;
        }
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

    public interface ILive
    {
        public double Speed { get; set; }
        public double DetectionRange { get; set; }
    }
}