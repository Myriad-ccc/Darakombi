namespace CombatWordle
{
    public class Direction
    {
        private readonly Random random = new Random();

        public static Direction Left = new(0, -1);
        public static Direction Right = new(0, 1);
        public static Direction Up = new(-1, 0);
        public static Direction Down = new(1, 0);

        public int RowOffset { get; }
        public int ColOffset { get; }

        public Direction(int rowOffset, int colOffset)
        {
            RowOffset = rowOffset;
            ColOffset = colOffset;
        }

        public Direction Opposite() => new(-RowOffset, -ColOffset);

        public override bool Equals(object obj)
        {
            return Equals(obj as Direction);
        }

        public bool Equals(Direction other)
        {
            return other is not null &&
                RowOffset == other.RowOffset &&
                ColOffset == other.ColOffset;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RowOffset, ColOffset);
        }

        public static bool operator ==(Direction left, Direction right)
        {
            return EqualityComparer<Direction>.Default.Equals(left, right);
        }

        public static bool operator !=(Direction left, Direction right)
        {
            return !(left == right);
        }
    }
}
