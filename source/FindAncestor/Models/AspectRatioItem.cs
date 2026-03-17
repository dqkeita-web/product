namespace FindAncestor.Models
{
    public class AspectRatioItem(string displayText, double value)
    {
        // UI表示用
        public string DisplayText { get; set; } = displayText;

        // 内部計算用（double）
        public double Value { get; set; } = value;
    }
}