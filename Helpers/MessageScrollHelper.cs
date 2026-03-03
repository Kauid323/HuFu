using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace HuFu.Helpers;

public class MessageScrollHelper
{
    private readonly ListView _messageListView;
    private readonly Func<System.Threading.Tasks.Task> _loadMoreAction;
    private ScrollViewer? _scrollViewer;

    public MessageScrollHelper(ListView messageListView, Func<System.Threading.Tasks.Task> loadMoreAction)
    {
        _messageListView = messageListView;
        _loadMoreAction = loadMoreAction;
    }

    public void Setup()
    {
        _scrollViewer = GetScrollViewer(_messageListView);
        if (_scrollViewer != null)
        {
            _scrollViewer.ViewChanged += OnMessageListScroll;
            System.Diagnostics.Debug.WriteLine("ScrollViewer found and event attached.");
        }
        else
        {
            _messageListView.LayoutUpdated += MessageListView_LayoutUpdated;
            System.Diagnostics.Debug.WriteLine("ScrollViewer NOT found initially, waiting for LayoutUpdated.");
        }
    }

    public void Cleanup()
    {
        if (_scrollViewer != null)
        {
            _scrollViewer.ViewChanged -= OnMessageListScroll;
        }
        _messageListView.LayoutUpdated -= MessageListView_LayoutUpdated;
    }

    private void MessageListView_LayoutUpdated(object? sender, object e)
    {
        _scrollViewer = GetScrollViewer(_messageListView);
        if (_scrollViewer != null)
        {
            _messageListView.LayoutUpdated -= MessageListView_LayoutUpdated;
            _scrollViewer.ViewChanged += OnMessageListScroll;
            System.Diagnostics.Debug.WriteLine("ScrollViewer found in LayoutUpdated and event attached.");
        }
    }

    private void OnMessageListScroll(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            if (!e.IsIntermediate && sv.VerticalOffset < 1.0)
            {
                System.Diagnostics.Debug.WriteLine("Top reached, triggering LoadMoreMessagesAsync");
                _ = _loadMoreAction();
            }
        }
    }

    private static ScrollViewer? GetScrollViewer(DependencyObject element)
    {
        if (element is ScrollViewer sv) return sv;
        for (int i = 0; i < Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element); i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i);
            var result = GetScrollViewer(child);
            if (result != null) return result;
        }
        return null;
    }
}
