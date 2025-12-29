namespace CombatWordle
{
    public class SpatialGrid
    {
        /*to consider:
         * 64 cell size
         * custom sensing range
         * better and more customizable query algorithm
         */

        private const int CellSize = 128;
        private readonly int GridWidth;
        private readonly int GridHeight;

        public readonly record struct GridCell(int X, int Y);
        private readonly Dictionary<GridCell, List<EntityData>> Cells;

        public SpatialGrid(double mapWidth, double mapHeight)
        {
            GridWidth = (int)Math.Ceiling(mapWidth / CellSize);
            GridHeight = (int)Math.Ceiling(mapHeight / CellSize);

            Cells = []; //each cell has a list of entities (or is empty)

            for (int r = 0; r < GridHeight; r++)
            {
                for (int c = 0; c < GridWidth; c++)
                {
                    var cell = new GridCell(c, r);
                    if (!Cells.TryGetValue(cell, out var _))
                        Cells[cell] = [];
                }
            }
        }

        public GridCell PointToCell(Point pos)
        {
            int x = (int)Math.Floor(pos.X / CellSize);
            int y = (int)Math.Floor(pos.Y / CellSize);

            x = Math.Max(0, Math.Min(x, GridWidth - 1)); //*
            y = Math.Max(0, Math.Min(y, GridHeight - 1));

            return new GridCell(x, y);
        }

        public IEnumerable<GridCell> OccupiedCells(Rect rect)
        {
            int left = Math.Max(0, (int)(rect.Left / CellSize));
            int top = Math.Max(0, (int)(rect.Top / CellSize));
            int right = Math.Min((int)(rect.Right / CellSize), GridWidth - 1);
            int bottom = Math.Min((int)(rect.Bottom / CellSize), GridHeight - 1);

            for (int r = top; r <= bottom; r++)
                for (int c = left; c <= right; c++)
                    yield return new(c, r);
        }

        public void Add(EntityData entity)
        {
            foreach (var cell in OccupiedCells(entity.Rect))
            {
                if (!Cells.TryGetValue(cell, out _))
                    Cells[cell] = [];
                Cells[cell].Add(entity);
            }
            var gridCell = PointToCell(entity.Pos);
            entity.GX = gridCell.X;
            entity.GY = gridCell.Y;
        }

        public void Remove(EntityData entity)
        {
            foreach (var cell in OccupiedCells(entity.Rect))
            {
                if (Cells.TryGetValue(cell, out var list))
                    list.Remove(entity);
            }
        }

        public void Update(EntityData entity)
        {
            Rect current = entity.Rect;
            Rect last = entity.LastRect;

            if (entity.GX < 0 || entity.GY < 0)
            {
                Add(entity);
                return;
            }

            foreach (var cell in OccupiedCells(last))
            {
                if (Cells.TryGetValue(cell, out var list))
                    list.Remove(entity);
            }
            foreach (var cell in OccupiedCells(current))
            {
                if (!Cells.TryGetValue(cell, out _))
                    Cells[cell] = [];
                Cells[cell].Add(entity);
            }
            entity.LastRect = current;
        }

        public List<EntityData> Search(Rect area) // to be improved
        {
            var found = new List<EntityData>();
            var seen = new HashSet<EntityData>();

            foreach (var cell in OccupiedCells(area))
            {
                if (Cells.TryGetValue(cell, out var list))
                    foreach (var entity in list)
                        if (seen.Add(entity))
                            found.Add(entity);
            }
            return found;
        }
    }
}