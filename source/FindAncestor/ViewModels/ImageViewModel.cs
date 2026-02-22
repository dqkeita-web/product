using CommunityToolkit.Mvvm.ComponentModel;
using FindAncestor.Enum;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FindAncestor.ViewModels
{
    public partial class ImageViewModel : ObservableObject, IDisposable
    {
        private DispatcherTimer? _timer;
        private int _index;

        public DisplayMode Mode { get; }

        public bool IsSlide => Mode == DisplayMode.Slide;
        public bool IsScroll1 => Mode == DisplayMode.Scroll1Row;
        public bool IsScroll4 => Mode == DisplayMode.Scroll4Rows;

        public ObservableCollection<ImageSource> ScrollImages { get; }
            = new();

        [ObservableProperty]
        private ImageSource? currentImage;

        public ImageViewModel(string subFolder, DisplayMode mode)
        {
            Mode = mode;

            var images = LoadImages(subFolder);

            if (Mode == DisplayMode.Slide)
                StartSlide(images);
            else
                StartScroll(images);
        }

        private ImageSource[] LoadImages(string subFolder)
        {
            string folder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Image",
                subFolder);

            if (!Directory.Exists(folder))
                return Array.Empty<ImageSource>();

            return Directory.GetFiles(folder, "*.png")
                .OrderBy(f => f)
                .Select(f =>
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(f);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    return (ImageSource)bmp;
                })
                .ToArray();
        }

        private void StartSlide(ImageSource[] images)
        {
            if (images.Length == 0) return;

            CurrentImage = images[0];

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };

            _timer.Tick += (s, e) =>
            {
                _index = (_index + 1) % images.Length;
                CurrentImage = images[_index];
            };

            _timer.Start();
        }

        private void StartScroll(ImageSource[] images)
        {
            foreach (var img in images)
                ScrollImages.Add(img);

            foreach (var img in images)
                ScrollImages.Add(img);
        }

        public void Dispose()
        {
            _timer?.Stop();
            _timer = null;
            ScrollImages.Clear();
            CurrentImage = null;
        }
    }
}