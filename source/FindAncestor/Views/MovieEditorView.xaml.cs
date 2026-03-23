using FindAncestor.Editor;
using FindAncestor.ErrorDialog;
using FindAncestor.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace FindAncestor.Views
{
    public partial class MovieEditorView : Window
    {
        private OverlayItem? _dragItem;
        private Point _dragOffset;
        private bool _isRightDragging = false;
        private Point _mouseStartScreen;
        private Point _windowStart;
        private Rect _recordingRect;
        private bool _isSelecting;
        private Point _start;
        private bool _isDragging;
        private bool _isResizing;
        private string _resizeMode = "";
        private bool _isEditingBorder = false;
        private Point _startMouse;
        private Rect _startRect;
        private readonly DispatcherTimer _uiTimer = new() { Interval = TimeSpan.FromSeconds(2) };
        // フィールド



        public void ShowRecordingBorder(Rect rect)
        {
            _recordingRect = rect;

            RecordingBorderOverlay.Visibility = Visibility.Visible;
            RecordingBorder.Visibility = Visibility.Visible;

            Canvas.SetLeft(RecordingBorder, rect.X);
            Canvas.SetTop(RecordingBorder, rect.Y);
            RecordingBorder.Width = rect.Width;
            RecordingBorder.Height = rect.Height;
        }

        public void HideRecordingBorder()
        {
            RecordingBorderOverlay.Visibility = Visibility.Collapsed;
            RecordingBorder.Visibility = Visibility.Collapsed;
        }






        private void Overlay_MouseDown(object sender, MouseButtonEventArgs e)
        {

            _isSelecting = true;

            _start = e.GetPosition(SelectionOverlay);

            Canvas.SetLeft(SelectionRect, _start.X);
            Canvas.SetTop(SelectionRect, _start.Y);

            SelectionRect.Width = 0;
            SelectionRect.Height = 0;

            SelectionRect.Visibility = Visibility.Visible;

            SelectionOverlay.CaptureMouse(); // 🔥 これが無いとドラッグ不能
        }
        private void Overlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting) return;

            var pos = e.GetPosition(SelectionOverlay);

            double x = Math.Min(pos.X, _start.X);
            double y = Math.Min(pos.Y, _start.Y);
            double w = Math.Abs(pos.X - _start.X);
            double h = Math.Abs(pos.Y - _start.Y);

            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width = w;
            SelectionRect.Height = h;
        }

        private void Overlay_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting) return;

            var rect = new Rect(
                Canvas.GetLeft(SelectionRect),
                Canvas.GetTop(SelectionRect),
                SelectionRect.Width,
                SelectionRect.Height
            );

            if (DataContext is MovieEditorViewModel vm)
            {
                vm.OnRegionSelected(rect);

                // 🔥 ここで完全終了（超重要）
                vm.IsRegionSelecting = false;
            }

            // 🔥 View側も完全終了
            ForceEndSelection();
        }


        public MovieEditorView()
        {
            InitializeComponent();

            var vm = new MovieEditorViewModel();
            DataContext = vm;

            // 🔥 ここでイベント購読
            vm.RecordingCompleted += OnRecordingCompleted;

            var overlayTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };

            overlayTimer.Tick += (s, e) => RenderOverlayUi();
            overlayTimer.Start();
        }
        public void PlayLatestVideo()
        {
            string folder = @"E:\ffmpegMovie";

            if (!Directory.Exists(folder)) return;

            var file = Directory.GetFiles(folder, "*.mp4")
                .OrderByDescending(File.GetLastWriteTime)
                .FirstOrDefault();

            if (file == null) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = file,
                    UseShellExecute = true
                });
            }
            catch { }
        }
        public void PlayLatestVideo(string path)
        {
            if (!File.Exists(path)) return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch { }
        }
        // 🔥 録画完了 → 再生
        private void OnRecordingCompleted(string path)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("動画再生失敗: " + path);
                    ErrorDialogService.Show(ex);
                }
            });
        }
        private void OnSizeSliderReleased(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MovieEditorViewModel vm)
            {
                vm.ApplyImageWidth();
            }
        }

        private void RootGrid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isRightDragging = true;
            _mouseStartScreen = PointToScreen(e.GetPosition(this));
            _windowStart = new Point(this.Left, this.Top);
            RootGrid.CaptureMouse();
        }

        private void RootGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isRightDragging = false;
            RootGrid.ReleaseMouseCapture();
        }

        private void RootGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isRightDragging && e.RightButton == MouseButtonState.Pressed)
            {
                Point currentScreen = PointToScreen(e.GetPosition(this));
                Vector delta = currentScreen - _mouseStartScreen;

                this.Left = _windowStart.X + delta.X;
                this.Top = _windowStart.Y + delta.Y;
            }
        }

        private void HideUI(object? sender, EventArgs e)
        {
            if (DataContext is MovieEditorViewModel vm)
                vm.UiOpacity = 0;

            _uiTimer.Stop();
        }

        private void OnSliderReleased(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MovieEditorViewModel vm)
                vm.ApplyImageWidth();
        }
        public void ForceEndSelection()
        {
            _isSelecting = false;

            SelectionOverlay.ReleaseMouseCapture();

            SelectionOverlay.Visibility = Visibility.Collapsed;
            SelectionOverlay.IsHitTestVisible = false;

            SelectionRect.Visibility = Visibility.Collapsed;

            Mouse.OverrideCursor = null;
        }
        private void OnDrop(object sender, DragEventArgs e)
        {
            if (DataContext is not MovieEditorViewModel vm) return;
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (var file in files)
            {
                if (!file.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
                    !file.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) &&
                    !file.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                    continue;

                vm.AddImage(file);
            }
        }


        public void StartRegionSelect()
        {
            ForceEndSelection();

            SelectionOverlay.Visibility = Visibility.Visible;
            SelectionOverlay.IsHitTestVisible = true;

            SelectionRect.Visibility = Visibility.Collapsed; // ← 初期表示しない
            Mouse.OverrideCursor = Cursors.Cross;

            _isSelecting = false;
        }
        public void ShowRec()
        {
            RecIndicator.Visibility = Visibility.Visible;
        }

        public void HideRec()
        {
            RecIndicator.Visibility = Visibility.Collapsed;
        }

        private void RenderOverlayUi()
        {
            try
            {
                if (DataContext is not MovieEditorViewModel vm) return;
                if (vm.EditorVM == null) return;

                OverlayCanvas.Children.Clear();

                double currentTime = vm.CurrentTime;

                OverlayCanvas.Children.Clear();

                foreach (var group in vm.EditorVM.Groups)
                {
                    if (!group.IsVisible) continue;

                    foreach (var item in group.Items)
                    {
                        if (vm.CurrentTime < item.Start || vm.CurrentTime > item.End)
                            continue;

                        var tb = new TextBlock
                        {
                            Text = item.Text,
                            FontSize = item.FontSize,
                            Foreground = Brushes.White,
                            Tag = item
                        };

                        tb.MouseLeftButtonDown += OverlayItem_MouseDown;
                        tb.MouseMove += OverlayItem_MouseMove;
                        tb.MouseLeftButtonUp += OverlayItem_MouseUp;

                        Canvas.SetLeft(tb, item.X);
                        Canvas.SetTop(tb, item.Y);

                        OverlayCanvas.Children.Add(tb);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorDialogService.Show("Overlay UIの描画に失敗: " + ex);
            }
        }

        private Point _dragStart;

        private void OverlayItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is OverlayItem item)
            {
                _dragItem = item;
                _dragStart = e.GetPosition(OverlayCanvas);

                fe.CaptureMouse();
            }
        }

        private void OverlayItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragItem == null) return;
            if (e.LeftButton != MouseButtonState.Pressed) return;

            var pos = e.GetPosition(OverlayCanvas);

            double dx = pos.X - _dragStart.X;
            double dy = pos.Y - _dragStart.Y;

            _dragItem.X += dx;
            _dragItem.Y += dy;

            _dragStart = pos;
        }

        private void OverlayItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe)
            {
                fe.ReleaseMouseCapture();
            }

            _dragItem = null;
        }
    }
}