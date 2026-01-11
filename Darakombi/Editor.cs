using System.IO;
using System.Text.Json;

namespace Darakombi
{
    public class Editor : FrameworkElement
    {
        public readonly record struct TileColor(byte R, byte G, byte B);
        public readonly record struct Tile(int X, int Y, TileColor Color);
        public readonly Dictionary<(int x, int y), Tile> Tiles = [];

        public readonly List<Tile> BufferTiles = [];
        public readonly List<TileColor> BufferColors = [];

        public readonly record struct MapData(int Width, int Height, List<Tile> Tiles);

        private readonly Dictionary<TileColor, StreamGeometry> TileLayers = [];
        private readonly Dictionary<TileColor, HashSet<(int X, int Y)>> TileColors = [];

        public int CellSize;
        public Rect Viewport;

        public bool DrawOver { get; set; } = false;
        public bool Saved { get; set; } = false;

        public bool NeedsRedraw = false;

        public Editor(int cellSize)
        {
            CellSize = cellSize;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);

            CacheMode = new BitmapCache()
            {
                EnableClearType = true,
                SnapsToDevicePixels = true,
                RenderAtScale = 1.0
            };
        }

        private static (int X, int Y) GetCell(Tile tile) => new(tile.X, tile.Y);
        private static SolidColorBrush GetBrush(TileColor tileColor) => new()
        { Color = Color.FromRgb(tileColor.R, tileColor.G, tileColor.B) };

        public void LayerTiles(TileColor color)
        {
            if (!TileColors.TryGetValue(color, out var tiles) || tiles.Count == 0)
            {
                TileLayers.Remove(color);
                TileColors.Remove(color);
                return;
            }

            var layer = new StreamGeometry();
            using var context = layer.Open();
            foreach (var (X, Y) in tiles)
            {
                context.BeginFigure(new(X, Y), true, true);
                context.PolyLineTo(
                    [new(X + CellSize, Y),
                    new(X + CellSize, Y + CellSize),
                    new(X, Y + CellSize)],
                    false, false);
            }
            layer.Freeze();
            TileLayers[color] = layer;
        }

        public void Add(Tile tile)
        {
            var cell = GetCell(tile);
            if (Tiles.TryGetValue(cell, out _) && !DrawOver) return;
            Tiles[cell] = tile;
            BufferTiles.Add(tile);
            NeedsRedraw = true;
        }

        public void CommitBuffer()
        {
            if (BufferTiles.Count == 0) return;

            BufferColors.Clear();
            foreach (var tile in BufferTiles)
            {
                if (!TileColors.TryGetValue(tile.Color, out _))
                    TileColors[tile.Color] = [];
                TileColors[tile.Color].Add(new(tile.X, tile.Y));
                BufferColors.Add(tile.Color);
            }

            foreach (var color in BufferColors)
                LayerTiles(color);

            BufferTiles.Clear();
            InvalidateVisual();
        }

        public void Remove(Tile tile)
        {
            if (Tiles.Remove(GetCell(tile)))
            {
                if (TileColors.TryGetValue(tile.Color, out var tiles))
                {
                    tiles.Remove(new(tile.X, tile.Y));
                    LayerTiles(tile.Color);
                    InvalidateVisual();
                }
            }
        }

        public void Clear()
        {
            Tiles.Clear();
            TileColors.Clear();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            foreach (var (color, tiles) in TileLayers)
                drawingContext.DrawGeometry(GetBrush(color), null, tiles);
            foreach (var tile in BufferTiles)
                drawingContext.DrawRectangle(GetBrush(tile.Color), null, 
                    new(tile.X, tile.Y, CellSize, CellSize));
        }

        private readonly JsonSerializerOptions SaveOptions = new() { WriteIndented = true };
        public void Save(string path, Size mapSize)
        {
            var mapData = new MapData((int)mapSize.Width, (int)mapSize.Height, []);

            foreach (var tile in Tiles.Values)
            {
                if (GetBrush(tile.Color) is SolidColorBrush brush)
                {
                    mapData.Tiles.Add(
                        new(
                            tile.X,
                            tile.Y,
                            new(
                                brush.Color.R,
                                brush.Color.G,
                                brush.Color.B)));
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
                Add(new Tile(tile.X, tile.Y, tile.Color));
            }
            InvalidateVisual();
        }
    }
}