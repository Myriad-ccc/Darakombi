namespace Darakombi
{
    public interface IManager
    {
        GlobalContext Context { get; internal set; }

        void Start();
        void Move(double dt);
        void Update(double dt, Rect viewport);
        void Stop();

        public StringBuilder DynamicDebug { get; set; }
        public StringBuilder EventDebug { get; set; }
    }
}