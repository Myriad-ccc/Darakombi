namespace CombatWordle
{
    public class Position
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Position()
        {
            X = 0;
            Y = 0;
        }

        public Position(double x, double y)
        {
            X = x;
            Y = y;
        }

        public void Move(double dx, double dy)
        {
            X += dx;
            Y += dy; 
        }

        public override bool Equals(object obj)
        {
            return obj is Position position &&
                X == position.X &&
                Y == position.Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public static bool operator ==(Position left, Position right)
        {
            return EqualityComparer<Position>.Default.Equals(left, right);
        }

        public static bool operator !=(Position left, Position right)
        {
            return !(left == right);
        }
    }
}
