namespace CombatWordle
{
    public class Player : Entity
    {
        public PlayerColor Color { get; }

        public Player()
        {
            //Color = PlayerColor.Default;
        }
    }

    public enum PlayerColor
    {
        Default,
        //TODO
    }
}
