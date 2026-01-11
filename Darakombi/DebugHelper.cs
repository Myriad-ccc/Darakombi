namespace Darakombi
{
    public static class DebugHelper
    {
        public static TimeSpan Uptime { get; set; }
        public static double FramesPerSecond { get; set; }
        public static double DeltaTime { get; set; }

        public static double VelocityX { get; set; }
        public static double VelocityY { get; set; }

        public static double PlayerX { get; set; }
        public static double PlayerY { get; set; }

        public static double MousePosX { get; set; }
        public static double MousePosY { get; set; }
        public static double ZoomFactor { get; set; }

        public static string GetApp()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"upt:{Uptime:hh\\:mm\\:ss}");
            sb.AppendLine($"fps:{FramesPerSecond:F0}");
            sb.Append($"dt:{DeltaTime:F3}");
            return sb.ToString();
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
            sb.Append($"zoom:{ZoomFactor:F2}");
            return sb.ToString();
        }
    }
}
