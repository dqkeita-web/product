using FindAncestor.Editor;
using System.Collections.ObjectModel;

namespace FindAncestor.Editor
{
    public class OverlayGroup
    {
        public string Name { get; set; } = "Group";

        public bool IsVisible { get; set; } = true;

        public double Opacity { get; set; } = 1;

        public ObservableCollection<OverlayItem> Items { get; set; }
            = new ObservableCollection<OverlayItem>();
    }
}