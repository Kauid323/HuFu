using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using HuFu.Services;
using Markdig;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace HuFu.Pages;

public sealed partial class PostDetailPage : Page, INotifyPropertyChanged
{
    private readonly YunhuApiClient _apiClient;
    private int _postId;
    private bool _isLoading;
    private string _postTitle = string.Empty;
    private string _postContent = string.Empty;
    private string _authorName = string.Empty;
    private string _authorAvatar = string.Empty;
    private string _createTime = string.Empty;
    private int _likeNum;
    private int _commentNum;
    private int _collectNum;
    private bool _isVip;
    private bool _isMarkdown;

    public PostDetailPage()
    {
        InitializeComponent();
        _apiClient = new YunhuApiClient();
    }

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

    public string PostTitle
    {
        get => _postTitle;
        set
        {
            if (_postTitle != value)
            {
                _postTitle = value;
                OnPropertyChanged();
            }
        }
    }

    public string PostContent
    {
        get => _postContent;
        set
        {
            if (_postContent != value)
            {
                _postContent = value;
                OnPropertyChanged();
            }
        }
    }

    public string AuthorName
    {
        get => _authorName;
        set
        {
            if (_authorName != value)
            {
                _authorName = value;
                OnPropertyChanged();
            }
        }
    }

    public string AuthorAvatar
    {
        get => _authorAvatar;
        set
        {
            if (_authorAvatar != value)
            {
                _authorAvatar = value;
                OnPropertyChanged();
            }
        }
    }

    public string CreateTime
    {
        get => _createTime;
        set
        {
            if (_createTime != value)
            {
                _createTime = value;
                OnPropertyChanged();
            }
        }
    }

    public int LikeNum
    {
        get => _likeNum;
        set
        {
            if (_likeNum != value)
            {
                _likeNum = value;
                OnPropertyChanged();
            }
        }
    }

    public int CommentNum
    {
        get => _commentNum;
        set
        {
            if (_commentNum != value)
            {
                _commentNum = value;
                OnPropertyChanged();
            }
        }
    }

    public int CollectNum
    {
        get => _collectNum;
        set
        {
            if (_collectNum != value)
            {
                _collectNum = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsVip
    {
        get => _isVip;
        set
        {
            if (_isVip != value)
            {
                _isVip = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsMarkdown
    {
        get => _isMarkdown;
        set
        {
            if (_isMarkdown != value)
            {
                _isMarkdown = value;
                OnPropertyChanged();
            }
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        
        if (e.Parameter is int postId)
        {
            _postId = postId;
            _ = LoadPostDetailAsync();
        }
    }

    private async Task LoadPostDetailAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;

            var token = SessionStore.Token;
            if (string.IsNullOrEmpty(token))
            {
                return;
            }

            var response = await _apiClient.GetPostDetailAsync(token, _postId);
            if (response.Code == 1 && response.Data?.Post != null)
            {
                var post = response.Data.Post;
                PostTitle = post.Title;
                PostContent = post.Content;
                AuthorName = post.SenderNickname;
                AuthorAvatar = post.SenderAvatar;
                CreateTime = post.CreateTimeText;
                LikeNum = post.LikeNum;
                CommentNum = post.CommentNum;
                CollectNum = post.CollectNum;
                IsVip = post.IsVip == 1;
                IsMarkdown = post.ContentType == 2;

                // Markdown 在 XAML 里用原生控件渲染
            }
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = "加载失败",
                Content = $"无法加载文章详情：{ex.Message}",
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

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
