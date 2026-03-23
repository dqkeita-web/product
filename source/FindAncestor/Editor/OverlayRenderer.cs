using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace FindAncestor.Editor
{
    public static class OverlayRenderer
    {
        public static void Draw(DrawingContext dc, IEnumerable<OverlayGroup> groups, double currentTime)
        {
            foreach (var group in groups)
            {
                if (!group.IsVisible) continue;

                foreach (var item in group.Items)
                {
                    if (currentTime < item.Start || currentTime > item.End)
                        continue;

                    var text = new FormattedText(
                        item.Text,
                        CultureInfo.CurrentCulture,
                        FlowDirection.LeftToRight,
                        new Typeface("Segoe UI"),
                        item.FontSize,
                        item.Foreground,
                        1.0
                    );

                    dc.DrawText(text, new Point(item.X, item.Y));
                }
            }
        }
    }
}