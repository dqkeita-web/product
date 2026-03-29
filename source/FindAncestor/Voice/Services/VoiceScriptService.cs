// ファイル: VoiceScriptService.cs
using System.Collections.Generic;

namespace FindAncestor.Voice
{
    public static class VoiceScriptService
    {
        public static List<string> GetDemoLines()
        {
            return new List<string>
            {
                "あ、はじめまして…えっと、その、今日ちょっと緊張してて…うまく話せるかわかんないけど、よろしくね",
                "ねえねえ、今日さ、めっちゃ面白いことあったんだけど聞いてよ、ほんと笑いすぎてお腹痛くなったんだから",
                "ねえ、ちょっとだけこっち来て…今日なんかね、すごく会いたかったんだ、こうやって話せると安心する",
                "なんか…こういうの初めてだから、ちょっとドキドキしてる…でも、あなたと一緒なら大丈夫な気がする",
                "もう、そんなこと言われたら照れるってば…でも、ちょっとだけ嬉しいかも、ほんとずるいよね"
            };
        }
    }
}