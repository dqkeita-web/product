using System;
using System.Collections.Generic;

namespace FindAncestor.Voice.Models
{
    public class VoiceProfile
    {
        // =========================
        // 基本情報
        // =========================
        public string Name { get; set; } = "Default";

        // =========================
        // 基本音声パラメータ
        // =========================
        public double BasePitch { get; set; } = 1.0;
        public double BaseSpeed { get; set; } = 1.0;
        public double BaseIntonation { get; set; } = 1.0;
        public double BaseVolume { get; set; } = 1.0;

        // =========================
        // キャラクター性
        // =========================
        public string Tone { get; set; } = "neutral"; // soft / cool / energetic / dark
        public string SpeakingStyle { get; set; } = "normal"; // calm / tsundere / shy / noble

        // =========================
        // 感情補正
        // key: emotion名（happy, sad, angry など）
        // value: 強度倍率
        // =========================
        public Dictionary<string, double> EmotionBias { get; set; } = new();

        // =========================
        // パラメータ変動範囲
        // =========================
        public ValueRange PitchRange { get; set; } = new(0.8, 1.2);
        public ValueRange SpeedRange { get; set; } = new(0.8, 1.2);
        public ValueRange IntonationRange { get; set; } = new(0.8, 1.2);
        public ValueRange VolumeRange { get; set; } = new(0.8, 1.2);

        // =========================
        // 音素・話し方傾向
        // =========================
        public PhonemeStyle Phoneme { get; set; } = new();

        // =========================
        // ランダム揺らぎ（再現性あり）
        // =========================
        public double Randomness { get; set; } = 0.03; // ±3%

        // =========================
        // 検証
        // =========================
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException("VoiceProfile: Name is required.");

            ClampRange(PitchRange);
            ClampRange(SpeedRange);
            ClampRange(IntonationRange);
            ClampRange(VolumeRange);

            BasePitch = Clamp(BasePitch, PitchRange);
            BaseSpeed = Clamp(BaseSpeed, SpeedRange);
            BaseIntonation = Clamp(BaseIntonation, IntonationRange);
            BaseVolume = Clamp(BaseVolume, VolumeRange);
        }

        private static void ClampRange(ValueRange range)
        {
            if (range.Min <= 0 || range.Max <= 0 || range.Min > range.Max)
                throw new InvalidOperationException("Invalid ValueRange.");
        }

        private static double Clamp(double value, ValueRange range)
        {
            return Math.Max(range.Min, Math.Min(range.Max, value));
        }
    }

    // =========================
    // 値範囲
    // =========================
    public class ValueRange
    {
        public double Min { get; set; }
        public double Max { get; set; }

        public ValueRange() { }

        public ValueRange(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }

    // =========================
    // 音素スタイル
    // =========================
    public class PhonemeStyle
    {
        // 母音の伸ばし
        public double VowelStretch { get; set; } = 1.0;

        // 子音の強さ
        public double ConsonantStrength { get; set; } = 1.0;

        // 語尾スタイル（例: だよねぇ / なのだ / わよ）
        public string SentenceEnding { get; set; } = "";

        // 語尾の強調度
        public double EndingEmphasis { get; set; } = 1.0;
    }
}