using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FindAncestor.Models;
using FindAncestor.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FindAncestor.ViewModels
{
    public partial class ImageDisplayViewModel : ObservableObject
    {
        public ObservableCollection<DisplaySize> DisplaySizes { get; } = new();
        [ObservableProperty]
        private DisplaySize? _selectedDisplaySize;

        private readonly List<ImageSource> _images = new();
        private int _index;
        private DispatcherTimer? _timer;

        [ObservableProperty]
        private ImageSource? _imageSource;

        // --- スライドショー速度（秒） ---
        [ObservableProperty]
        private double _scrollSpeed = 3;

        /// <summary>
        /// パラメーターなしコンストラクター（XAMLで使用可能）
        /// </summary>
        public ImageDisplayViewModel() : this(0) { }

        /// <summary>
        /// パラメーター付きコンストラクター（MainMenuViewModelから初期画像位置を指定可能）
        /// </summary>
        /// <param name="startIndex"></param>
        public ImageDisplayViewModel(int startIndex)
        {
            LoadImages();

            if (_images.Count == 0)
            {
                ImageSource = null;
                return;
            }

            _index = startIndex % _images.Count;
            ImageSource = _images[_index];

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(ScrollSpeed)
            };

            _timer.Tick += (s, e) =>
            {
                _index = (_index + 1) % _images.Count;
                ImageSource = _images[_index];
            };

            _timer.Start();
        }

        partial void OnScrollSpeedChanged(double value)
        {
            if (_timer != null)
            {
                _timer.Interval = TimeSpan.FromSeconds(value);
            }
        }

        private void LoadImages()
        {
            string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Image");
            if (!Directory.Exists(folder)) return;

            var files = Directory.EnumerateFiles(folder)
                .Where(f =>
                    f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f =>
                {
                    var name = Path.GetFileNameWithoutExtension(f);
                    return int.TryParse(name, out int n) ? n : int.MaxValue;
                });

            foreach (var file in files)
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(file, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    _images.Add(bitmap);
                }
                catch { }
            }
        }

        [RelayCommand]
        private void OpenScroll1Row()
        {
            if (SelectedDisplaySize == null) return;

            var window = new Scroll1RowWindow();
            window.DataContext = new Scroll1RowViewModel(
                imageHeight: SelectedDisplaySize.Height,
                aspectRatio: SelectedDisplaySize.AspectRatio,
                scrollSpeed: ScrollSpeed
            );
            window.Show();
        }

        public void Dispose()
        {
            _timer?.Stop();
        }
    }
}