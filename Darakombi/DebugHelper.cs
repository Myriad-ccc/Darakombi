namespace Darakombi
{
    public static class DebugHelper
    {
        public static double VelocityX { get; set; }
        public static double VelocityY { get; set; }

        public static double PlayerX { get; set; }
        public static double PlayerY { get; set; }

        public static int Tiles { get; set; }
        public static int BufferTiles { get; set; }

        public static double MousePosX { get; set; }
        public static double MousePosY { get; set; }
        public static double ZoomFactor { get; set; }

        public static void ResetValues()
        {
            VelocityX = 0;
            VelocityY = 0;

            PlayerX = 0;
            PlayerY = 0;

            Tiles = 0;
            BufferTiles = 0;

            MousePosX = 0;
            MousePosY = 0;
            ZoomFactor = 0;
        }

        public static string GetGameDynamic()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"vx:{VelocityX:F1}");
            sb.Append($"vy:{VelocityY:F1}");
            return sb.ToString();
        }

        public static string GetGameEvent()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"px:{PlayerX:F1}");
            sb.Append($"py:{PlayerY:F1}");
            return sb.ToString();
        }

        public static string GetEditorEvent()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"mx:{MousePosX:F1}");
            sb.AppendLine($"my:{MousePosY:F1}");
            sb.AppendLine($"tile:{Tiles}");
            sb.AppendLine($"buf:{BufferTiles}");
            sb.Append($"zoom:{ZoomFactor:F2}");
            return sb.ToString();
        }
    }
}
