// AiVoice/Services/SimpleAiVoiceModel.cs
using System.Linq;
using FindAncestor.AiVoice.Interfaces;
namespace FindAncestor.AiVoice.Services
{
    public class SimpleAiVoiceModel : IAiVoiceModel { public string Predict(float[][] features) { if (features == null || features.Length == 0) return "[No Audio]"; var avg = features.SelectMany(f => f).Select(v => System.Math.Abs(v)).DefaultIfEmpty(0).Average(); if (avg < 0.01) return "無音"; if (avg < 0.05) return "小さい音"; return "発話あり"; } }
}