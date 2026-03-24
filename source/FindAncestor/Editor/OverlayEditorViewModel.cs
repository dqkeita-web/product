using System.Windows.Media;

namespace FindAncestor.Editor
{
    using System.Collections.ObjectModel;

    public class OverlayEditorViewModel : BindableBase
    {
        public ObservableCollection<OverlayGroup> Groups { get; set; } = new();

        private OverlayItem? _selectedItem;
        public OverlayItem? SelectedItem
        {
            get => _selectedItem;
            set => Set(ref _selectedItem, value);
        }

        // 🔥 コンストラクタ追加
        public OverlayEditorViewModel()
        {
            var g = new OverlayGroup { Name = "サンプル" };

            g.Items.Add(new OverlayItem { Text = "テスト1" });
            g.Items.Add(new OverlayItem { Text = "テスト2" });

            Groups.Add(g);
        }

        public void AddItem(OverlayGroup g)
        {
            var item = new OverlayItem
            {
                Text = "New Text",
                Foreground = Brushes.Black // 🔥 明示
            };

            g.Items.Add(item);
            SelectedItem = item;
        }

        public void RemoveItem(OverlayGroup g, OverlayItem item)
        {
            g.Items.Remove(item);

            if (SelectedItem == item)
                SelectedItem = null;
        }
    }
}