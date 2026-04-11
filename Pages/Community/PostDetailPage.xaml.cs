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

                // 如果是 Markdown，渲染到 WebView2
                if (IsMarkdown)
                {
                    await RenderMarkdownAsync(post.Content);
                }
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

    private async Task RenderMarkdownAsync(string markdown)
    {
        try
        {
            // 使用 Markdig 将 Markdown 转换为 HTML
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var html = Markdown.ToHtml(markdown, pipeline);

            // 创建完整的 HTML 页面
            var fullHtml = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            font-size: 15px;
            line-height: 1.6;
            color: #333;
            padding: 16px;
            margin: 0;
        }}
        h1, h2, h3, h4, h5, h6 {{
            margin-top: 24px;
            margin-bottom: 16px;
            font-weight: 600;
            line-height: 1.25;
        }}
        h1 {{ font-size: 2em; border-bottom: 1px solid #eaecef; padding-bottom: 0.3em; }}
        h2 {{ font-size: 1.5em; border-bottom: 1px solid #eaecef; padding-bottom: 0.3em; }}
        h3 {{ font-size: 1.25em; }}
        p {{ margin-bottom: 16px; }}
        code {{
            background-color: rgba(27,31,35,0.05);
            border-radius: 3px;
            font-size: 85%;
            margin: 0;
            padding: 0.2em 0.4em;
            font-family: 'Consolas', 'Monaco', monospace;
        }}
        pre {{
            background-color: #f6f8fa;
            border-radius: 3px;
            font-size: 85%;
            line-height: 1.45;
            overflow: auto;
            padding: 16px;
        }}
        pre code {{
            background-color: transparent;
            border: 0;
            display: inline;
            line-height: inherit;
            margin: 0;
            overflow: visible;
            padding: 0;
            word-wrap: normal;
        }}
        blockquote {{
            border-left: 0.25em solid #dfe2e5;
            color: #6a737d;
            padding: 0 1em;
            margin: 0 0 16px 0;
        }}
        ul, ol {{ padding-left: 2em; margin-bottom: 16px; }}
        li {{ margin-bottom: 0.25em; }}
        img {{ max-width: 100%; height: auto; }}
        a {{ color: #0366d6; text-decoration: none; }}
        a:hover {{ text-decoration: underline; }}
        table {{
            border-collapse: collapse;
            width: 100%;
            margin-bottom: 16px;
        }}
        table th, table td {{
            border: 1px solid #dfe2e5;
            padding: 6px 13px;
        }}
        table th {{
            background-color: #f6f8fa;
            font-weight: 600;
        }}
    </style>
</head>
<body>
{html}
</body>
</html>";

            // 加载到 WebView2
            await MarkdownWebView.EnsureCoreWebView2Async();
            MarkdownWebView.NavigateToString(fullHtml);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Markdown 渲染失败: {ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
