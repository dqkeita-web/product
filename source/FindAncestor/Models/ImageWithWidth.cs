using System.Windows.Media;

namespace FindAncestor.Models
{
    public class ImageWithWidth
    {
        public ImageSource Source { get; set; } = null!;
        public double Width { get; set; }
        public double Height { get; set; }
    }
}