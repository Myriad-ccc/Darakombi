namespace CombatWordle
{
    public class SceneManager
    {
        private readonly Canvas Canvas;
        private readonly EntityStager Stager;
        private readonly Renderer Renderer;

        private List<EntityData> Visible;
        private List<EntityData> Hidden;

        public SceneManager(Canvas canvas, List<EntityData> visible, List<EntityData> hidden)
        {
            Canvas = canvas;
            Stager = new(new());
            Renderer = new(canvas);

            Visible = visible;
            Hidden = hidden;
        }

        public void Update(Rect viewport, List<EntityData> entities)
        { // REMOVE COLLIDERS FROM SCENE UPDATER
            Stager.Viewport = viewport;

            foreach (var data in entities)
            {
                Stager.UpdateLoadStage(data);
                Cull(viewport, data);
                var e = data.Entity;
                switch (data.CurrentLoadStage)
                {
                    case LoadStage.Unregistered:
                        //e.CanCollide = false;
                        //Colliders.Remove(data);
                        break;
                    case LoadStage.Registered:
                        //e.CanCollide = true;
                        //Colliders.Add(data);
                        if (e.Visual != null)
                        {
                            Renderer.Drop(data);
                            e.Visual = null;
                        }
                        break;
                    case LoadStage.Rendered:
                        e.Visual ??= new()
                        {
                            Width = e.Width,
                            Height = e.Height,
                            BorderThickness = new(e.Area / (5 * e.Parameter)),
                            Background = e.DefaultColor,
                            BorderBrush = e.DefaultBorderColor
                        };
                        break;
                }
            }

            Renderer.RenderEntities(Visible, Hidden);

            Visible.Clear();
            Hidden.Clear();
        }

        private void Cull(Rect viewport, EntityData entity)
        {
            if (viewport.IntersectsWith(entity.Rect))
            {
                if (!entity.Visible)
                {
                    entity.Visible = true;
                    Visible.Add(entity);
                }
            }
            else
            {
                if (entity.Visible)
                {
                    entity.Visible = false;
                    Hidden.Add(entity);
                }
            }
        }
    }
}