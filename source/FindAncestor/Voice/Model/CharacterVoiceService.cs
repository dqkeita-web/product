// パス: source\FindAncestor\Voice\CharacterVoiceService.cs
using System;
using System.Collections.Generic;
using FindAncestor.Voice.Models;

namespace FindAncestor.Voice
{
    public class CharacterVoiceService
    {
        private readonly VoiceConfig _config;
        private readonly Dictionary<string, Queue<VoiceParameter>> _history = new();
        private readonly Random _random;

        public CharacterVoiceService(VoiceConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = new Random(_config.GlobalSettings.RandomSeed);
        }

        public VoiceParameter CreateParameter(
            string characterName,
            string text,
            string emotion,
            float intensity)
        {
            var profile = _config.GetCharacter(characterName);

            // =========================
            // 1. Base
            // =========================
            double pitch = profile.BasePitch;
            double speed = profile.BaseSpeed;
            double intonation = profile.BaseIntonation;
            double volume = profile.BaseVolume;

            // =========================
            // 2. Emotion補正
            // =========================
            var bias = profile.EmotionBias.TryGetValue(emotion, out var b) ? b : 1.0;
            var strength = intensity * bias;

            ApplyEmotion(ref pitch, ref speed, ref intonation, emotion, strength);

            // =========================
            // 3. ease補間
            // =========================
            strength = EaseInOut(strength);

            // =========================
            // 4. Clamp
            // =========================
            pitch = Clamp(pitch, profile.PitchRange);
            speed = Clamp(speed, profile.SpeedRange);
            intonation = Clamp(intonation, profile.IntonationRange);
            volume = Clamp(volume, profile.VolumeRange);

            // =========================
            // 5. ランダム揺らぎ
            // =========================
            pitch *= RandomFactor(profile.Randomness);
            speed *= RandomFactor(profile.Randomness);
            intonation *= RandomFactor(profile.Randomness);

            // =========================
            // 6. 履歴補正（声ブレ防止）
            // =========================
            ApplyHistory(characterName, ref pitch, ref speed, ref intonation);

            // =========================
            // 7. TTSマッピング
            // =========================
            var tts = _config.TtsMapping;

            var param = new VoiceParameter
            {
                Name = characterName,
                SpeakerId = tts.DefaultVoiceId,
                Pitch = (float)((pitch - 1.0) * tts.PitchScaleBase),
                Speed = (float)(speed * tts.SpeedScaleBase),
                Volume = (float)(volume * tts.VolumeScaleBase),
                Emotion = emotion
            };

            SaveHistory(characterName, param);

            return param;
        }

        // =========================
        // Emotion補正
        // =========================
        private void ApplyEmotion(ref double pitch, ref double speed, ref double intonation, string emotion, double s)
        {
            switch (emotion)
            {
                case "happy":
                    pitch += 0.15 * s;
                    speed += 0.1 * s;
                    intonation += 0.2 * s;
                    break;

                case "sad":
                    pitch -= 0.15 * s;
                    speed -= 0.1 * s;
                    intonation -= 0.2 * s;
                    break;

                case "angry":
                    pitch += 0.2 * s;
                    speed += 0.15 * s;
                    intonation += 0.25 * s;
                    break;

                case "surprised":
                    pitch += 0.25 * s;
                    intonation += 0.3 * s;
                    break;
            }
        }

        // =========================
        // 履歴制御
        // =========================
        private void ApplyHistory(string name, ref double pitch, ref double speed, ref double intonation)
        {
            if (!_history.TryGetValue(name, out var queue) || queue.Count == 0)
                return;

            var last = queue.Peek();
            var g = _config.GlobalSettings;

            pitch = LimitDelta(last.Pitch, pitch, g.MaxPitchDeltaPerLine);
            speed = LimitDelta(last.Speed, speed, g.MaxSpeedDeltaPerLine);
            intonation = LimitDelta(last.Pitch, intonation, g.MaxIntonationDeltaPerLine);
        }

        private void SaveHistory(string name, VoiceParameter param)
        {
            if (!_history.TryGetValue(name, out var queue))
            {
                queue = new Queue<VoiceParameter>();
                _history[name] = queue;
            }

            queue.Enqueue(param);

            while (queue.Count > _config.GlobalSettings.HistoryLimit)
                queue.Dequeue();
        }

        // =========================
        // Utils
        // =========================
        private static double Clamp(double v, ValueRange r)
            => Math.Max(r.Min, Math.Min(r.Max, v));

        private static double LimitDelta(double prev, double current, double maxDelta)
        {
            var delta = current - prev;
            if (Math.Abs(delta) > maxDelta)
                return prev + Math.Sign(delta) * maxDelta;
            return current;
        }

        private double RandomFactor(double range)
        {
            return 1.0 + (_random.NextDouble() - 0.5) * 2 * range;
        }

        private static double EaseInOut(double t)
        {
            return t < 0.5
                ? 2 * t * t
                : 1 - Math.Pow(-2 * t + 2, 2) / 2;
        }
    }
}