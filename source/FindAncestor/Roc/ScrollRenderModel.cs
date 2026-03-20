namespace FindAncestor.Roc
{
    using System.Windows.Media.Imaging;
    using System.Collections.Generic;

    public class ScrollRenderModel
    {
        public List<BitmapSource> Images { get; set; } = new();
        public List<double> Widths { get; set; } = new();
        public List<double> Heights { get; set; } = new();
    }
}