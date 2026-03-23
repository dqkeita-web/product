

using System.Windows.Media;

namespace FindAncestor.Editor
{
    public class OverlayItem
    {
        public string Text { get; set; } = "テキスト";
        public double X { get; set; } = 100;
        public double Y { get; set; } = 100;

        public double Start { get; set; } = 0;
        public double End { get; set; } = 9999;

        public double FontSize { get; set; } = 32;

        // 🔥 色（今回追加）
        public Brush Foreground { get; set; } = Brushes.White;

        // 🔥 将来用（消すな）
        public string? FilePath { get; set; }
    }
}
