using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using InlineCollection = Avalonia.Controls.Documents.InlineCollection;
using MarkdigInline = Markdig.Syntax.Inlines.Inline;
using Table = Markdig.Extensions.Tables.Table;
using TableCell = Markdig.Extensions.Tables.TableCell;
using TableRow = Markdig.Extensions.Tables.TableRow;

namespace DnDManager.Controls;

public class MarkdownRenderer : ContentControl {
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownRenderer, string?>(nameof(Markdown));

    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    private static IBrush GetBrush(string key, string fallbackHex) {
        if (Application.Current?.Resources.TryGetResource(key, null, out var resource) == true
            && resource is IBrush brush) {
            return brush;
        }
        return new SolidColorBrush(Color.Parse(fallbackHex));
    }

    private static FontFamily GetMonospaceFont() {
        if (Application.Current?.Resources.TryGetResource("DnDMonospaceFont", null, out var resource) == true
            && resource is FontFamily font)
            return font;
        return new FontFamily("Cascadia Mono, Consolas, monospace");
    }

    public string? Markdown {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    static MarkdownRenderer() {
        MarkdownProperty.Changed.AddClassHandler<MarkdownRenderer>((r, _) => r.Render());
    }

    private void Render() {
        var text = Markdown;
        if (string.IsNullOrEmpty(text)) {
            Content = null;
            return;
        }

        var document = Markdig.Markdown.Parse(text, Pipeline);
        var panel = new StackPanel { Spacing = 8 };

        foreach (var block in document) {
            var control = RenderBlock(block);
            if (control != null)
                panel.Children.Add(control);
        }

        Content = panel;
    }

    private Control? RenderBlock(Block block) {
        return block switch {
            HeadingBlock heading => RenderHeading(heading),
            ParagraphBlock paragraph => RenderParagraph(paragraph),
            FencedCodeBlock fenced => RenderCodeBlock(fenced),
            CodeBlock code => RenderCodeBlock(code),
            ListBlock list => RenderList(list),
            QuoteBlock quote => RenderBlockquote(quote),
            Table table => RenderTable(table),
            ThematicBreakBlock => RenderHorizontalRule(),
            _ => null
        };
    }

    private Control RenderHeading(HeadingBlock heading) {
        var fontSize = heading.Level switch {
            1 => 28.0,
            2 => 24.0,
            3 => 20.0,
            4 => 18.0,
            5 => 16.0,
            _ => 14.0
        };

        var tb = new SelectableTextBlock {
            FontSize = fontSize,
            FontWeight = FontWeight.Bold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 2)
        };

        if (heading.Inline != null)
            AddInlines(tb.Inlines!, heading.Inline, InlineStyle.Normal);

        return tb;
    }

    private Control RenderParagraph(ParagraphBlock paragraph) {
        var tb = new SelectableTextBlock {
            TextWrapping = TextWrapping.Wrap
        };

        if (paragraph.Inline != null)
            AddInlines(tb.Inlines!, paragraph.Inline, InlineStyle.Normal);

        return tb;
    }

    private Control RenderCodeBlock(LeafBlock code) {
        var text = code.Lines.ToString();

        return new Border {
            Background = GetBrush("DnDCodeBlockBg", "#1E1E1E"),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 4),
            Child = new SelectableTextBlock {
                Text = text,
                FontFamily = GetMonospaceFont(),
                Foreground = GetBrush("DnDCodeText", "#E0E0E0"),
                TextWrapping = TextWrapping.Wrap
            }
        };
    }

    private Control RenderList(ListBlock list) {
        var panel = new StackPanel { Spacing = 4, Margin = new Thickness(16, 0, 0, 0) };
        var index = 1;

        foreach (var item in list) {
            if (item is not ListItemBlock listItem) continue;

            var bullet = list.IsOrdered ? $"{index++}." : "\u2022";
            var row = new Grid {
                ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            };
            var bulletBlock = new TextBlock {
                Text = bullet,
                VerticalAlignment = VerticalAlignment.Top,
                MinWidth = 16,
                Margin = new Thickness(0, 0, 8, 0)
            };
            Grid.SetColumn(bulletBlock, 0);
            row.Children.Add(bulletBlock);

            var contentPanel = new StackPanel { Spacing = 4 };
            foreach (var childBlock in listItem) {
                var control = RenderBlock(childBlock);
                if (control != null)
                    contentPanel.Children.Add(control);
            }
            Grid.SetColumn(contentPanel, 1);
            row.Children.Add(contentPanel);
            panel.Children.Add(row);
        }

        return panel;
    }

    private Control RenderBlockquote(QuoteBlock quote) {
        var innerPanel = new StackPanel { Spacing = 4 };
        foreach (var childBlock in quote) {
            var control = RenderBlock(childBlock);
            if (control != null)
                innerPanel.Children.Add(control);
        }

        return new Border {
            BorderBrush = GetBrush("DnDBlockquoteBorder", "#555555"),
            BorderThickness = new Thickness(3, 0, 0, 0),
            Padding = new Thickness(12, 4),
            Margin = new Thickness(0, 4),
            Child = innerPanel
        };
    }

    private Control RenderTable(Table table) {
        // Count columns
        var colCount = table.ColumnDefinitions?.Count ?? 0;
        if (colCount == 0 && table.Count > 0 && table[0] is TableRow firstRow)
            colCount = firstRow.Count;

        var grid = new Grid { Margin = new Thickness(0, 4) };

        for (var c = 0; c < colCount; c++)
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

        var rowIndex = 0;
        foreach (var block in table) {
            if (block is not TableRow row) continue;

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var isHeader = row.IsHeader;

            for (var c = 0; c < row.Count && c < colCount; c++) {
                if (row[c] is not TableCell cell) continue;

                var cellContent = new StackPanel { Margin = new Thickness(0) };
                foreach (var childBlock in cell) {
                    var control = RenderBlock(childBlock);
                    if (control != null) {
                        if (isHeader && control is SelectableTextBlock stb)
                            stb.FontWeight = FontWeight.Bold;
                        cellContent.Children.Add(control);
                    }
                }

                var border = new Border {
                    BorderBrush = GetBrush("DnDTableBorder", "#444444"),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(8, 4),
                    Child = cellContent
                };

                Grid.SetRow(border, rowIndex);
                Grid.SetColumn(border, c);
                grid.Children.Add(border);
            }

            rowIndex++;
        }

        return grid;
    }

    private static Control RenderHorizontalRule() {
        return new Border {
            Height = 1,
            Background = GetBrush("DnDRuleBrush", "#555555"),
            Margin = new Thickness(0, 8)
        };
    }

    // --- Inline rendering ---

    [Flags]
    private enum InlineStyle {
        Normal = 0,
        Bold = 1,
        Italic = 2
    }

    private void AddInlines(InlineCollection target, ContainerInline container, InlineStyle style) {
        var child = container.FirstChild;
        while (child != null) {
            foreach (var inline in RenderInline(child, style))
                target.Add(inline);
            child = child.NextSibling;
        }
    }

    private IEnumerable<Avalonia.Controls.Documents.Inline> RenderInline(MarkdigInline inline, InlineStyle style) {
        switch (inline) {
            case LiteralInline literal:
                yield return MakeRun(literal.Content.ToString(), style);
                break;

            case EmphasisInline emphasis: {
                var newStyle = style;
                if (emphasis.DelimiterCount == 1 || emphasis.DelimiterChar == '_' && emphasis.DelimiterCount == 1)
                    newStyle |= InlineStyle.Italic;
                else if (emphasis.DelimiterCount >= 2)
                    newStyle |= InlineStyle.Bold;

                // For bold-italic (***text***), both flags get set
                if (emphasis.DelimiterCount >= 3)
                    newStyle = style | InlineStyle.Bold | InlineStyle.Italic;

                foreach (var child in RenderContainerInline(emphasis, newStyle))
                    yield return child;
                break;
            }

            case CodeInline code: {
                var run = new Run(code.Content) {
                    FontFamily = GetMonospaceFont(),
                    Foreground = GetBrush("DnDCodeText", "#E0E0E0"),
                    Background = GetBrush("DnDInlineCodeBg", "#2D2D2D")
                };
                yield return run;
                break;
            }

            case LinkInline link: {
                var url = link.Url;
                var tb = new TextBlock {
                    Foreground = GetBrush("DnDLinkColor", "#6CB2EB"),
                    TextDecorations = TextDecorations.Underline,
                    Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand)
                };

                // Get link text from children
                var linkText = GetPlainText(link);
                tb.Text = string.IsNullOrEmpty(linkText) ? url : linkText;

                if (!string.IsNullOrEmpty(url)) {
                    tb.PointerPressed += (_, _) => {
                        try {
                            Process.Start(new ProcessStartInfo {
                                FileName = url,
                                UseShellExecute = true
                            });
                        } catch {
                            // Ignore failures to open URL
                        }
                    };
                }

                yield return new InlineUIContainer { Child = tb };
                break;
            }

            case LineBreakInline:
                yield return new LineBreak();
                break;

            case ContainerInline container: {
                foreach (var child in RenderContainerInline(container, style))
                    yield return child;
                break;
            }

            default: {
                // Fallback: render as plain text
                var text = inline.ToString();
                if (!string.IsNullOrEmpty(text))
                    yield return MakeRun(text, style);
                break;
            }
        }
    }

    private IEnumerable<Avalonia.Controls.Documents.Inline> RenderContainerInline(ContainerInline container, InlineStyle style) {
        var child = container.FirstChild;
        while (child != null) {
            foreach (var rendered in RenderInline(child, style))
                yield return rendered;
            child = child.NextSibling;
        }
    }

    private static Run MakeRun(string text, InlineStyle style) {
        var run = new Run(text);
        if (style.HasFlag(InlineStyle.Bold))
            run.FontWeight = FontWeight.Bold;
        if (style.HasFlag(InlineStyle.Italic))
            run.FontStyle = FontStyle.Italic;
        return run;
    }

    private static string GetPlainText(ContainerInline container) {
        var parts = new List<string>();
        var child = container.FirstChild;
        while (child != null) {
            if (child is LiteralInline lit)
                parts.Add(lit.Content.ToString());
            else if (child is ContainerInline inner)
                parts.Add(GetPlainText(inner));
            child = child.NextSibling;
        }
        return string.Join("", parts);
    }
}
