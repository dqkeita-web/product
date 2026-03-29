using System;

namespace FindAncestor.Voice
{
    public class VoiceVariationService
    {
        private readonly Random _rand = new();

        public VoiceParameter Apply(
            VoiceParameter baseParam,
            string emotion,
            double intensity,
            string style)
        {
            // ★ 変更：影響を小さく
            float pitch = baseParam.Pitch;
            float speed = baseParam.Speed;

            pitch += (float)(intensity * 0.05);
            speed += (float)(intensity * 0.03);

            switch (style)
            {
                case "happy":
                    pitch += 0.04f;
                    break;
                case "sad":
                    speed -= 0.04f;
                    break;
                case "cute":
                    pitch += 0.011f;     // 上げすぎない
                    speed -= 0.02f;     // 少しゆっくり
                    break;
            }
            // ランダム揺らぎ（重要）
            pitch += (float)(_rand.NextDouble() * 0.1 - 0.05);
            speed += (float)(_rand.NextDouble() * 0.1 - 0.05);

            baseParam.Pitch = pitch;
            baseParam.Speed = speed;

            return baseParam;
        }
    }
}