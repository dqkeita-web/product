namespace FindAncestor.Voice
{
    public class CharacterStyleService
    {
        public string ApplyAmamiyaStyle(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "ねぇ…来てくれたの？うれしいなぁ…";

            text = text.Replace("です", "だよぉ");
            text = text.Replace("ます", "するねぇ");

            return text + "…えへへ♪";
        }
    }
}