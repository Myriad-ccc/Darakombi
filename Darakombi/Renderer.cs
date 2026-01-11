using System.Windows.Shapes;

namespace Darakombi
{
    public class Renderer
    {
        private readonly Canvas Canvas;

        private readonly HashSet<EntityData> Rendered = [];
        private readonly HashSet<EntityData> Visible = [];
        private readonly List<EntityData> ToRemove = [];

        private readonly Stack<Border> Visuals = [];
        private readonly Stack<Ellipse> Overlays = [];

        public bool ShowOverlays { get; set; } = true;

        public Renderer(Canvas canvas) => Canvas = canvas;

        public void RenderEntities(IEnumerable<EntityData> viewportEntities)
        {
            Visible.Clear();
            ToRemove.Clear();

            foreach (var data in viewportEntities)
            {
                RenderVisual(data);
                RenderOverlay(data);
            }

            foreach (var data in Rendered.Where(e => !Visible.Contains(e)))
                ToRemove.Add(data);
            foreach (var data in ToRemove)
            {
                Remove(data);
                Rendered.Remove(data);
            }
        }

        private void RenderVisual(EntityData data)
        {
            Visible.Add(data);
            if (Rendered.Add(data))
                Add(data);
            Canvas.SetLeft(data.Entity.Visual, data.X);
            Canvas.SetTop(data.Entity.Visual, data.Y);
        }

        private void RenderOverlay(EntityData data)
        {
            var e = data.Entity;
            if (e.HasOverlay)
            {
                if (e.Overlay == null)
                {
                    if (Overlays.Count > 0)
                    {
                        e.Overlay = Overlays.Pop();
                        e.UpdateOverlay();
                    }
                    else
                        e.CreateOverlay();
                }

                if (!ShowOverlays)
                {
                    if (e.Overlay.Parent != null)
                        Canvas.Children.Remove(e.Overlay);
                    return;
                }

                if (e.Overlay.Parent == null)
                {
                    Canvas.Children.Add(e.Overlay);
                    Panel.SetZIndex(e.Overlay, 10);
                }
                if (e.Overlay != null)
                {
                    Canvas.SetLeft(e.Overlay, data.X + data.Width / 2 - e.Overlay.Width / 2);
                    Canvas.SetTop(e.Overlay, data.Y + data.Height / 2 - e.Overlay.Height / 2);
                }
            }
        }

        public void Add(EntityData data)
        {
            var e = data.Entity;
            if (e.Visual == null)
            {
                if (Visuals.Count > 0)
                {
                    e.Visual = Visuals.Pop();
                    e.UpdateVisual();
                }
                else
                    e.CreateVisual();
                data.Visible = true;
            }
            if (e.Visual.Parent == null)
            {
                Canvas.Children.Add(e.Visual);
                Panel.SetZIndex(e.Visual, 40);
            }
        }

        public void Remove(EntityData data)
        {
            var e = data.Entity;

            if (e.Visual != null)
            {
                Canvas.Children.Remove(e.Visual);
                Visuals.Push(e.Visual);
                e.Visual = null;
                data.Visible = false;
            }

            if (e.Overlay != null)
            {
                Canvas.Children.Remove(e.Overlay);
                Overlays.Push(e.Overlay);
                e.Overlay = null;
            }
        }

        public void ClearCache()
        {
            Visuals.Clear();
            Overlays.Clear();
        }

        public void ClearAll()
        {
            foreach (var visual in Visuals)
                Canvas.Children.Remove(visual);
            foreach (var overlay in Overlays)
                Canvas.Children.Remove(overlay);
            ClearCache();
        }
    }
}