namespace CombatWordle
{
    public class GameGrid
    {
        private readonly int[,] grid;
        public int Rows { get; }
        public int Cols { get; }

        public int this[int r, int c]
        {
            get => grid[r, c];
            set => grid[r, c] = value;
        }

        public GameGrid(int rows, int cols)
        {
            Rows = rows;
            Cols = cols;
            grid = new int[rows, cols];
        }

        public bool InGrid(Position position) =>
            position.Row >= 0 && position.Row < Rows &&
            position.Col >= 0 && position.Col < Cols;

        public bool EmptyCell(Position position) => InGrid(position) && grid[position.Row, position.Col] == 0;

        public void Clear(GridValue? type = null)
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (type == null)
                        this[r, c] = (int)GridValue.Empty;
                    else
                        if (this[r, c] == (int)type)
                        this[r, c] = (int)GridValue.Empty;
        }

        public bool EntityCanFit(Entity entity)
        {
            foreach (Position p in entity.TilePositions())
                if (!EmptyCell(p))
                    return false;
            return true;
        }

        public void UpdateGridValue(Entity entity, GridValue type)
        {
            foreach (Position p in entity.TilePositions())
                this[p.Row, p.Col] = (int)type;
        }

        public void MoveEntityBy(Entity entity, GridValue type, int rowOffset, int colOffset)
        {
            Clear(type);

            int vertDir = Math.Sign(rowOffset);
            int vertDist = Math.Abs(rowOffset);

            for (int r = 0; r < vertDist; r++)
            {
                entity.MoveBy(vertDir, 0);
                entity.VX += -vertDir * 0.1;
                if (!EntityCanFit(entity))
                {
                    entity.MoveBy(-vertDir, -0);
                    entity.VX -= -vertDir * 0.1;
                    break;
                }
            }

            int horDir = Math.Sign(colOffset);
            int horDist = Math.Abs(colOffset);

            for (int c = 0; c < horDist; c++)
            {
                entity.MoveBy(0, horDir);
                if (!EntityCanFit(entity))
                {
                    entity.MoveBy(0, -horDir);
                    break;
                }
            }

            UpdateGridValue(entity, type);
        }

        public void TryMoveEntityTo(Entity entity, GridValue type, int row, int col)
        {
            Clear(type);

            Position old = entity.Offset;

            entity.SetPos(row, col);
            if (!EntityCanFit(entity))
                entity.Offset = old;

            UpdateGridValue(entity, type);
        }

        public void CenterEntity(Entity entity, GridValue type) => TryMoveEntityTo(entity, type, Rows / 2 - entity.Height / 2, Cols / 2 - entity.Width / 2);

        public void TopLeftEntity(Entity entity, GridValue type) => TryMoveEntityTo(entity, type, 0, 0);
        public void TopRightEntity(Entity entity, GridValue type) => TryMoveEntityTo(entity, type, Rows - entity.Height, 0);
        public void BottomLeftEntity(Entity entity, GridValue type) => TryMoveEntityTo(entity, type, 0, Cols - entity.Width);
        public void BottomRightEntity(Entity entity, GridValue type) => TryMoveEntityTo(entity, type, Rows - entity.Height, Cols - entity.Width);

        public void TopCenterEntity(Entity entity, GridValue type) => TryMoveEntityTo(entity, type, 0, Cols / 2 - entity.Width / 2);
        public void LeftCenterEntity(Entity entity, GridValue type) => TryMoveEntityTo(entity, type, Rows / 2 - entity.Height / 2, 0);
        public void RightCenterEntity(Entity entity, GridValue type) => TryMoveEntityTo(entity, type, Rows / 2 - entity.Height / 2, Cols - entity.Width);
        public void BottomCenterEntity(Entity entity, GridValue type) => TryMoveEntityTo(entity, type, Rows - entity.Height, Cols / 2 - entity.Width / 2);

        public bool EntityAirborne(Entity entity)
        {
            int rowBelow = entity.Offset.Row + entity.Height;

            if (rowBelow >= Rows)
                return false;

            for (int c = 0; c < entity.Width; c++)
                if (this[rowBelow, entity.Offset.Col + c] != (int)GridValue.Empty)
                    return false;

            return true;
        }

        public void EntityGravitate(Entity entity, GridValue type)
        {
            if (EntityAirborne(entity))
                MoveEntityBy(entity, type, 1, 0);
        }

        public void EntityJump(Entity entity, GridValue type)
        {
            if (!EntityAirborne(entity))
                MoveEntityBy(entity, type, -entity.JumpHeight, 0);
        }
    }
}