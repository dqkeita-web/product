using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FindAncestor.ViewModels
{
    public partial class Scroll1RowViewModel : ObservableObject
    {
        public ObservableCollection<ImageWithWidth> ScrollImages { get; } = new();

        [ObservableProperty]
        private double _scrollSpeed;

        [ObservableProperty]
        private double _imageHeight;

        [ObservableProperty]
        private double _aspectRatio;

        // 追加: 横スクロール位置
        [ObservableProperty]
        private double _scrollPosition;

        public Scroll1RowViewModel(double imageHeight, double aspectRatio, double scrollSpeed)
        {
            _imageHeight = imageHeight;
            _aspectRatio = aspectRatio;
            _scrollSpeed = scrollSpeed;

            LoadImages(_imageHeight, _aspectRatio);
        }

        private void LoadImages(double imageHeight, double aspectRatio)
        {
            ScrollImages.Clear();

            string baseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image");
            string[] folders = { "A", "B", "C", "D" };

            // 対応拡張子
            string[] extensions = { "*.png", "*.jpg", "*.jpeg" };

            foreach (var f in folders)
            {
                string folderPath = Path.Combine(baseFolder, f);
                if (!Directory.Exists(folderPath)) continue;

                var files = extensions
                    .SelectMany(ext => Directory.GetFiles(folderPath, ext))
                    .OrderBy(x => x);

                foreach (var file in files)
                {
                    try
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(file);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        double width = bitmap.PixelWidth * (imageHeight / bitmap.PixelHeight);

                        ScrollImages.Add(new ImageWithWidth
                        {
                            Source = bitmap,
                            Width = width,
                            Height = imageHeight
                        });
                    }
                    catch
                    {
                        // 読み込み失敗は無視
                    }
                }
            }

            // 無限スクロール用にコピー
            int count = ScrollImages.Count;
            for (int i = 0; i < count; i++)
                ScrollImages.Add(ScrollImages[i]);
        }

        public void UpdateSize(double imageWidth, double aspectRatio)
        {
            _aspectRatio = aspectRatio;
            _imageHeight = imageWidth / aspectRatio;
            LoadImages(_imageHeight, _aspectRatio);
        }

        public void UpdateScrollSpeed(double speed)
        {
            ScrollSpeed = speed;
        }
    }

    public class ImageWithWidth
    {
        public ImageSource Source { get; set; } = null!;
        public double Width { get; set; }
        public double Height { get; set; }
    }
}