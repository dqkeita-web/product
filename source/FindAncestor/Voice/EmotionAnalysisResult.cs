namespace FindAncestor.Voice
{
    public class EmotionAnalysisResult
    {
        public List<SegmentEmotion> Segments { get; set; } = new();

        // ★ 追加（全体代表値）
        public string PrimaryEmotion =>
            Segments.Count > 0 ? Segments[0].Emotion : "neutral";

        public double Score =>
            Segments.Count > 0 ? Segments[0].Intensity : 0.5;

        public class SegmentEmotion
        {
            public string Text { get; set; } = "";
            public string Emotion { get; set; } = "neutral";
            public double Intensity { get; set; } = 0.5;
            public double Emphasis { get; set; }
            public double Pause { get; set; }
        }
    }
}