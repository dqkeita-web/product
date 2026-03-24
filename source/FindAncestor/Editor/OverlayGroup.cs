
namespace FindAncestor.Editor
{
    using System.Collections.ObjectModel;

    public class OverlayGroup : BindableBase
    {
        private string _name = "Group";
        public string Name { get => _name; set => Set(ref _name, value); }

        private bool _isVisible = true;
        public bool IsVisible { get => _isVisible; set => Set(ref _isVisible, value); }

        private double _opacity = 1;
        public double Opacity { get => _opacity; set => Set(ref _opacity, value); }

        public ObservableCollection<OverlayItem> Items { get; set; } = new();
    }
}