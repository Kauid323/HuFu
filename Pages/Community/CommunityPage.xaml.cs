using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HuFu.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace HuFu.Pages;

public sealed partial class CommunityPage : Page, INotifyPropertyChanged
{
    private readonly YunhuApiClient _apiClient;
    private int _currentPage = 1;
    private bool _isLoading;
    private bool _isLoadingMore;
    private bool _hasMoreData = true;

    public CommunityPage()
    {
        InitializeComponent();
        _apiClient = new YunhuApiClient();
        RecommendPosts = new ObservableCollection<PostDisplayItem>();
        Loaded += CommunityPage_Loaded;
    }

    public ObservableCollection<PostDisplayItem> RecommendPosts { get; }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    private async void CommunityPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadRecommendPostsAsync();
    }

    private void PostItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is Border border && border.Tag is int postId)
        {
            Frame.Navigate(typeof(PostDetailPage), postId);
        }
    }

    private async void PostScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
        var scrollViewer = sender as ScrollViewer;
        if (scrollViewer == null) return;

        // 检查是否滚动到底部（距离底部小于 100 像素时触发）
        var verticalOffset = scrollViewer.VerticalOffset;
        var maxVerticalOffset = scrollViewer.ScrollableHeight;

        if (maxVerticalOffset - verticalOffset < 100 && !_isLoadingMore && _hasMoreData && !IsLoading)
        {
            await LoadMorePostsAsync();
        }
    }

    private async Task LoadRecommendPostsAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            _currentPage = 1;
            RecommendPosts.Clear();

            var token = SessionStore.Token;
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            var response = await _apiClient.GetRecommendPostsAsync(token, size: 20, page: _currentPage);
            if (response.Code == 1 && response.Data?.Posts != null)
            {
                foreach (var post in response.Data.Posts)
                {
                    RecommendPosts.Add(new PostDisplayItem
                    {
                        Id = post.Id,
                        Title = post.Title,
                        Content = post.Content,
                        SenderNickname = post.SenderNickname,
                        SenderAvatar = post.SenderAvatar,
                        LikeNum = post.LikeNum,
                        CommentNum = post.CommentNum,
                        CollectNum = post.CollectNum,
                        CreateTime = DateTimeOffset.FromUnixTimeSeconds(post.CreateTime).ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        IsVip = post.IsVip == 1
                    });
                }

                _hasMoreData = response.Data.Posts.Length >= 20;
            }
        }
        catch (Exception ex)
        {
            // 显示错误提示
            var dialog = new ContentDialog
            {
                Title = "加载失败",
                Content = $"无法加载推荐文章：{ex.Message}",
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadMorePostsAsync()
    {
        if (_isLoadingMore || !_hasMoreData) return;

        try
        {
            _isLoadingMore = true;
            _currentPage++;

            var token = SessionStore.Token;
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            var response = await _apiClient.GetRecommendPostsAsync(token, size: 20, page: _currentPage);
            if (response.Code == 1 && response.Data?.Posts != null)
            {
                foreach (var post in response.Data.Posts)
                {
                    RecommendPosts.Add(new PostDisplayItem
                    {
                        Id = post.Id,
                        Title = post.Title,
                        Content = post.Content,
                        SenderNickname = post.SenderNickname,
                        SenderAvatar = post.SenderAvatar,
                        LikeNum = post.LikeNum,
                        CommentNum = post.CommentNum,
                        CollectNum = post.CollectNum,
                        CreateTime = DateTimeOffset.FromUnixTimeSeconds(post.CreateTime).ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        IsVip = post.IsVip == 1
                    });
                }

                // 如果返回的数据少于 20 条，说明没有更多数据了
                _hasMoreData = response.Data.Posts.Length >= 20;
            }
        }
        catch
        {
            // 加载更多失败时静默处理，不显示错误提示
            _currentPage--; // 回退页码
        }
        finally
        {
            _isLoadingMore = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class PostDisplayItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string SenderNickname { get; set; } = string.Empty;
    public string SenderAvatar { get; set; } = string.Empty;
    public int LikeNum { get; set; }
    public int CommentNum { get; set; }
    public int CollectNum { get; set; }
    public string CreateTime { get; set; } = string.Empty;
    public bool IsVip { get; set; }
}
