using System;
using System.Collections.Generic;
using FindAncestor.Voice.Models;

namespace FindAncestor.Voice.Services
{
    public class CharacterVoiceService
    {
        private readonly VoiceHistoryService _history = new();

        // =========================
        // メイン処理
        // =========================
        public VoiceSynthesisResult Process(
            VoiceProfile profile,
            List<VoiceSegment> segments,
            int seed = 0)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (segments == null || segments.Count == 0)
                throw new ArgumentException("segments empty");

            profile.Validate();

            var rand = new Random(seed);

            var result = new VoiceSynthesisResult
            {
                Segments = new List<VoiceSynthesisSegment>()
            };

            double prevPitch = profile.BasePitch;

            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];

                // =========================
                // Emotion補正
                // =========================
                double emotionFactor = GetEmotionFactor(profile, seg.Emotion);

                // =========================
                // 基本パラメータ
                // =========================
                double pitch = profile.BasePitch * emotionFactor;
                double speed = profile.BaseSpeed * (1.0 + seg.Intensity * 0.2);
                double intonation = profile.BaseIntonation * (1.0 + seg.Emphasis * 0.3);
                double volume = profile.BaseVolume;

                // =========================
                // ease補間（前フレームとの滑らか化）
                // =========================
                pitch = Ease(prevPitch, pitch, 0.6);
                prevPitch = pitch;

                // =========================
                // 履歴補正（急変防止）
                // =========================
                pitch = _history.SmoothPitch(pitch);

                // =========================
                // ランダム揺らぎ（±）
                // =========================
                pitch *= 1.0 + RandomRange(rand, profile.Randomness);
                speed *= 1.0 + RandomRange(rand, profile.Randomness);
                intonation *= 1.0 + RandomRange(rand, profile.Randomness);

                // =========================
                // Clamp
                // =========================
                pitch = Clamp(pitch, profile.PitchRange);
                speed = Clamp(speed, profile.SpeedRange);
                intonation = Clamp(intonation, profile.IntonationRange);
                volume = Clamp(volume, profile.VolumeRange);

                // =========================
                // 音素変換
                // =========================
                string text = ApplyPhoneme(profile.Phoneme, seg.Text);

                result.Segments.Add(new VoiceSynthesisSegment
                {
                    Text = text,
                    Pitch = pitch,
                    Speed = speed,
                    Intonation = intonation,
                    Volume = volume,
                    Pause = seg.Pause
                });
            }

            return result;
        }

        // =========================
        // Emotion補正
        // =========================
        private double GetEmotionFactor(VoiceProfile profile, string emotion)
        {
            if (string.IsNullOrEmpty(emotion))
                return 1.0;

            if (profile.EmotionBias != null &&
                profile.EmotionBias.TryGetValue(emotion, out var v))
                return v;

            return 1.0;
        }

        // =========================
        // ease補間
        // =========================
        private double Ease(double current, double target, double t)
        {
            // easeInOut簡易版
            return current + (target - current) * t;
        }

        // =========================
        // ランダム
        // =========================
        private double RandomRange(Random rand, double range)
        {
            return (rand.NextDouble() * 2.0 - 1.0) * range;
        }

        // =========================
        // Clamp
        // =========================
        private double Clamp(double value, ValueRange range)
        {
            if (value < range.Min) return range.Min;
            if (value > range.Max) return range.Max;
            return value;
        }

        // =========================
        // 音素変換
        // =========================
        private string ApplyPhoneme(PhonemeStyle phoneme, string text)
        {
            if (phoneme == null || string.IsNullOrEmpty(text))
                return text;

            // 語尾追加
            if (!string.IsNullOrEmpty(phoneme.SentenceEnding))
            {
                text += phoneme.SentenceEnding;
            }

            return text;
        }
    }

    // =========================
    // 入力セグメント
    // =========================
    public class VoiceSegment
    {
        public string Text { get; set; } = "";
        public string Emotion { get; set; } = "";
        public double Intensity { get; set; }
        public double Emphasis { get; set; }
        public double Pause { get; set; }
    }

    // =========================
    // 出力
    // =========================
    public class VoiceSynthesisResult
    {
        public List<VoiceSynthesisSegment> Segments { get; set; } = new();
    }

    public class VoiceSynthesisSegment
    {
        public string Text { get; set; } = "";
        public double Pitch { get; set; }
        public double Speed { get; set; }
        public double Intonation { get; set; }
        public double Volume { get; set; }
        public double Pause { get; set; }
    }

    // =========================
    // 履歴補正
    // =========================
    public class VoiceHistoryService
    {
        private double _lastPitch = 1.0;

        public double SmoothPitch(double current)
        {
            double smoothed = (_lastPitch * 0.7) + (current * 0.3);
            _lastPitch = smoothed;
            return smoothed;
        }
    }
}