namespace Darakombi
{
    public interface IManager
    {
        public GlobalContext Context { get; set; }
        public UIElement HUD { get; set; }
        public bool Paused { get; set; }

        void Start();
        void Update(double dt);
        void End();

        StringBuilder DynamicDebug { get; init; }
        StringBuilder EventDebug { get; init; }
        StringBuilder StaticDebug { get; init; }
        public StringBuilder ModeDebug { get; set; }
    }
}