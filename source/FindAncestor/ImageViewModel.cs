using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FindAncestor
{
    public class ImageViewModel
    {
        public ObservableCollection<ImageSource> Images { get; }
            = new ObservableCollection<ImageSource>();

        public ImageViewModel()
        {
            LoadImages();
        }

        private void LoadImages()
        {
            string folder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Image");

            if (!Directory.Exists(folder))
                return;

            var files = Directory
                .GetFiles(folder, "*.png")
                .OrderBy(f => f);

            foreach (var file in files)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new System.Uri(file);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;

                // メモリ対策（重要）
                bitmap.DecodePixelHeight = 300;

                bitmap.EndInit();
                bitmap.Freeze();

                Images.Add(bitmap);
            }

            // ループ用に同じ画像をもう一度追加
            foreach (var img in Images.ToList())
                Images.Add(img);
        }
    }
}