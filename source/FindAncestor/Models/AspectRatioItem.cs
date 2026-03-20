namespace FindAncestor.Models
{
    public class AspectRatioItem
    {
        public AspectRatioItem(string displayText, double value)
        {
            DisplayText = displayText;
            Value = value;
        }

        public string DisplayText { get; set; }
        public double Value { get; set; }
    }
}