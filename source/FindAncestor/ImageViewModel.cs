using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FindAncestor
{
    public class ImageViewModel : INotifyPropertyChanged
    {
        private readonly DispatcherTimer _timer;
        private readonly List<ImageSource> _images = new();
        private int _index;

        private ImageSource _imageSource;
        public ImageSource ImageSource
        {
            get => _imageSource;
            set
            {
                _imageSource = value;
                OnPropertyChanged();
            }
        }

        public ImageViewModel(int startIndex = 0)
        {
            LoadImages();

            if (_images.Count == 0)
                return;

            _index = startIndex % _images.Count;
            ImageSource = _images[_index];

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };

            _timer.Tick += (s, e) =>
            {
                _index = (_index + 1) % _images.Count;
                ImageSource = _images[_index];
            };

            _timer.Start();
        }

        private void LoadImages()
        {
            string folder = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Image");

            if (!Directory.Exists(folder))
                return;

            // 1.png, 2.png, 3.png ... の前提
            var files = Directory
                .GetFiles(folder, "*.png")
                .OrderBy(f =>
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    return int.TryParse(name, out int n) ? n : int.MaxValue;
                })
                .ToList();

            foreach (var file in files)
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(file, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                _images.Add(bitmap);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}