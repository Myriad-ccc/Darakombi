namespace CombatWordle
{
    public class Player : Entity
    {
        public PlayerColor Color { get; }

        public Player(int width = 2, int height = 2) : base(width, height)
        {
            Color = PlayerColor.Default;
        }
    }

    public enum PlayerColor
    {
        Default,
        //TODO
    }
}
