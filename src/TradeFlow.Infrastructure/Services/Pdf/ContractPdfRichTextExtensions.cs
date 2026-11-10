using System.Collections.Generic;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using TradeFlow.Application.Common.Helpers;

namespace TradeFlow.Infrastructure.Services.Pdf;

public static class ContractPdfRichTextExtensions
{
    /// <summary>
    /// Renders a legal clause (Điều X...) into a QuestPDF ColumnDescriptor with:
    /// 1. Anti-orphan protection: The clause heading is always kept together with the first paragraph/sub-clause.
    /// 2. Natural multi-page flow: Subsequent paragraphs/sub-clauses flow naturally across page breaks without leaving large blank spaces.
    /// 3. Full rich-text preservation: Bold, italic, underline, numbered sub-clauses (1.1., 1.), lettered points (a), b)), and bullets (-, +).
    /// </summary>
    public static void RenderLegalClause(
        this ColumnDescriptor column,
        string clauseHeading,
        string? rawContent,
        float headingFontSize = 12.5f,
        float bodyFontSize = 12.5f,
        float lineHeight = 1.22f)
    {
        var blocks = ContractRichTextFormatter.ParseParagraphs(rawContent);

        if (blocks.Count == 0)
        {
            column.Item().PaddingTop(6).ShowEntire().Text(text =>
            {
                text.Span(clauseHeading).Bold().FontSize(headingFontSize);
            });
            return;
        }

        // If the first paragraph is extremely long (> 600 chars) and has multiple sentences,
        // split off the first ~2 sentences so the heading + opening lines stay together without forcing a huge single paragraph to jump pages.
        blocks = NormalizeFirstBlockForPagination(blocks);

        // 1. Heading + First Paragraph together in ShowEntire() to prevent orphan clause headings at page bottom
        column.Item().PaddingTop(4.5f).ShowEntire().Column(headAndFirst =>
        {
            headAndFirst.Item().PaddingBottom(1.5f).Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(headingFontSize).LineHeight(lineHeight));
                text.Span(clauseHeading).Bold();
            });

            RenderParagraphBlock(headAndFirst.Item(), blocks[0], bodyFontSize, lineHeight);
        });

        // 2. Remaining paragraphs flow naturally across A4 pages
        for (int i = 1; i < blocks.Count; i++)
        {
            var block = blocks[i];
            var totalChars = block.Spans.Sum(s => s.Text.Length);

            // Short sub-clauses (< 320 chars) stay intact; very long paragraphs can wrap across page breaks naturally
            var itemContainer = column.Item().PaddingTop(1.8f);
            if (totalChars <= 320)
            {
                itemContainer = itemContainer.ShowEntire();
            }

            RenderParagraphBlock(itemContainer, block, bodyFontSize, lineHeight);
        }
    }

    /// <summary>
    /// Renders a single structured paragraph block with proper indentation and inline styles (bold, italic, underline).
    /// </summary>
    public static void RenderParagraphBlock(
        IContainer container,
        ContractParagraphBlock block,
        float fontSize = 12.5f,
        float lineHeight = 1.22f,
        bool defaultItalic = false)
    {
        float leftIndent = block.IndentLevel switch
        {
            2 => 20f,
            1 => 12f,
            _ => 0f
        };

        if (leftIndent > 0)
        {
            container = container.PaddingLeft(leftIndent);
        }

        container.Text(text =>
        {
            text.DefaultTextStyle(x => x.FontSize(fontSize).LineHeight(lineHeight));
            text.Justify();

            if (!string.IsNullOrEmpty(block.BulletPrefix))
            {
                var prefixSpan = text.Span(block.BulletPrefix);
                if (char.IsDigit(block.BulletPrefix[0]))
                {
                    prefixSpan.Bold();
                }
            }

            foreach (var span in block.Spans)
            {
                var s = text.Span(span.Text);
                if (span.Bold) s = s.Bold();
                if (span.Italic || defaultItalic) s = s.Italic();
                if (span.Underline) s = s.Underline();
            }
        });
    }

    /// <summary>
    /// Renders multi-line rich text (such as LegalBasis, GeneralTerms, or Notes) into a ColumnDescriptor.
    /// </summary>
    public static void RenderRichTextParagraphs(
        this ColumnDescriptor column,
        string? rawContent,
        float fontSize = 12.5f,
        float lineHeight = 1.22f,
        bool defaultItalic = false,
        float paragraphSpacing = 2.5f)
    {
        var blocks = ContractRichTextFormatter.ParseParagraphs(rawContent);
        for (int i = 0; i < blocks.Count; i++)
        {
            var container = i == 0 ? column.Item() : column.Item().PaddingTop(paragraphSpacing);
            RenderParagraphBlock(container, blocks[i], fontSize, lineHeight, defaultItalic);
        }
    }

    private static List<ContractParagraphBlock> NormalizeFirstBlockForPagination(List<ContractParagraphBlock> blocks)
    {
        if (blocks.Count == 0)
            return blocks;

        var first = blocks[0];
        var totalLen = first.Spans.Sum(s => s.Text.Length);
        if (totalLen <= 650 || first.Spans.Count != 1)
            return blocks;

        var rawText = first.Spans[0].Text;
        // Find a sentence boundary around 250-450 chars
        int splitIdx = -1;
        for (int i = 220; i < System.Math.Min(rawText.Length - 80, 480); i++)
        {
            if (rawText[i] == '.' && i + 1 < rawText.Length && rawText[i + 1] == ' ')
            {
                splitIdx = i + 1;
                break;
            }
        }

        if (splitIdx <= 0)
            return blocks;

        var part1 = rawText[..splitIdx].TrimEnd();
        var part2 = rawText[splitIdx..].TrimStart();

        var newBlocks = new List<ContractParagraphBlock>(blocks.Count + 1)
        {
            new ContractParagraphBlock
            {
                BulletPrefix = first.BulletPrefix,
                IndentLevel = first.IndentLevel,
                IsListItem = first.IsListItem,
                Spans = new List<ContractTextSpan>
                {
                    new ContractTextSpan
                    {
                        Text = part1,
                        Bold = first.Spans[0].Bold,
                        Italic = first.Spans[0].Italic,
                        Underline = first.Spans[0].Underline
                    }
                }
            },
            new ContractParagraphBlock
            {
                BulletPrefix = null,
                IndentLevel = first.IndentLevel,
                IsListItem = false,
                Spans = new List<ContractTextSpan>
                {
                    new ContractTextSpan
                    {
                        Text = part2,
                        Bold = first.Spans[0].Bold,
                        Italic = first.Spans[0].Italic,
                        Underline = first.Spans[0].Underline
                    }
                }
            }
        };

        for (int i = 1; i < blocks.Count; i++)
        {
            newBlocks.Add(blocks[i]);
        }

        return newBlocks;
    }
}
