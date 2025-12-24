namespace CombatWordle
{
    public class GameState
    {
        private readonly Random random = new();

        public int Rows { get; }
        public int Cols { get; }
        public GameGrid Grid { get; }
        public Direction Direction { get; private set; }
        public bool GameOver { get; private set; }
        public int Score { get; private set; }

        public Player Player { get; private set; }
        //public Enemy Enemy { get; private set; }
        //public Wall Wall { get; private set; }

        private List<Player> players = [];
        //private List<Enemy> enemies = [];
        //private List<Wall> walls = [];

        public GameState(int gridSize = 1)
        {
            Rows = gridSize * 6; //3
            Cols = gridSize * 8; //4
            Grid = new GameGrid(Rows, Cols);
            Direction = Direction.Right;

            AddPlayer();
            //AddEnemy();
            //AddWall();
        }

        private double AirBuffer = 0;
        public async Task Update()
        {
            AirBuffer++;
            if (Player.VY >= 0.1)
            {
                Player.VY -= 0.1;
                AirBuffer -= 0.1;
            }

            if (AirBuffer >= 3)
            {
                foreach (Player player in players)
                    Grid.EntityGravitate(player, GridValue.Player);
                AirBuffer = 0;
            }
        }

        private void AddPlayer()
        {
            Player = new Player(10, 10);
            players.Add(Player);
            Grid.BottomCenterEntity(Player, GridValue.Player);
            Grid.UpdateGridValue(Player, GridValue.Player);
        }

        private async void MovePlayer(int rowOffset, int colOffset) => Grid.MoveEntityBy(Player, GridValue.Player, rowOffset, colOffset);

        public void MovePlayerLeft() => MovePlayer(0, -1);
        public void MovePlayerRight() => MovePlayer(0, 1);
        public void MovePlayerUp() => MovePlayer(-1, 0);
        public void MovePlayerDown() => MovePlayer(1, 0);

        //private void AddWall()
        //{
        //    Wall = new Wall(random.Next(4, 16), random.Next(2, 16));
        //    walls.Add(Wall);

        //    Grid.UpdateGridValue(Wall, GridValue.Wall);
        //}

        //private void AddEnemy()
        //{
        //    Enemy = new Enemy(5, 5);
        //    enemies.Add(Enemy);

        //    do
        //    {
        //        Enemy.Offset = new Position(Player.Offset.Row + random.Next(-Rows / 2, Rows / 2), Player.Offset.Col + random.Next(-Cols / 2, Cols / 2));
        //    }
        //    while (!Grid.EntityCanFit(Enemy));

        //    Grid.UpdateGridValue(Enemy, GridValue.Enemy);
        //}

        public Entity EntityAtPos(int r, int c)
        {
            foreach (Player player in players)
                if (player.IsAt(r, c))
                    return player;
            //foreach (Enemy enemy in enemies)
            //    if (enemy.IsAt(r, c))
            //        return enemy;
            //foreach (Wall wall in walls)
            //    if (wall.IsAt(r, c))
            //        return wall;
            return null;
        }
    }
}