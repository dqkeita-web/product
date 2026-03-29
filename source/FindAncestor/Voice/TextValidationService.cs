// ファイル: TextValidationService.cs
using System.Text.RegularExpressions;

namespace FindAncestor.Voice
{
    public class TextValidationService
    {
        public string Validate(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            // 長すぎ防止
            if (text.Length > 120)
                text = text.Substring(0, 120);

            // 記号統一
            text = text.Replace("。。", "。")
                .Replace("、、", "、")
                .Replace("!!", "！")
                .Replace("??", "？");

            // 間を強制（人間化）
            text = Regex.Replace(text, "(よ|ね|かな|かも)$", "$1？");

            return text.Trim();
        }
    }
}