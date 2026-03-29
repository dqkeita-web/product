// AiVoice/Services/BasicAudioRecorder.cs
using System;
using System.IO;
using NAudio.Wave;
using FindAncestor.AiVoice.Interfaces;
using FindAncestor.AiVoice.Interfaces;
namespace FindAncestor.AiVoice.Services
{
    public class BasicAudioRecorder : IAudioRecorder 
    { private WaveInEvent? _waveIn; private MemoryStream? _memoryStream;
        private WaveFileWriter? _writer; public bool IsRecording { get; private set; }

        public void StartRecording()
        {
            try
            {
                if (IsRecording) return;

                if (WaveIn.DeviceCount == 0)
                {
                    throw new Exception("録音デバイスが存在しません");
                }

                _memoryStream = new MemoryStream();

                _waveIn = new WaveInEvent
                {
                    DeviceNumber = 0,
                    WaveFormat = new WaveFormat(16000, 1)
                };

                _writer = new WaveFileWriter(_memoryStream, _waveIn.WaveFormat);

                _waveIn.DataAvailable += (s, e) =>
                {
                    _writer?.Write(e.Buffer, 0, e.BytesRecorded);
                    _writer?.Flush();
                };

                _waveIn.StartRecording();
                IsRecording = true;
            }
            catch (Exception ex)
            {
                throw new Exception("録音初期化失敗", ex);
            }
        }
        public byte[] StopRecording()
        { if (!IsRecording) return Array.Empty<byte>();
            _waveIn?.StopRecording(); _waveIn?.Dispose(); _writer?.Dispose(); 
            IsRecording = false; return _memoryStream?.ToArray() ?? Array.Empty<byte>();
        }
        public void Dispose() { _waveIn?.Dispose(); 
            _writer?.Dispose(); _memoryStream?.Dispose();

        }
    }
}