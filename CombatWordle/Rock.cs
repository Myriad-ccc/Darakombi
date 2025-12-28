namespace CombatWordle
{
    public class Rock : Entity
    {
        private Dictionary<string, double[]> RockTypes = new()
        {
            //["Clay"] = [1, 4, 1, 4],
            //["Silt"] = [8, 16, 8, 16],
            //["Sand"] = [20, 32, 20, 32],
            //["Granule"] = [40, 64, 40, 64],
            //["Pebble"] = [80, 128, 80, 128],
            //["Cobble"] = [160, 256, 160, 256],
            //["Boulder"] = [320, 512, 320, 512],
            ["Cliff"] = [640, 1024, 640, 1024]
        };
        public string RockType { get; private set; }

        private string GetRandomRockType() => RockTypes.Keys.ElementAt(Random.Shared.Next(RockTypes.Count));

        public Rock()
        {
            RockType = GetRandomRockType();
            var size = RockTypes[RockType];
            SetSize(QOL.GetRandomSize(size));
            SetAttributes();
        }

        public Rock(Point pos) : base(pos)
        {
            RockType = GetRandomRockType();
            var size = RockTypes[RockType];
            SetSize(QOL.GetRandomSize(size));
            SetAttributes();
        }

        public Rock(Point pos, Size size) : base(pos, size)
        {
            SetAttributes();
        }

        private void SetAttributes()
        {
            CanCollide = true;
            CollisionType = CollisionType.Enviornment;

            DefaultColor = Brushes.Gray;
            DefaultBorderColor = Brushes.LightGray;
        }
    }
}
