using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FindAncestor.Roc
{
    public class FFmpegNvencRecorder
    {
        private Process? _ffmpeg;
        private BinaryWriter? _writer;
        private BlockingCollection<byte[]> _queue = new();
        private Task? _task;
        private int _width;
        private int _height;
        public int Width => _width;
        public int Height => _height;
        private readonly string ffmpegPath = @"C:\Tools\ffmpeg\bin\ffmpeg.exe";
        public void Start(string outputPath, int width, int height, int fps, string? audioPath)
        {
            _width = width;
            _height = height;


            string args =
                $"-y -f rawvideo -pix_fmt bgra -s {width}x{height} -r {fps} -i - " +
                "-c:v libx264 -preset veryfast -crf 18 -tune fastdecode " +
                "-pix_fmt yuv420p " +
                $"{outputPath}";

            _ffmpeg = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _ffmpeg.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Console.WriteLine(e.Data);
            };

            _ffmpeg.Start();
            _ffmpeg.BeginErrorReadLine();

            _writer = new BinaryWriter(_ffmpeg.StandardInput.BaseStream);

            _queue = new BlockingCollection<byte[]>();
            _task = Task.Run(EncodeLoop);
        }

        public void AddFrame(BitmapSource bmp)
        {
            if (_queue.IsAddingCompleted) return;

            if (bmp.Format != PixelFormats.Bgra32)
                bmp = new FormatConvertedBitmap(bmp, PixelFormats.Bgra32, null, 0);

            int width = bmp.PixelWidth;
            int height = bmp.PixelHeight;

            int stride = width * 4; // ★修正

            byte[] pixels = new byte[stride * height];

            bmp.CopyPixels(pixels, stride, 0);

            _queue.Add(pixels);
        }

        private void EncodeLoop()
        {
            try
            {
                foreach (var frame in _queue.GetConsumingEnumerable())
                {
                    if (_ffmpeg == null || _ffmpeg.HasExited)
                        break;

                    _writer?.Write(frame);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public void Stop()
        {
            try
            {
                _queue.CompleteAdding();

                _task?.Wait();

                Thread.Sleep(200); // ★追加（これで0KB防止）

                _writer?.Flush();
                _writer?.Close();

                _ffmpeg?.StandardInput.Close();
                _ffmpeg?.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}