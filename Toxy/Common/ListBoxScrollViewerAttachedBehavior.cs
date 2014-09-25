using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Toxy.Common
{
    public class ListBoxScrollViewerAttachedBehavior
    {
        public static readonly DependencyProperty ScrollingLinesProperty
            = DependencyProperty.RegisterAttached("ScrollingLines",
                                                  typeof(int),
                                                  typeof(ListBoxScrollViewerAttachedBehavior),
                                                  new UIPropertyMetadata(3, OnScrollingLinesPropertyChangedCallback));

        public static readonly DependencyProperty ScrollViewerProperty
            = DependencyProperty.RegisterAttached("ScrollViewer",
                                                  typeof(ScrollViewer),
                                                  typeof(ListBoxScrollViewerAttachedBehavior),
                                                  new UIPropertyMetadata(null));

        public static int GetScrollingLines(DependencyObject source)
        {
            return (int)source.GetValue(ScrollingLinesProperty);
        }

        public static void SetScrollingLines(DependencyObject source, int value)
        {
            source.SetValue(ScrollingLinesProperty, value);
        }

        public static ScrollViewer GetScrollViewer(DependencyObject source)
        {
            return (ScrollViewer)source.GetValue(ScrollViewerProperty);
        }

        public static void SetScrollViewer(DependencyObject source, ScrollViewer value)
        {
            source.SetValue(ScrollViewerProperty, value);
        }

        private static void OnScrollingLinesPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var lb = dependencyObject as ListBox;
            if (lb != null && e.NewValue != e.OldValue && e.NewValue is int)
            {
                lb.Loaded -= OnListBoxLoaded;
                lb.Loaded += OnListBoxLoaded;
            }
        }

        private static void OnListBoxLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            var lb = sender as ListBox;
            if (lb != null)
            {
                // get or store scrollviewer
                SetScrollViewer(lb, lb.GetDescendantByType(typeof(ScrollViewer)) as ScrollViewer);
                lb.PreviewMouseWheel -= ListBoxOnPreviewMouseWheel;
                lb.PreviewMouseWheel += ListBoxOnPreviewMouseWheel;
            }
        }

        private static void ListBoxOnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var lb = sender as ListBox;
            if (lb != null)
            {
                // get or store scrollviewer
                var lbScrollViewer = GetScrollViewer(lb);
                if (lbScrollViewer != null)
                {
                    var scrollingLines = GetScrollingLines(lb);
                    if (e.Delta < 0)
                    {
                        for (var i = 0; i < scrollingLines; i++)
                        {
                            lbScrollViewer.LineDown();
                        }
                    }
                    else
                    {
                        for (var i = 0; i < scrollingLines; i++)
                        {
                            lbScrollViewer.LineUp();
                        }
                    }
                    e.Handled = true;
                }
            }
        }
    }
}