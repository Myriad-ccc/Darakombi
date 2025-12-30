namespace CombatWordle
{
    public class SceneManager
    {
        private readonly Canvas Canvas;

        private readonly HashSet<EntityData> Rendered = [];
        private readonly HashSet<EntityData> Visible = [];
        private readonly List<EntityData> ToRemove = [];

        private readonly Stack<Border> Visuals = [];

        public SceneManager(Canvas canvas)
        {
            Canvas = canvas;
        }

        public void Update(IEnumerable<EntityData> viewportEntities)
        {
            Visible.Clear();
            ToRemove.Clear();

            foreach (var data in viewportEntities)
            {
                Visible.Add(data);
                if (Rendered.Add(data))
                    Add(data);
                Canvas.SetLeft(data.Entity.Visual, data.X);
                Canvas.SetTop(data.Entity.Visual, data.Y);
            }

            foreach (var data in Rendered.Where(e => !Visible.Contains(e)))
                ToRemove.Add(data);
            foreach (var data in ToRemove)
            {
                Remove(data);
                Rendered.Remove(data);
            }
        }

        public void Add(EntityData data)
        {
            var e = data.Entity;
            if (e.Visual == null)
            {
                e.Visual = Visuals.Count > 0 ? Visuals.Pop() : new();
                e.Visual.Width = e.Width;
                e.Visual.Height = e.Height;
                e.Visual.BorderThickness = new(e.Area / (5 * e.Parameter));
                e.Visual.Background = e.Color;
                e.Visual.BorderBrush = e.BorderColor;
                data.Visible = true;
            }

            if (e.Visual.Parent == null)
                Canvas.Children.Add(e.Visual);
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
        }

        public IEnumerable<EntityData> EntitiesInArea(Rect area, List<EntityData> allEntities)
        { // use spatial grid instead if possible
            foreach (var entity in allEntities)
            {
                if (entity.Rect.IntersectsWith(area))
                    yield return entity;
            }
        }
    }
}