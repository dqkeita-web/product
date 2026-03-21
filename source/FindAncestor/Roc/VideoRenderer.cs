using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FindAncestor.Roc
{
    public class VideoRenderer
    {
        private readonly int _width;
        private readonly int _height;
        private readonly ScrollRenderModel _model;

        private readonly DrawingVisual _dv = new();
        private readonly RenderTargetBitmap _bmp;

        public VideoRenderer(int width, int height, ScrollRenderModel model)
        {
            _width = width;
            _height = height;
            _model = model;

            _bmp = new RenderTargetBitmap(
                _width,
                _height,
                96,
                96,
                PixelFormats.Pbgra32);
        }

        public BitmapSource Render(double scrollPos)
        {
            using (var dc = _dv.RenderOpen())
            {
                // 背景
                dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, _width, _height));

                // 🔥 合計幅
                double totalWidth = 0;
                foreach (var w in _model.Widths)
                    totalWidth += w;

                if (totalWidth <= 0)
                    return _bmp;

                // 🔥 ループ（負も対応）
                double x = -scrollPos % totalWidth;
                if (x > 0) x -= totalWidth;

                // 🔥 画面埋めるまで描画
                while (x < _width)
                {
                    for (int i = 0; i < _model.Images.Count; i++)
                    {
                        var img = _model.Images[i];
                        double w = _model.Widths[i];
                        double h = _model.Heights[i];

                        dc.DrawImage(img, new Rect(x, 0, w, h));
                        x += w;

                        if (x > _width)
                            break;
                    }
                }
            }

            _bmp.Clear();
            _bmp.Render(_dv);

            var safe = _bmp.Clone();
            safe.Freeze();
            return safe;
        }
    }
}