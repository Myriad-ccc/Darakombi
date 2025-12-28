namespace CombatWordle
{
    public class Renderer
    {
        private readonly Canvas Canvas;

        private readonly HashSet<EntityData> CurrentlyVisible = [];
        private readonly List<EntityData> Nulls = [];

        public Renderer(Canvas canvas)
        {
            Canvas = canvas;
        }

        public void RenderEntities(List<EntityData> visible, List<EntityData> hidden)
        {
            foreach (var data in visible)
            {
                Add(data);
            }
            foreach (var data in hidden)
            {
                Drop(data);
            }
            foreach (var data in CurrentlyVisible)
            {
                var v = data.Entity.Visual;
                if (v != null)
                {
                    Canvas.SetLeft(data.Entity.Visual, data.X);
                    Canvas.SetTop(data.Entity.Visual, data.Y);
            }
                else
            {
                Nulls.Add(data);
            }
        }
            foreach (var data in Nulls)
                Drop(data);
        }

        public void Add(EntityData data)
        {
            var v = data.Entity.Visual;
            if (v != null)
                if (CurrentlyVisible.Add(data))
                    Canvas.Children.Add(data.Entity.Visual);
        }

        public void Drop(EntityData data)
        {
            var v = data.Entity.Visual;
            if (v != null)
                if (CurrentlyVisible.Remove(data))
                    Canvas.Children.Remove(v);
        }
    }
}
