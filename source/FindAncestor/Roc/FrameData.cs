using FindAncestor.Models;

namespace FindAncestor.Roc
{
    public class FrameData
    {
        public List<ImageWithWidth> Images { get; set; } = new();
        public double ScrollPosition { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }
    }
}