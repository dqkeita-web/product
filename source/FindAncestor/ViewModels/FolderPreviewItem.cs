using FindAncestor.Enum;
using System.Windows.Media.Imaging;

namespace FindAncestor.ViewModels
{
    public class FolderPreviewItem
    {
        public ImageFolderType Folder { get; set; }
        public BitmapImage? Thumbnail { get; set; }
        public string Color { get; set; } = "#FFFFFF";
    }
}