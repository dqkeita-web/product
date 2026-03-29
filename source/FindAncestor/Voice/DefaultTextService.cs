namespace FindAncestor.Voice
{
    public class DefaultTextService
    {
        public string GetDefaultLine()
        {
            return "ご主人さま〜♡ 今日もがんばっててえらいねっ♪ えへへ、ぎゅーしてあげる〜♡";
        }

        public bool Validate(string? text)
        {
            return !string.IsNullOrWhiteSpace(text);
        }

        public string Resolve(string? input)
        {
            return Validate(input) ? input! : GetDefaultLine();
        }
    }
}