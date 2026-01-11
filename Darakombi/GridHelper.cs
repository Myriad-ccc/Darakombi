using System.Windows.Shapes;

namespace Darakombi
{
    public class GridHelper : Grid
    {
        public int CellSize { get; set; } = 128;
        public Brush Color { get; set; } = Brushes.White;
        public double Thickness { get; set; } = 1.0;

        public GridHelper(int cellSize, int width, int height, Brush color, double opacity)
        {
            CellSize = cellSize;
            Width = width;
            Height = height;
            Color = color;
            Opacity = opacity;
            Create();
        }

        public Grid Create()
        {
            var grid = new Grid()
            {
                IsHitTestVisible = false,
                Opacity = Opacity,
            };
            Canvas.SetLeft(grid, 0);
            Canvas.SetTop(grid, 0);

            Build();
            return grid;
        }

        public void Build()
        {
            for (int x = 0; x <= Width; x += CellSize)
            {
                var line = new Line()
                {
                    X1 = x,
                    X2 = x,
                    Y1 = 0,
                    Y2 = Height,
                    Stroke = Color,
                    StrokeThickness = Thickness
                };
                Children.Add(line);
            }

            for (int y = 0; y <= Height; y += CellSize)
            {
                var line = new Line()
                {
                    X1 = 0,
                    X2 = Width,
                    Y1 = y,
                    Y2 = y,
                    Stroke = Color,
                    StrokeThickness = Thickness
                };
                Children.Add(line);
            }
        }

        public void UpdateVisuals(Brush newColor = null, double? newThickness = null, double? newOpacity = null)
        {
            foreach (var child in Children)
            {
                if (child is Line line)
                {
                    line.Stroke = newColor ?? line.Stroke;
                    line.StrokeThickness = newThickness ?? line.StrokeThickness;
                }
            }
            Thickness = newThickness ?? Thickness;
            Opacity = newOpacity ?? Opacity;
        }

        public void UpdateCellSize(int newCellSize)
        {
            CellSize = newCellSize;
            Children.Clear();
            Build();
        }

        public void UpdateGridSize(Size newSize)
        {
            Width = newSize.Width;
            Height = newSize.Height;
            Children.Clear();
            Build();
        }
    }
}