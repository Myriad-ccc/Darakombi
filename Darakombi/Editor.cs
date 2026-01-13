using System.IO;
using System.Text.Json;

namespace Darakombi
{
    public class Editor : FrameworkElement
    {
        public readonly record struct TileColor(byte R, byte G, byte B);
        private readonly Dictionary<TileColor, SolidColorBrush> BrushCache = [];
        public readonly record struct Tile(int X, int Y, TileColor Color);
        public readonly Dictionary<(int X, int Y), Tile> Tiles = [];
        private readonly record struct MapData(int Width, int Height, List<Tile> Tiles);

        private readonly Dictionary<(int X, int Y), Dictionary<TileColor, StreamGeometry>> ChunkLayers = [];
        private readonly Dictionary<(int X, int Y), Dictionary<TileColor, HashSet<(int X, int Y)>>> ChunkTiles = [];
        public readonly List<Tile> BufferTiles = [];
        private readonly HashSet<((int cx, int cy), TileColor color)> BufferChunks = [];

        private readonly int CellSize;
        private readonly int ChunkSize;

        public bool DrawOver { get; set; } = false;
        public bool Saved { get; set; } = false;

        public event Action<Size> ResizeMap;
        //public event Action<UIElement> RemoveElementFromCanvas;

        public Editor(int cellSize, int chunkSize)
        {
            CellSize = cellSize;
            ChunkSize = chunkSize;
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        }

        private (int X, int Y) GetChunkCell(int x, int y) => (x / ChunkSize, y / ChunkSize);
        private (int X, int Y) GetChunkCell((int x, int y) cell) => (cell.x / ChunkSize, cell.y / ChunkSize);
        private static (int X, int Y) GetCell(Tile tile) => new(tile.X, tile.Y);

        private SolidColorBrush GetBrush(TileColor color)
        {
            if (!BrushCache.TryGetValue(color, out var brush))
            {
                brush = new() { Color = Color.FromRgb(color.R, color.G, color.B) };
                brush.Freeze(); //...
                BrushCache[color] = brush;
            }
            return brush;
        }

        public void Add(Tile tile)
        {
            var cell = GetCell(tile);
            if (Tiles.TryGetValue(cell, out _) && !DrawOver) return;

            Tiles[cell] = tile;
            BufferTiles.Add(tile);
            //DebugManager.Tiles = Tiles.Count;
            //DebugManager.BufferTiles++;
        }
        public void Remove(Tile tile)
        {
            var tileCell = GetCell(tile);
            if (Tiles.Remove(tileCell))
            {
                var chunkCell = GetChunkCell(tileCell);
                if (ChunkTiles.TryGetValue(chunkCell, out var layers))
                    if (layers.TryGetValue(tile.Color, out var chunkTiles))
                        if (chunkTiles.Remove((tile.X, tile.Y)))
                        {
                            if (chunkTiles.Count == 0)
                                layers.Remove(tile.Color);
                            UpdateChunkLayer(chunkCell, tile.Color);
                            InvalidateVisual();
                        }
            }
        }
        public void Clear()
        {
            Tiles.Clear();
            BrushCache.Clear();
            ChunkTiles.Clear();
            ChunkLayers.Clear();
        }

        public void CommitBuffer()
        {
            if (BufferTiles.Count == 0) return;

            foreach (var tile in BufferTiles)
            {
                var chunkCell = GetChunkCell(tile.X, tile.Y);
                if (!ChunkTiles.TryGetValue(chunkCell, out _))
                    ChunkTiles[chunkCell] = [];
                if (!ChunkTiles[chunkCell].TryGetValue(tile.Color, out _))
                    ChunkTiles[chunkCell][tile.Color] = [];

                ChunkTiles[chunkCell][tile.Color].Add((tile.X, tile.Y));
                BufferChunks.Add((chunkCell, tile.Color));
            }

            foreach (var (cell, color) in BufferChunks)
                UpdateChunkLayer(cell, color);

            BufferTiles.Clear();
            BufferChunks.Clear();

            //DebugManager.BufferTiles = 0;
            InvalidateVisual();
        }

        public void UpdateChunkLayer((int x, int y) chunkCell, TileColor layerColor)
        {
            if (!ChunkLayers.TryGetValue(chunkCell, out _))
                ChunkLayers[chunkCell] = [];

            HashSet<(int X, int Y)> tiles = null;
            if (ChunkTiles.TryGetValue(chunkCell, out var layers))
                layers.TryGetValue(layerColor, out tiles);
            if (tiles == null || tiles.Count == 0)
            {
                ChunkTiles[chunkCell].Remove(layerColor);
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
            ChunkLayers[chunkCell][layerColor] = layer;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            foreach (var chunk in ChunkLayers.Values)
                foreach (var (color, geometry) in chunk)
                    drawingContext.DrawGeometry(GetBrush(color), null, geometry);
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

        public void Load(string path)
        {
            if (!File.Exists(path)) return;

            string json = File.ReadAllText(path);
            var mapData = JsonSerializer.Deserialize<MapData>(json);

            ResizeMap?.Invoke(new(mapData.Width, mapData.Height));

            foreach (var tile in mapData.Tiles)
                Add(new Tile(tile.X, tile.Y, tile.Color));
            InvalidateVisual();
        }
    }
}