namespace CombatWordle
{
    public class Rock : Entity
    {
        private readonly Random random = new();

        public Rock()
        {
            Visual.Background = Brushes.Gray;

            int w = 0;
            int h = 0;

            int i = 0;
            while (w * h < 1000)
            {
                w = random.Next(20, 200);
                h = random.Next(5, 120);
            }

            UpdateDimensions(w, h);
        }
    }
}
