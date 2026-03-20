namespace FindAncestor.Roc
{
    using FindAncestor.ViewModels;
    using System.Threading;
    using System.Windows.Media.Imaging;

    public class RecordingEngine
    {
        private VideoRenderer? _renderer;
        private readonly FFmpegNvencRecorder _recorder = new();

        private bool _isRecording;

        public void Start(string path, int w, int h, ScrollRenderModel model)
        {
            _renderer = new VideoRenderer(w, h, model);

            _recorder.Start(path, w, h, 60, null);

            _isRecording = true;
        }

        public void Stop()
        {
            if (!_isRecording) return;

            _isRecording = false;

            Thread.Sleep(300); // ★重要（ffmpeg flush待ち）

            _recorder.Stop();
        }

        public void ProcessFrame(ScrollingPreviewViewModel vm)
        {
            if (!_isRecording) return;
            if (_renderer == null) return;

            // ★UI完全同期
            double pos = vm.ScrollPosition;

            var bmp = _renderer.Render(pos);

            // ★安全チェック
            if (bmp == null || bmp.PixelWidth == 0 || bmp.PixelHeight == 0)
                return;

            _recorder.AddFrame(bmp);
        }
    }
}