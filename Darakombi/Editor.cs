namespace Darakombi
{
    public class Editor : FrameworkElement
    {
        public readonly record struct EditorDTO((int X, int Y) Cell, Brush Color);
        public readonly Dictionary<(int x, int y), EditorDTO> EditorObjects = [];

        public int CellSize;
        public Rect Viewport;

        public bool DrawOver { get; set; } = false;

        public Editor(int cellSize) => CellSize = cellSize;

        public void Add(EditorDTO obj)
        {
            if (!EditorObjects.TryGetValue(obj.Cell, out _) || DrawOver)
                EditorObjects[obj.Cell] = obj;
        }

        public void Remove((int x, int y) cell) => EditorObjects.Remove(cell);

        public void Update(Rect viewport)
        {
            Viewport = viewport;
            InvalidateVisual();
        }

        public void Clear() => EditorObjects.Clear();

        protected override void OnRender(DrawingContext drawingContext)
        {
            int left = (int)Math.Floor(Viewport.X / CellSize) * CellSize;
            int top = (int)Math.Floor(Viewport.Y / CellSize) * CellSize;
            int right = (int)Math.Ceiling(Viewport.Right / CellSize) * CellSize;
            int bottom = (int)Math.Ceiling(Viewport.Bottom / CellSize) * CellSize;

            for (int r = left; r <= right; r += CellSize)
            {
                for (int c = top; c <= bottom; c += CellSize)
                {
                    if (EditorObjects.TryGetValue((r, c), out var obj))
                    {
                        drawingContext.DrawRectangle(obj.Color, null,
                            new Rect(
                                new Point(obj.Cell.X, obj.Cell.Y),
                                new Size(CellSize + 2.5, CellSize + 2.5)));
                    }
                }
            }
        }
    }
}