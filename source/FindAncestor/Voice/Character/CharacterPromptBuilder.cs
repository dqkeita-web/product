namespace FindAncestor.Voice.Character
{
    public class CharacterPromptBuilder
    {
        public string Build(string text, CharacterProfile profile)
        {
            return $"{profile.Personality} {profile.SpeakingStyle}：{text}";
        }
    }
}