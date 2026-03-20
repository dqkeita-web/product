namespace FindAncestor.Roc
{
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    public class FrameRenderer
    {
        private readonly ScrollRenderModel _model;
        private readonly int _width;
        private readonly int _height;

        public FrameRenderer(ScrollRenderModel model, int width, int height)
        {
            _model = model;
            _width = width;
            _height = height;
        }

        public BitmapSource Render(double scrollX)
        {
            var dv = new DrawingVisual();

            using (var dc = dv.RenderOpen())
            {
                double x = -scrollX;

                for (int i = 0; i < _model.Images.Count; i++)
                {
                    var img = _model.Images[i];
                    double w = _model.Widths[i];
                    double h = _model.Heights[i];

                    dc.DrawImage(img, new Rect(x, 0, w, h));

                    x += w;
                }
            }

            var rtb = new RenderTargetBitmap(
                _width,
                _height,
                96,
                96,
                PixelFormats.Pbgra32);

            rtb.Render(dv);
            return rtb;
        }
    }
}