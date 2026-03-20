using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FindAncestor.Services
{
    public class SafeRecordingService
    {
        private MemoryStream? _buffer;

        public void Start()
        {
            _buffer = new MemoryStream();
        }

        public void Capture(Window window)
        {
            if (_buffer == null) return;

            var dpi = 96d;
            var width = (int)window.Width;
            var height = (int)window.Height;

            var render = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);
            render.Render(window);

            BitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(render));
            encoder.Save(_buffer);
        }

        public void Stop()
        {
            _buffer?.Dispose();
            _buffer = null;
        }
    }
}