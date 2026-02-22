namespace FindAncestor.Models
{
    public class AspectRatioItem
    {
        // UI表示用
        public string DisplayText { get; set; } = "";

        // 内部計算用（double）
        public double Value { get; set; }

        public AspectRatioItem(string displayText, double value)
        {
            DisplayText = displayText;
            Value = value;
        }
    }
}