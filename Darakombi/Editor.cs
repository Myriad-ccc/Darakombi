using System.IO;
using System.Text.Json;

namespace Darakombi
{
    public class Editor : FrameworkElement
    {
        public readonly record struct MapData(int Width, int Height, List<TileData> Tiles);
        public readonly record struct TileData(int X, int Y, byte R, byte G, byte B);

        public readonly record struct Tile((int X, int Y) Cell, Brush Color);
        public readonly Dictionary<(int x, int y), Tile> Tiles = [];

        public int CellSize;
        public Rect Viewport;

        public bool DrawOver { get; set; } = false;

        public Editor(int cellSize)
        {
            CellSize = cellSize;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
        }

        public void Add(Tile tile)
        {
            if (!Tiles.TryGetValue(tile.Cell, out _) || DrawOver)
                Tiles[tile.Cell] = tile;
        }

        public void Remove((int x, int y) cell) => Tiles.Remove(cell);

        public void Update(Rect viewport)
        {
            Viewport = viewport;
            InvalidateVisual();
        }

        public void Clear() => Tiles.Clear();

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
                    if (Tiles.TryGetValue((r, c), out var tile))
                    {
                        drawingContext.DrawRectangle(tile.Color, null,
                            new Rect(
                                new Point(r, c),
                                new Size(CellSize + 2.5,CellSize + 2.5)));
                        QOL.D($"Drew tile at {r},{c} of color {tile.Color}");
                    }
                }
            }
        }

        private readonly JsonSerializerOptions SaveOptions = new() { WriteIndented = true };
        public void Save(string path, Size mapSize)
        {
            var mapData = new MapData((int)mapSize.Width, (int)mapSize.Height, []);

            foreach (var tile in Tiles.Values)
            {
                if (tile.Color is SolidColorBrush brush)
                {
                    mapData.Tiles.Add(
                        new(
                            tile.Cell.X,
                            tile.Cell.Y,
                            brush.Color.R,
                            brush.Color.G,
                            brush.Color.B));
                }
            }
            string json = JsonSerializer.Serialize(mapData, SaveOptions);
            File.WriteAllText(path, json);
        }

        public Action<Size> ResizeMap;

        public void Load(string path)
        {
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            var mapData = JsonSerializer.Deserialize<MapData>(json);

            ResizeMap?.Invoke(new(mapData.Width, mapData.Height));

            foreach (var tile in mapData.Tiles)
            {
                var color = new SolidColorBrush(Color.FromRgb(tile.R, tile.G, tile.B));
                Add(new Tile((tile.X, tile.Y), color));
            }
            InvalidateVisual();
        }
    }
}