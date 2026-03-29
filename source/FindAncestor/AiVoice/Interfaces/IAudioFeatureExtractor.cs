// AiVoice/Interfaces/IAudioFeatureExtractor.cs
namespace FindAncestor.AiVoice.Interfaces { public interface IAudioFeatureExtractor { float[][] ExtractFeatures(byte[] wavData); } }