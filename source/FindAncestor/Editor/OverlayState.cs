using System.Collections.ObjectModel;

namespace FindAncestor.Editor
{
    public static class OverlayState
    {
        public static ObservableCollection<OverlayItem> Items { get; } = new();
    }
}