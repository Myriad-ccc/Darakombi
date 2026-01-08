
namespace Darakombi
{
    public class Map : Border
    {
        public double Thickness => Math.Max(Math.Max(BorderThickness.Top, BorderThickness.Left), Math.Max(BorderThickness.Right, BorderThickness.Bottom));
        public double Area => Width * Height;
        public double Parameter => 2 * (Width + Height);

        public Point Center => new(Width / 2, Height / 2);

        public Map(double width, double height) => SetPropeties(new Size(width, height));
        public Map(Size size) => SetPropeties(size);

        private void SetPropeties(Size size)
        {
            Width = size.Width;
            Height = size.Height;
            Background = QOL.RGB(20);
            BorderThickness = new(0);
        }

        public bool RectInside(Rect rect)
        {
            return rect.Left >= Thickness
                && rect.Top >= Thickness
                && rect.Right <= Width - Thickness
                && rect.Bottom <= Height - Thickness;
        }
    }
}