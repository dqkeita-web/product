using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace FindAncestor.Editor
{

    namespace FindAncestor.Editor
    {
        public class OverlayEditorViewModel
        {
            public ObservableCollection<OverlayGroup> Groups { get; }
                = new ObservableCollection<OverlayGroup>();

            public OverlayEditorViewModel()
            {
                var g = new OverlayGroup { Name = "テキスト1" };

                // 初期10行
                for (int i = 0; i < 10; i++)
                {
                    g.Items.Add(new OverlayItem
                    {
                        Text = $"テキスト{i + 1}",
                        Y = 100 + i * 40
                    });
                }

                Groups.Add(g);
            }

            public void AddGroup()
            {
                Groups.Add(new OverlayGroup { Name = $"Group{Groups.Count + 1}" });
            }

            public void RemoveItem(OverlayGroup group, OverlayItem item)
            {
                group.Items.Remove(item);
            }

            public void AddItem(OverlayGroup group)
            {
                if (group.Items.Count >= 10) return;

                group.Items.Add(new OverlayItem
                {
                    Text = "新規テキスト",
                    Y = 100
                });
            }

        }
    }
}