namespace Darakombi
{
    public static class ConsoleLogger
    {
        public static RichTextBox Logs;

        public static bool IncludeTimeStamps = true;

        private static Paragraph LastDiv => Logs.Document.Blocks.LastBlock as Paragraph;

        public static void Log(string text, bool append = false, Brush color = null)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            text = text.Replace(' ', ' ');
            color ??= Brushes.White;

            Paragraph div;
            if ((append && LastDiv != null))
            {
                div = LastDiv;
            }
            else
            {
                div = new()
                {
                    Padding = new(0),
                    Margin = new(0)
                };

                if (IncludeTimeStamps)
                {
                    var ts = new Run($"[{DateTime.Now:HH:mm:ss}]") { Foreground = Brushes.Gray };
                    var floater = new Floater(new Paragraph(ts))
                    {
                        Padding = new(0),
                        Margin = new(0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                    };
                    div.Inlines.Add(floater);
                }
            }

            var span = new Run(text) { Foreground = color };
            div.Inlines.Add(span);

            Logs.Document.Blocks.Add(div);

            Logs.ScrollToEnd();
        }

        public static void LogTwo(string firstText, string secondText, Brush firstColor = null, Brush secondColor = null)
        {
            Log(firstText, false, firstColor);
            Log(secondText, true, secondColor);
        }

        public static void LogMany(string[] spans, Brush[] colors)
        {
            Log(spans[0], false, colors[0]);
            for (int i = 1; i < spans.Length; i++)
                if (!string.IsNullOrWhiteSpace(spans[i]))
                    Log(spans[i], true, colors[i]);
        }

        public static void LogInvalid(string text, Brush color = null) 
            => LogTwo(text, " not found.", Brushes.White, color);

        public static void LogNull(string text, Brush firstColor = null, Brush secondColor = null) //for objects
            => LogMany([text, " is ", "null."], [Brushes.White, firstColor, secondColor]);

        public static void Clear() => Logs.Document.Blocks.Clear();
    }
}