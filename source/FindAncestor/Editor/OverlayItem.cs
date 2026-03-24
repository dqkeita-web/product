namespace FindAncestor.Editor
{
    using System.Windows.Media;

    public class OverlayItem : BindableBase
    {
        private Brush _foreground = Brushes.Black;
        public Brush Foreground
        {
            get => _foreground;
            set => Set(ref _foreground, value);
        }
        private string _text = "";
        public string Text { get => _text; set => Set(ref _text, value); }

        private bool _isVisible = true;
        public bool IsVisible { get => _isVisible; set => Set(ref _isVisible, value); }


        private double _opacity = 1;
        public double Opacity { get => _opacity; set => Set(ref _opacity, value); }

        private double _fontSize = 32;
        public double FontSize { get => _fontSize; set => Set(ref _fontSize, value); }

        private string _fontFamily = "Segoe UI";
        public string FontFamily { get => _fontFamily; set => Set(ref _fontFamily, value); }

        // 🔥 これ追加（エラー原因）
        private double _start = 0;
        public double Start { get => _start; set => Set(ref _start, value); }

        private double _end = 9999;
        public double End { get => _end; set => Set(ref _end, value); }

        public double X { get; set; } = 100;
        public double Y { get; set; } = 100;
    }
}