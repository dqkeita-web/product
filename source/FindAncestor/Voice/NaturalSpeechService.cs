namespace FindAncestor.Voice
{
    public class NaturalSpeechService
    {
        public string ToNatural(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "ねぇ…ちょっと話そ？";

            text = text.Replace("です", "だよ");
            text = text.Replace("ます", "するよ");

            return text;
        }
    }
}