using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FindAncestor.ViewModels
{
    public partial class Scroll1RowViewModel : ObservableObject
    {
        public ObservableCollection<ImageWithWidth> ScrollImages { get; } = new();

        private MediaPlayer? _mediaPlayer;

        private bool _isLoopEnabled;

        private DispatcherTimer? _fadeTimer;

        [ObservableProperty]
        private double _scrollSpeed;

        [ObservableProperty]
        private double _imageHeight;

        [ObservableProperty]
        private double _aspectRatio;

        // 追加: 横スクロール位置
        [ObservableProperty]
        private double _scrollPosition;



        public Scroll1RowViewModel(
            double imageHeight,
            double aspectRatio,
            double scrollSpeed)
        {
            ImageHeight = imageHeight;
            AspectRatio = aspectRatio;
            ScrollSpeed = scrollSpeed;

            LoadImages(imageHeight, aspectRatio);
        }

        /// <summary>
        /// 音声再生
        /// </summary>
        public void PlayAudio(string path, int volume0to100, bool loop)
        {
            StopAudio();

            if (string.IsNullOrEmpty(path))
                return;

            _isLoopEnabled = loop;

            _mediaPlayer = new MediaPlayer();

            _mediaPlayer.MediaOpened += (s, e) =>
            {
                // 0-100 → 0.0-1.0 へ変換
                _mediaPlayer.Volume = Math.Clamp(volume0to100 / 100.0, 0, 1);
                _mediaPlayer.Play();
            };

            _mediaPlayer.MediaEnded += (s, e) =>
            {
                if (_isLoopEnabled && _mediaPlayer != null)
                {
                    _mediaPlayer.Position = TimeSpan.Zero;
                    _mediaPlayer.Play();
                }
            };

            _mediaPlayer.Open(new Uri(path));
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

        public void SetVolume(double volume)
        {
            if (_mediaPlayer != null)
                _mediaPlayer.Volume = volume;  // 0-1
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
        public double GetCurrentAudioPositionSeconds()
        {
            if (_mediaPlayer == null)
                return 0;

            return _mediaPlayer.Position.TotalSeconds;
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

        public void Dispose()
        {
            StopAudio();

        }
    }


    public class ImageWithWidth
    {
        public ImageSource Source { get; set; } = null!;
        public double Width { get; set; }
        public double Height { get; set; }
    }


}