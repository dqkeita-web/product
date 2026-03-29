// パス: source\FindAncestor\Voice\VoiceConfigLoader.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FindAncestor.Voice.Models;

namespace FindAncestor.Voice
{
    public class VoiceConfig
    {
        public Dictionary<string, VoiceProfile> Characters { get; set; } = new();

        public GlobalVoiceSettings GlobalSettings { get; set; } = new();

        // ★ 追加：TTSマッピング
        public TtsMappingSettings TtsMapping { get; set; } = new();

        public VoiceProfile GetCharacter(string name)
        {
            if (Characters.TryGetValue(name, out var profile))
                return profile;

            if (Characters.TryGetValue("default", out var def))
                return def;

            throw new KeyNotFoundException($"キャラクターが見つかりません: {name}");
        }
    }

    // =========================
    // GlobalSettings
    // =========================
    public class GlobalVoiceSettings
    {
        public int RandomSeed { get; set; } = 42;
        public double RandomnessPercent { get; set; } = 0.03;
        public int HistoryLimit { get; set; } = 10;
        public string EmotionSmoothing { get; set; } = "easeInOut";

        public double MaxPitchDeltaPerLine { get; set; } = 0.15;
        public double MaxSpeedDeltaPerLine { get; set; } = 0.15;
        public double MaxIntonationDeltaPerLine { get; set; } = 0.2;
    }

    // =========================
    // ★ 追加：TTSマッピング設定
    // =========================
    public class TtsMappingSettings
    {
        public string Engine { get; set; } = "VOICEVOX";

        public int DefaultVoiceId { get; set; } = 1;

        public double PitchScaleBase { get; set; } = 1.0;
        public double SpeedScaleBase { get; set; } = 1.0;
        public double IntonationScaleBase { get; set; } = 1.0;
        public double VolumeScaleBase { get; set; } = 1.0;
    }

    public static class VoiceConfigLoader
    {
        public static VoiceConfig Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"voice_config.json が見つかりません: {path}");

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;

            var config = new VoiceConfig();

            // =========================
            // GlobalSettings
            // =========================
            if (root.TryGetProperty("globalSettings", out var global))
            {
                config.GlobalSettings = new GlobalVoiceSettings
                {
                    RandomSeed = GetInt(global, "randomSeed", 42),
                    RandomnessPercent = GetDouble(global, "randomnessPercent", 0.03),
                    HistoryLimit = GetInt(global, "historyLimit", 10),
                    EmotionSmoothing = GetString(global, "emotionSmoothing", "easeInOut"),
                    MaxPitchDeltaPerLine = GetDouble(global, "maxPitchDeltaPerLine", 0.15),
                    MaxSpeedDeltaPerLine = GetDouble(global, "maxSpeedDeltaPerLine", 0.15),
                    MaxIntonationDeltaPerLine = GetDouble(global, "maxIntonationDeltaPerLine", 0.2)
                };
            }

            // =========================
            // ★ TTS Mapping
            // =========================
            if (root.TryGetProperty("ttsMapping", out var tts))
            {
                config.TtsMapping = new TtsMappingSettings
                {
                    Engine = GetString(tts, "engine", "VOICEVOX"),
                    DefaultVoiceId = GetInt(tts, "defaultVoiceId", 1),
                    PitchScaleBase = GetDouble(tts, "pitchScaleBase", 1.0),
                    SpeedScaleBase = GetDouble(tts, "speedScaleBase", 1.0),
                    IntonationScaleBase = GetDouble(tts, "intonationScaleBase", 1.0),
                    VolumeScaleBase = GetDouble(tts, "volumeScaleBase", 1.0)
                };
            }

            // =========================
            // Characters
            // =========================
            if (!root.TryGetProperty("characters", out var charactersElem) ||
                charactersElem.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidDataException("voice_config.json に 'characters' 配列がありません");
            }

            foreach (var ch in charactersElem.EnumerateArray())
            {
                var profile = ParseVoiceProfile(ch, config.GlobalSettings);
                profile.Validate();
                config.Characters[profile.Name] = profile;
            }

            return config;
        }

        private static VoiceProfile ParseVoiceProfile(JsonElement e, GlobalVoiceSettings global)
        {
            var profile = new VoiceProfile
            {
                Name = GetString(e, "name", "default"),
                BasePitch = GetDouble(e, "basePitch", 1.0),
                BaseSpeed = GetDouble(e, "baseSpeed", 1.0),
                BaseIntonation = GetDouble(e, "baseIntonation", 1.0),
                BaseVolume = GetDouble(e, "baseVolume", 1.0),
                Tone = GetString(e, "tone", "neutral"),
                SpeakingStyle = GetString(e, "speakingStyle", "normal"),
                Randomness = GetDouble(e, "randomnessPercent", global.RandomnessPercent)
            };

            // EmotionBias
            if (e.TryGetProperty("emotionBias", out var biasElem) &&
                biasElem.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in biasElem.EnumerateObject())
                {
                    profile.EmotionBias[prop.Name] = prop.Value.GetDouble();
                }
            }

            // Range
            profile.PitchRange = ParseRange(e, "pitchRange", 0.8, 1.2);
            profile.SpeedRange = ParseRange(e, "speedRange", 0.8, 1.2);
            profile.IntonationRange = ParseRange(e, "intonationRange", 0.8, 1.2);
            profile.VolumeRange = ParseRange(e, "volumeRange", 0.8, 1.2);

            // Phoneme
            if (e.TryGetProperty("phonemeStyle", out var ph))
            {
                profile.Phoneme = new PhonemeStyle
                {
                    VowelStretch = GetDouble(ph, "vowelStretch", 1.0),
                    ConsonantStrength = GetDouble(ph, "consonantStrength", 1.0),
                    SentenceEnding = GetString(ph, "endingStyle", ""),
                    EndingEmphasis = 1.0
                };
            }

            return profile;
        }

        private static ValueRange ParseRange(JsonElement parent, string name, double minDef, double maxDef)
        {
            if (parent.TryGetProperty(name, out var e))
            {
                return new ValueRange(
                    GetDouble(e, "min", minDef),
                    GetDouble(e, "max", maxDef));
            }
            return new ValueRange(minDef, maxDef);
        }

        private static string GetString(JsonElement e, string name, string def)
        {
            return e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
                ? v.GetString() ?? def
                : def;
        }

        private static double GetDouble(JsonElement e, string name, double def)
        {
            return e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
                ? v.GetDouble()
                : def;
        }

        private static int GetInt(JsonElement e, string name, int def)
        {
            return e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number
                ? v.GetInt32()
                : def;
        }
    }
}