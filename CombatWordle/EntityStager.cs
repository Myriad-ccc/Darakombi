namespace CombatWordle
{
    public class EntityStager
    {
        private Rect viewport;
        public Rect Viewport
        {
            get => viewport;
            set
            {
                viewport = value;
                UpdateRegionRects();
            }
        }

        readonly int regionCount = Enum.GetValues<Region>().Length;
        public Region[] Regions;
        public Rect[] RegionRects;

        private readonly LoadStage[] LoadStages =
            [
            LoadStage.Unregistered,
            LoadStage.Registered,
            LoadStage.Rendered
            ];

        public EntityStager(Rect viewport)
        {
            Regions = Enum.GetValues<Region>();
            RegionRects = new Rect[regionCount];
            Viewport = viewport;
        }

        private void UpdateRegionRects()
        {
            for (int i = 0; i < regionCount; i++)
            {
                var v = viewport;
                v.Inflate((int)Regions[i], (int)Regions[i]);
                RegionRects[i] = v;
            }
        }

        public Rect GetRectFromRegion(Region region)
        {
            return RegionRects[Array.IndexOf(Regions, region)];
        }

        public Region GetRegionFromRect(Rect rect)
        {
            for (int i = 0; i < regionCount - 1; i++)
            {
                if (RegionRects[i].IntersectsWith(rect))
                    return Regions[i];
            }
            return Regions.Last();
        }

        private LoadStage GetTargetLoadStage(EntityData data)
        {
            var r = GetRegionFromRect(data.Rect);
            if (r == Region.Immediate) return LoadStage.Rendered;
            if (r == Region.Near) return LoadStage.Registered;
            if (r == Region.Far) return LoadStage.Registered;
            return LoadStage.Unregistered;
        }

        public void UpdateLoadStage(EntityData data)
        {
            var current = data.CurrentLoadStage;
            var target = GetTargetLoadStage(data);

            if (current == target) return;

            int currentIndex = Array.IndexOf(LoadStages, current);
            int targetIndex = Array.IndexOf(LoadStages, target);

            if (targetIndex > currentIndex)
                data.CurrentLoadStage = LoadStages[++currentIndex];
            if (targetIndex < currentIndex)
                data.CurrentLoadStage = LoadStages[--currentIndex];
        }
    }
}