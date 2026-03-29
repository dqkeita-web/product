using System;

namespace FindAncestor.Voice
{
    public class VoiceParameter
    {
        public string Name { get; set; } = string.Empty;

        // ★ 追加（VOICEVOX用）
        public int SpeakerId { get; set; }

        public float Pitch { get; set; } = 0.0f;
        public float Speed { get; set; } = 0.95f;
        public float Volume { get; set; } = 1.0f;

        public string? Emotion { get; set; }

        public static VoiceParameter FromConfig(dynamic config)
        {
            return new VoiceParameter
            {
                Name = config.name,
                SpeakerId = config.speaker_id,
                Pitch = config.pitch ?? 1.0f,
                Speed = config.speed ?? 1.0f,
                Volume = config.volume ?? 1.0f,
                Emotion = config.emotion
            };
        }
    }
}