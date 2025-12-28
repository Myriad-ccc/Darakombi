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
            int x = (int)Math.Ceiling(pos.X / CellSize);
            int y = (int)Math.Ceiling(pos.Y / CellSize);

            x = Math.Max(0, Math.Min(x, GridWidth - 1)); //*
            y = Math.Max(0, Math.Min(y, GridWidth - 1));

            return new GridCell(x, y);
        }

        public void Add(EntityData entity)
        {
            var cell = PointToCell(entity.Pos);
            Cells[cell].Add(entity);
            entity.GX = cell.X;
            entity.GY = cell.Y;
        }

        public void Remove(EntityData entity)
        {
            Cells[new(entity.GX, entity.GY)].Remove(entity);
            entity.GX = 0;
            entity.GY = 0;
        }

        public void Update(EntityData entity)
        {
            var cell = PointToCell(entity.Pos);
            if (cell.X != entity.GX || cell.Y != entity.GY)
            {
                Remove(entity);
                Add(entity);
            }
        }

        public List<EntityData> SearchWorld(Rect area) // to be improved
        {
            var found = new List<EntityData>();

            var topLeft = PointToCell(area.TopLeft);
            var bottomRight = PointToCell(area.BottomRight);

            for (int x = topLeft.X; x < bottomRight.X; x++)
            {
                for (int y = topLeft.Y; y < bottomRight.Y; y++)
                {
                    found.AddRange(Cells[new(x, y)]);
                }
            }
            return found;
        }
    }
}