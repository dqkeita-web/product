using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace FindAncestor.Voice
{
    public class VoicePlaybackService : IDisposable
    {
        private WaveOutEvent _waveOut;
        private WaveStream _reader;
        private readonly object _lock = new();

        public async Task PlayAsync(byte[] wavData, CancellationToken ct = default)
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    StopInternal();

                    var ms = new MemoryStream(wavData);
                    _reader = new WaveFileReader(ms);

                    _waveOut = new WaveOutEvent();
                    _waveOut.Init(_reader);
                    _waveOut.Play();
                }

                while (true)
                {
                    if (ct.IsCancellationRequested)
                    {
                        Stop();
                        break;
                    }

                    if (_waveOut.PlaybackState != PlaybackState.Playing)
                        break;

                    Thread.Sleep(10);
                }

            }, ct);
        }

        public void Stop()
        {
            lock (_lock)
            {
                StopInternal();
            }
        }

        private void StopInternal()
        {
            try
            {
                _waveOut?.Stop();
                _waveOut?.Dispose();
                _waveOut = null;

                _reader?.Dispose();
                _reader = null;
            }
            catch
            {
                // ignore
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}