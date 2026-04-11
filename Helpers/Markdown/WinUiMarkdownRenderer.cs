using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Globalization;
using System.Linq;
using Windows.System;
using Windows.UI;

using XamlBlock = Microsoft.UI.Xaml.Documents.Block;
using XamlInline = Microsoft.UI.Xaml.Documents.Inline;

namespace HuFu.Helpers.Markdown;

public sealed class WinUiMarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public void Render(RichTextBlock target, string markdown, MarkdownRenderOptions options)
    {
        target.Blocks.Clear();

        var doc = Markdig.Markdown.Parse(markdown ?? string.Empty, Pipeline);
        foreach (var block in doc)
        {
            var rendered = RenderBlock(block, options);
            if (rendered != null)
            {
                target.Blocks.Add(rendered);
            }
        }

        if (target.Blocks.Count == 0)
        {
            target.Blocks.Add(new Paragraph());
        }
    }

    private XamlBlock? RenderBlock(Markdig.Syntax.Block block, MarkdownRenderOptions options)
    {
        switch (block)
        {
            case HeadingBlock heading:
                return RenderHeading(heading, options);
            case ParagraphBlock paragraph:
                return RenderParagraph(paragraph, options);
            case QuoteBlock quote:
                return RenderQuote(quote, options);
            case ListBlock list:
                return RenderList(list, options);
            case FencedCodeBlock fenced:
                return RenderCodeBlock(fenced, options);
            case CodeBlock code:
                return RenderCodeBlock(code, options);
            case ThematicBreakBlock:
                return RenderThematicBreak(options);
            case Table table:
                return RenderTable(table, options);
            case HtmlBlock html:
                return RenderHtmlBlock(html, options);
            case ContainerBlock container:
                return RenderContainer(container, options);
            default:
                return null;
        }
    }

    private XamlBlock RenderHeading(HeadingBlock heading, MarkdownRenderOptions options)
    {
        var paragraph = new Paragraph
        {
            Margin = new Thickness(0, 8, 0, 6),
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Foreground = options.Foreground
        };

        paragraph.FontSize = heading.Level switch
        {
            1 => 20,
            2 => 18,
            3 => 16,
            4 => 15,
            5 => 14,
            _ => 13
        };

        AddInlines(paragraph.Inlines, heading.Inline, options);
        return paragraph;
    }

    private XamlBlock RenderParagraph(ParagraphBlock paragraph, MarkdownRenderOptions options)
    {
        var p = new Paragraph
        {
            Margin = new Thickness(0, 0, 0, 8),
            Foreground = options.Foreground
        };
        AddInlines(p.Inlines, paragraph.Inline, options);
        return p;
    }

    private XamlBlock RenderQuote(QuoteBlock quote, MarkdownRenderOptions options)
    {
        var quoteForeground = options.QuoteForeground ?? options.Foreground;
        var inner = new RichTextBlock
        {
            IsTextSelectionEnabled = true,
            Foreground = quoteForeground
        };

        foreach (var child in quote)
        {
            var rendered = RenderBlock(child, options with { Foreground = quoteForeground });
            if (rendered != null)
            {
                inner.Blocks.Add(rendered);
            }
        }

        var border = new Border
        {
            BorderBrush = options.QuoteBorderBrush,
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(8, 0, 0, 0),
            Margin = new Thickness(0, 4, 0, 8),
            Child = inner
        };

        return WrapUi(border);
    }

    private XamlBlock RenderList(ListBlock list, MarkdownRenderOptions options)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 8)
        };

        var depth = GetListDepth(list);
        var indent = 16 * depth;
        var index = 1;
        if (int.TryParse(list.OrderedStart?.ToString(), out var parsedStart) && parsedStart > 0)
        {
            index = parsedStart;
        }
        var isOrdered = list.IsOrdered;

        foreach (var item in list)
        {
            if (item is not ListItemBlock listItem)
                continue;

            var itemPanel = new StackPanel
            {
                Margin = new Thickness(indent, 0, 0, 6)
            };

            var firstLine = new RichTextBlock
            {
                IsTextSelectionEnabled = true,
                Foreground = options.Foreground
            };

            var firstParagraph = new Paragraph();
            firstParagraph.Inlines.Add(new Run { Text = isOrdered ? $"{index}. " : "• " });

            var blocks = listItem.ToList();
            var firstBlock = blocks.FirstOrDefault();
            if (firstBlock is ParagraphBlock firstText)
            {
                AddInlines(firstParagraph.Inlines, firstText.Inline, options);
            }

            firstLine.Blocks.Add(firstParagraph);
            itemPanel.Children.Add(firstLine);

            foreach (var child in blocks.Skip(1))
            {
                var rendered = RenderBlock(child, options);
                if (rendered != null)
                {
                    var element = CreateBlockElement(rendered, options);
                    element.Margin = new Thickness(18, 2, 0, 2);
                    itemPanel.Children.Add(element);
                }
            }

            panel.Children.Add(itemPanel);
            index++;
        }

        return WrapUi(panel);
    }

    private XamlBlock RenderCodeBlock(LeafBlock code, MarkdownRenderOptions options)
    {
        var text = GetLinesText(code);
        var border = new Border
        {
            Background = options.CodeBlockBackground,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 8)
        };

        var textBlock = new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = options.MonoFont,
            Foreground = options.Foreground
        };

        border.Child = textBlock;
        return WrapUi(border);
    }

    private XamlBlock RenderThematicBreak(MarkdownRenderOptions options)
    {
        var rect = new Rectangle
        {
            Height = 1,
            Fill = options.SeparatorBrush,
            Margin = new Thickness(0, 6, 0, 8)
        };
        return WrapUi(rect);
    }

    private XamlBlock RenderTable(Table table, MarkdownRenderOptions options)
    {
        var grid = new Grid
        {
            Margin = new Thickness(0, 0, 0, 8)
        };

        var maxColumns = table.OfType<TableRow>()
            .Select(r => r.Count)
            .DefaultIfEmpty(0)
            .Max();

        for (int c = 0; c < maxColumns; c++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        }

        int rowIndex = 0;
        foreach (var row in table.OfType<TableRow>())
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            int colIndex = 0;

            foreach (var cellObj in row)
            {
                if (cellObj is not TableCell cell)
                {
                    colIndex++;
                    continue;
                }

                var cellBlock = new RichTextBlock
                {
                    IsTextSelectionEnabled = true,
                    Foreground = options.Foreground
                };

                bool hasContent = false;
                foreach (var cellChild in cell)
                {
                    var rendered = RenderBlock(cellChild, options);
                    if (rendered != null)
                    {
                        cellBlock.Blocks.Add(rendered);
                        hasContent = true;
                    }
                }
                if (!hasContent)
                {
                    var paragraph = new Paragraph();
                    paragraph.Inlines.Add(new Run { Text = string.Empty });
                    cellBlock.Blocks.Add(paragraph);
                }
                if (row.IsHeader && cellBlock.Blocks.FirstOrDefault() is Paragraph headerParagraph)
                {
                    headerParagraph.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
                }

                var border = new Border
                {
                    BorderBrush = options.TableBorderBrush,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(6, 4, 6, 4),
                    Child = cellBlock
                };

                Grid.SetRow(border, rowIndex);
                Grid.SetColumn(border, colIndex);
                grid.Children.Add(border);
                colIndex++;
            }

            rowIndex++;
        }

        return WrapUi(grid);
    }

    private XamlBlock RenderHtmlBlock(HtmlBlock html, MarkdownRenderOptions options)
    {
        var paragraph = new Paragraph
        {
            Margin = new Thickness(0, 0, 0, 8),
            Foreground = options.Foreground
        };

        paragraph.Inlines.Add(new Run { Text = GetLinesText(html) });
        return paragraph;
    }

    private XamlBlock RenderContainer(ContainerBlock container, MarkdownRenderOptions options)
    {
        var panel = new StackPanel();
        foreach (var child in container)
        {
            var rendered = RenderBlock(child, options);
            if (rendered != null)
            {
                panel.Children.Add(CreateBlockElement(rendered, options));
            }
        }
        return WrapUi(panel);
    }

    private void AddInlines(InlineCollection inlines, ContainerInline? container, MarkdownRenderOptions options)
    {
        if (container == null)
            return;

        var current = container.FirstChild;
        while (current != null)
        {
            var rendered = RenderInline(current, options);
            if (rendered != null)
            {
                inlines.Add(rendered);
            }
            current = current.NextSibling;
        }
    }

    private XamlInline? RenderInline(Markdig.Syntax.Inlines.Inline inline, MarkdownRenderOptions options)
    {
        switch (inline)
        {
            case LiteralInline literal:
                return new Run { Text = literal.Content.ToString() };
            case LineBreakInline lineBreak:
                if (lineBreak.IsHard)
                {
                    return new LineBreak();
                }
                return new Run { Text = " " };
            case EmphasisInline emphasis:
                return RenderEmphasis(emphasis, options);
            case LinkInline link:
                return RenderLink(link, options);
            case CodeInline code:
                return RenderInlineCode(code, options);
            case HtmlInline html:
                return new Run { Text = html.Tag ?? string.Empty };
            default:
                if (inline is ContainerInline container)
                {
                    var span = new Span();
                    AddInlines(span.Inlines, container, options);
                    return span;
                }
                return null;
        }
    }

    private XamlInline RenderEmphasis(EmphasisInline emphasis, MarkdownRenderOptions options)
    {
        var span = new Span();
        if (emphasis.DelimiterCount >= 2)
        {
            span.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
        }
        if (emphasis.DelimiterCount % 2 == 1)
        {
            span.FontStyle = Windows.UI.Text.FontStyle.Italic;
        }
        if (emphasis.DelimiterChar == '~')
        {
            return RenderStrikethrough(emphasis, options);
        }

        AddInlines(span.Inlines, emphasis, options);
        return span;
    }

    private XamlInline RenderStrikethrough(EmphasisInline emphasis, MarkdownRenderOptions options)
    {
        var textBlock = new TextBlock
        {
            Foreground = options.Foreground,
            TextWrapping = TextWrapping.Wrap
        };

        var tempParagraph = new Paragraph();
        AddInlines(tempParagraph.Inlines, emphasis, options);
        var runText = string.Concat(tempParagraph.Inlines.OfType<Run>().Select(r => r.Text));

        textBlock.Text = runText;

        var grid = new Grid();
        grid.Children.Add(textBlock);
        grid.Children.Add(new Rectangle
        {
            Height = 1,
            Fill = options.Foreground,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 0)
        });

        return new InlineUIContainer { Child = grid };
    }

    private static int GetListDepth(ListBlock list)
    {
        int depth = 0;
        var parent = list.Parent;
        while (parent != null)
        {
            if (parent is ListBlock)
            {
                depth++;
            }
            parent = parent.Parent;
        }
        return depth;
    }

    private static Paragraph WrapUi(UIElement element)
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(new InlineUIContainer { Child = element });
        return paragraph;
    }

    private static FrameworkElement CreateBlockElement(XamlBlock block, MarkdownRenderOptions options)
    {
        var rtb = new RichTextBlock
        {
            IsTextSelectionEnabled = true,
            Foreground = options.Foreground
        };
        rtb.Blocks.Add(block);
        return rtb;
    }

    private XamlInline RenderLink(LinkInline link, MarkdownRenderOptions options)
    {
        if (link.IsImage)
        {
            return RenderImage(link, options);
        }

        var hyperlink = new Hyperlink();
        if (Uri.TryCreate(link.Url ?? string.Empty, UriKind.Absolute, out var uri))
        {
            hyperlink.UnderlineStyle = UnderlineStyle.Single;
            hyperlink.Click += async (_, __) =>
            {
                try
                {
                    await Launcher.LaunchUriAsync(uri);
                }
                catch
                {
                    // ignore
                }
            };
        }

        if (link.FirstChild != null)
        {
            AddInlines(hyperlink.Inlines, link, options with { Foreground = options.LinkForeground });
        }
        else
        {
            hyperlink.Inlines.Add(new Run { Text = link.Url ?? string.Empty });
        }

        hyperlink.Foreground = options.LinkForeground;
        return hyperlink;
    }

    private XamlInline RenderImage(LinkInline link, MarkdownRenderOptions options)
    {
        var image = new Image
        {
            MaxWidth = options.MaxImageWidth,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 4, 0, 4)
        };

        if (Uri.TryCreate(link.Url ?? string.Empty, UriKind.Absolute, out var uri))
        {
            image.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
        }

        var container = new InlineUIContainer
        {
            Child = image
        };
        return container;
    }

    private XamlInline RenderInlineCode(CodeInline code, MarkdownRenderOptions options)
    {
        var border = new Border
        {
            Background = options.CodeInlineBackground,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(4, 2, 4, 2),
            Margin = new Thickness(0, 0, 2, 0)
        };

        var text = new TextBlock
        {
            Text = code.Content ?? string.Empty,
            FontFamily = options.MonoFont,
            Foreground = options.Foreground
        };

        border.Child = text;
        return new InlineUIContainer { Child = border };
    }

    private static string GetLinesText(LeafBlock block)
    {
        return block.Lines.ToString() ?? string.Empty;
    }
}

public sealed record MarkdownRenderOptions
{
    public Brush Foreground { get; init; } = new SolidColorBrush(Colors.Black);
    public Brush LinkForeground { get; init; } = new SolidColorBrush(Colors.DodgerBlue);
    public Brush QuoteForeground { get; init; } = new SolidColorBrush(Colors.Gray);
    public Brush QuoteBorderBrush { get; init; } = new SolidColorBrush(Colors.LightGray);
    public Brush SeparatorBrush { get; init; } = new SolidColorBrush(Colors.LightGray);
    public Brush TableBorderBrush { get; init; } = new SolidColorBrush(Colors.LightGray);
    public Brush CodeInlineBackground { get; init; } = new SolidColorBrush(Windows.UI.Color.FromArgb(20, 0, 0, 0));
    public Brush CodeBlockBackground { get; init; } = new SolidColorBrush(Windows.UI.Color.FromArgb(14, 0, 0, 0));
    public FontFamily MonoFont { get; init; } = new FontFamily("Consolas");
    public double MaxImageWidth { get; init; } = 300;
}
