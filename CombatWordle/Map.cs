
namespace CombatWordle
{
    public class Map : Border
    {
        public double Thickness => Math.Max(Math.Max(BorderThickness.Top, BorderThickness.Left), Math.Max(BorderThickness.Bottom, BorderThickness.Right));
        public double Area => Width * Height;
        public double Parameter => 2 * (Width + Height);

        public Map()
        {
            Background = QOL.RGB(20);
            BorderBrush = QOL.RGB(15);

            SizeChanged += (s, ev) =>
            {
                double w = ActualWidth;
                double h = ActualHeight;

                if (w <= 0 || 0 >= h)
                    return;
                BorderThickness = new Thickness(Parameter);
            };
        }

        public Map(double width, double height)
        {
            Width = width;
            Height = height;
            Background = QOL.RGB(20);
            BorderBrush = QOL.RGB(15);

            SizeChanged += (s, ev) =>
            {
                double w = ActualWidth;
                double h = ActualHeight;

                if (w <= 0 || 0 >= h)
                    return;
                BorderThickness = new Thickness((w + (h - w) / 2) / 100);
            };
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            CoerceValue(BorderThicknessProperty);
        }
    }
}