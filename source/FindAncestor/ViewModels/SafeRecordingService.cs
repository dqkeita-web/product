using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

public class SafeRecordingService
{
    private bool _isRecording;
    private List<BitmapSource> _frames = new();

    public void Start()
    {
        _frames.Clear();
        _isRecording = true;
    }

    public void Stop()
    {
        _isRecording = false;

        // 動画書き出し（FFmpegなど使う）
        ExportVideo();
    }

    public void Capture(FrameworkElement target)
    {
        if (!_isRecording) return;

        // 🔥 WebViewはキャプチャされない（これが重要）
        var rtb = new RenderTargetBitmap(
            (int)target.ActualWidth,
            (int)target.ActualHeight,
            96, 96,
            PixelFormats.Pbgra32);

        rtb.Render(target);

        _frames.Add(rtb);
    }

    private void ExportVideo()
    {
        // 👉 ここは既存の VideoExportService に接続
    }
}