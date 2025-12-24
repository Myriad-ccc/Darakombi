namespace CombatWordle
{
    public static class QOL
    {
        private static readonly Random random = new Random();

        public static SolidColorBrush RandomColor() => new(Color.FromArgb(255, (byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));
        public static SolidColorBrush RGB(int r) => new(Color.FromArgb(255, (byte)r, (byte)r, (byte)r));

        public static void WriteOut(object obj) => MessageBox.Show($"{obj}");
    }
}
