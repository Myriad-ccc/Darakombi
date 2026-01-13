using System.Reflection;

namespace Darakombi
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

        public static Type[] GetDerivedTypes<T>() =>
            Assembly
            .GetAssembly(typeof(T))!
            .GetTypes()
            .Where(t => typeof(T).IsAssignableFrom(t) && t != typeof(T) && !t.IsAbstract)
            .ToArray();

        public static double GetDistance(Point one, Point two) => Math.Sqrt(Math.Pow(two.X - one.X, 2) + Math.Pow(two.Y - one.Y, 2));
        public static double GetDistanceSquared(Point one, Point two) => Math.Pow(two.X - one.X, 2) + Math.Pow(two.Y - one.Y, 2);

        public static void D(object obj, bool dateTime = true) => Debug.WriteLine(dateTime ? $"[{DateTime.Now}] " + $"{obj}" : $"{obj}");

        public static Point ScreenToWorld(Point screenPos, ScaleTransform scale, TranslateTransform translate) =>
            new(
                (screenPos.X - translate.X) / scale.ScaleX,
                (screenPos.Y - translate.Y) / scale.ScaleY);

        public static bool IsPointOverElement(MouseButtonEventArgs e, FrameworkElement element)
        {
            var mousePos = e.GetPosition(element);
            return mousePos.X >= 0 && mousePos.X <= (element.ActualWidth == 0 ? element.Width : element.ActualWidth)
                && mousePos.Y >= 0 && mousePos.Y <= (element.ActualHeight == 0 ? element.Height : element.ActualHeight);
        }

        public static bool NumInRange(int num, int min, int max) => num >= min && num <= max;
        public static bool NumInRange(double num, double min, double max) => num >= min && num <= max;

        public static bool SizeInRange(Size size, double minW, double maxW, double minH, double maxH) =>
            size.Width >= minW && size.Width <= maxW &&
            size.Height >= minH && size.Height <= maxH;
        public static bool SizeInRange(double width, double height, double minW, double maxW, double minH, double maxH) =>
            width >= minW && width <= maxW &&
            height >= minH && height <= maxH;

        public static bool NameObjectMatch(string name, object obj)
        {
            var type = obj.GetType();
            return type != null && (name == type.Name || type.GetProperty(name) != null || type.GetField(name) != null);
        }

        public static bool IsVis(FrameworkElement el) => el.Visibility == Visibility.Visible;
    }
}