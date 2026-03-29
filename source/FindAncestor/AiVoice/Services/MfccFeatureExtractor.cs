// AiVoice/Services/MfccFeatureExtractor.cs
using System;
using System.Collections.Generic;
using System.IO;
using FindAncestor.AiVoice.Interfaces;
using NAudio.Wave;
namespace FindAncestor.AiVoice.Services
{
    public class MfccFeatureExtractor : IAudioFeatureExtractor { public float[][] ExtractFeatures(byte[] wavData) { using var ms = new MemoryStream(wavData); using var reader = new WaveFileReader(ms); var sampleProvider = reader.ToSampleProvider(); var samples = new List<float>(); float[] buffer = new float[1024]; int read; while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0) { for (int i = 0; i < read; i++) samples.Add(buffer[i]); } return SimpleFrameSplit(samples.ToArray(), 400, 160); } private float[][] SimpleFrameSplit(float[] signal, int frameSize, int hopSize) { var frames = new List<float[]>(); for (int i = 0; i + frameSize < signal.Length; i += hopSize) { var frame = new float[frameSize]; Array.Copy(signal, i, frame, 0, frameSize); frames.Add(frame); } return frames.ToArray(); } }
}