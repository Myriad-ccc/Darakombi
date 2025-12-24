namespace CombatWordle
{
    public abstract class Entity
    {
        private int Health { get; set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Position Offset { get; set; }
        public bool Gravity { get; set; }

        public int JumpHeight { get; set; } = 12;
        public double VX { get; set; } = 0;
        public double VY { get; set; } = 0;
        public double RemainderX { get; set; } = 0;
        public double RemainderY { get; set; } = 0;

        public int CellTop => Offset.Row;
        public int CellBottom => CellTop + Height - 1;
        public int CellLeft => Offset.Col;
        public int CellRight => CellLeft + Width - 1;
        public double BorderWidth => Math.Pow(Width * Height, 1 / 2.8);

        public Entity(int width = 2, int height = 2, bool gravity = true)
        {
            Width = width;
            Height = height;
            Offset = new Position(0, 0);
            Gravity = gravity;
        }

        public void MoveBy(int rows, int cols)
        {
            Offset.Row += rows;
            Offset.Col += cols;
        }

        public void SetPos(int row, int col)
        {
            Offset.Row = row;
            Offset.Col = col;
        }

        public IEnumerable<Position> TilePositions()
        {
            for (int r = 0; r < Height; r++)
                for (int c = 0; c < Width; c++)
                    yield return new Position(Offset.Row + r, Offset.Col + c);
        }

        public bool IsAt(int r, int c)
        {
            return r >= Offset.Row &&
                r < Offset.Row + Height &&
                c >= Offset.Col &&
                c < Offset.Col + Width;
        }
    }
}
