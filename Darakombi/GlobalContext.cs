namespace Darakombi
{
    public sealed class GlobalContext
    {
        public Rect Viewport { get; set; }
        public Map Map { get; set; }
        public ScaleTransform ScaleTransform { get; set; }
        public TranslateTransform TranslateTransform { get; set; }
        public HashSet<Key> PressedKeys { get; set; }
    }
}