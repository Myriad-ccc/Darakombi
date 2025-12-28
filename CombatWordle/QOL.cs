namespace CombatWordle
{
    public static class QOL
    {
        public static SolidColorBrush RandomColor() => new(Color.FromArgb(255, (byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256), (byte)Random.Shared.Next(256)));
        public static SolidColorBrush RGB(int r) => new(Color.FromArgb(255, (byte)r, (byte)r, (byte)r));

        public static void WriteOut(object obj) => MessageBox.Show($"{obj}");

        public static Point CenterOfRect(Rect rect) => new(
            rect.X + rect.Width / 2,
            rect.Y + rect.Height / 2
            );

        private static double AverageFPS;
        public static double GetAverageFPS(double dt)
        {
            double perFrame = 1.0 / dt;
            double alpha = 1 - Math.Exp(-dt / 0.5);
            AverageFPS = (AverageFPS == 0) ? perFrame : AverageFPS + alpha * (perFrame - AverageFPS);
            return AverageFPS;
        }

        public static double NextDoubleInRange(double min, double max)
        {
            if (min >= max) return min;
            return Random.Shared.NextDouble() * (max - min) + min;
        }

        public static Size GetRandomSize(double wMin, double wMax, double hMin, double hMax)
        {
            if (wMin <= 0 || hMin <= 0 || wMin > wMax || hMin > hMax)
                return Size.Empty;
            double w = NextDoubleInRange(wMin, wMax);
            double h = NextDoubleInRange(hMin, hMax);
            return new(w, h);
        }

        public static Size GetRandomSize(double[] arg)
        {
            if (arg.Length != 4) return Size.Empty;
            double wMin = arg[0], wMax = arg[1], hMin = arg[2], hMax = arg[3];
            if (wMin <= 0 || hMin <= 0 || wMin > wMax || hMin > hMax)
                return Size.Empty;
            double w = NextDoubleInRange(wMin, wMax);
            double h = NextDoubleInRange(hMin, hMax);
            return new(w, h);
        }
    }
}
