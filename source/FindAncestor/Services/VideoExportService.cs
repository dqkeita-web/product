using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FindAncestor.Enum;
using FindAncestor.Models;
using FindAncestor.ViewModels;

namespace FindAncestor.Services
{
    public class VideoExportService
    {
        public static async Task ExportAsync(
            ScrollingPreviewViewModel vm,
                ImageExportFormat format,
            int durationSeconds,
            DisplaySize? displaySize,
            bool isPreset,
            double imageWidth,
            AspectRatioItem aspectRatio,
            IList<string> audioFiles,
            int currentAudioIndex)
        {
            // ★中身は元のExportScrollVideoそのまま移植（省略せずコピー推奨）
        }
    }
}