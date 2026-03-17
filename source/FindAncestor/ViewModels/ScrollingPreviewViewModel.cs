using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FindAncestor.Models;

namespace FindAncestor.ViewModels
{
    public partial class ScrollingPreviewViewModel : ObservableObject
    {
        public ObservableCollection<ImageWithWidth> ScrollImages { get; } = [];

        private MediaPlayer? _mediaPlayer;

        private DispatcherTimer? _fadeTimer;

        [ObservableProperty]
        private double _scrollSpeed;

        [ObservableProperty]
        private double _imageHeight;

        [ObservableProperty]
        private double _aspectRatio;

        // 横スクロール位置
        [ObservableProperty]
        private double _scrollPosition;

        public ScrollingPreviewViewModel(
            double imageHeight,
            double aspectRatio,
            double scrollSpeed)
        {
            ImageHeight = imageHeight;
            AspectRatio = aspectRatio;
            ScrollSpeed = scrollSpeed;

            LoadImages(imageHeight, aspectRatio);
        }

        public void StopAudio(double fadeSeconds)
        {
            if (_mediaPlayer == null) return;
            StartFade(0, fadeSeconds, false);
        }

        public void StopAudio()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Close();
                _mediaPlayer = null;
            }
        }

        private void StartFade(double targetVolume, double durationSeconds, bool fadeIn)
        {
            _fadeTimer?.Stop();

            if (_mediaPlayer == null) return;

            double intervalMs = 50;
            double steps = durationSeconds * 1000 / intervalMs;
            double volumeStep = 1.0 / steps;

            _fadeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(intervalMs)
            };

            _fadeTimer.Tick += (s, e) =>
            {
                if (_mediaPlayer == null) return;

                if (fadeIn)
                {
                    _mediaPlayer.Volume += volumeStep;
                    if (_mediaPlayer.Volume >= targetVolume)
                        _fadeTimer.Stop();
                }
                else
                {
                    _mediaPlayer.Volume -= volumeStep;
                    if (_mediaPlayer.Volume <= 0)
                    {
                        _fadeTimer.Stop();
                        _mediaPlayer.Stop();
                    }
                }
            };

            _fadeTimer.Start();
        }



        public void StartAudio(string path, bool loop, double fadeSeconds)
        {
            if (!File.Exists(path)) return;

            _mediaPlayer ??= new MediaPlayer();
            _mediaPlayer.Open(new Uri(path));
            _mediaPlayer.Volume = 0;
            _mediaPlayer.Play();

            if (loop)
            {
                _mediaPlayer.MediaEnded += (s, e) =>
                {
                    _mediaPlayer.Position = TimeSpan.Zero;
                    _mediaPlayer.Play();
                };
            }

            StartFade(1.0, fadeSeconds, true);
        }

        private void LoadImages(double imageHeight, double aspectRatio)
        {
            ScrollImages.Clear();

            string baseFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image");
            string[] folders = ["A", "B", "C", "D"];

            // 対応拡張子
            string[] extensions = ["*.png", "*.jpg", "*.jpeg"];

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
            var count = ScrollImages.Count;
            for (var i = 0; i < count; i++)
                ScrollImages.Add(ScrollImages[i]);
        }

        public void UpdateSize(double imageWidth, double aspectRatio)
        {
            AspectRatio = aspectRatio;
            ImageHeight = imageWidth / aspectRatio;
            LoadImages(ImageHeight, AspectRatio);
        }

        public void UpdateScrollSpeed(double speed)
        {
            ScrollSpeed = speed;
        }

        public void Dispose()
        {
            StopAudio();
        }
    }
}