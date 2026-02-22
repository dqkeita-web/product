using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Xaml.Behaviors;

namespace FindAncestor.Behaviors
{
    public class ScrollAnimationBehavior : Behavior<ScrollViewer>
    {
        private double _scrollDelta;

        public static readonly DependencyProperty SpeedProperty =
            DependencyProperty.Register(
                nameof(Speed),
                typeof(double),
                typeof(ScrollAnimationBehavior),
                new PropertyMetadata(0.0, OnSpeedChanged));

        /// <summary>
        /// スクロール速度 (-5～5程度)
        /// 正: 右方向, 負: 左方向
        /// </summary>
        public double Speed
        {
            get => (double)GetValue(SpeedProperty);
            set => SetValue(SpeedProperty, value);
        }

        private static void OnSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ScrollAnimationBehavior behavior)
            {
                behavior._scrollDelta = behavior.Speed * 0.3; // 1フレームで進むピクセル数
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            _scrollDelta = Speed * 0.3;

            CompositionTarget.Rendering += OnRendering;
        }

        protected override void OnDetaching()
        {
            CompositionTarget.Rendering -= OnRendering;
            base.OnDetaching();
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (AssociatedObject.ScrollableWidth == 0) return;

            double newOffset = AssociatedObject.HorizontalOffset + _scrollDelta;

            // 無限スクロール処理
            if (newOffset >= AssociatedObject.ScrollableWidth)
                newOffset -= AssociatedObject.ScrollableWidth;
            else if (newOffset < 0)
                newOffset += AssociatedObject.ScrollableWidth;

            AssociatedObject.ScrollToHorizontalOffset(newOffset);
        }
    }
}