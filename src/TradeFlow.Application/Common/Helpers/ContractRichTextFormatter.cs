using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using TradeFlow.Application.Common.Models.Contracts;

namespace TradeFlow.Application.Common.Helpers;

public sealed class ContractTextSpan
{
    public string Text { get; set; } = string.Empty;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
}

public sealed class ContractParagraphBlock
{
    public List<ContractTextSpan> Spans { get; set; } = new();
    public string? BulletPrefix { get; set; }
    public int IndentLevel { get; set; }
    public bool IsListItem { get; set; }
}

public sealed class ContractThirdPartyInfo
{
    public string HeadingTitle { get; set; } = "ĐẠI DIỆN BÊN C";
    public string PartyLabel { get; set; } = "BÊN C (BÊN THỨ BA / ĐƠN VỊ PHỐI HỢP)";
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Address { get; set; }
    public string? Representative { get; set; }
    public string? Position { get; set; }
    public string? Phone { get; set; }
}

public static class ContractRichTextFormatter
{
    private static readonly Regex BlockTagBreakRegex = new(
        @"<\s*/?\s*(p|div|ul|ol|table|tr)\b[^>]*>|<\s*br\s*/?\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ListItemOpenRegex = new(
        @"<\s*li\b[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex ListItemCloseRegex = new(
        @"<\s*/\s*li\s*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex SubClausePrefixRegex = new(
        @"^(?<prefix>(\d+\.\d+(\.\d+)?\.?|\d+\.|[a-zđ]\)|[-+•]))\s+(?<rest>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex InlineTokenRegex = new(
        @"(?<openTag><\s*(?<tag>b|strong|i|em|u)\s*>)|(?<closeTag><\s*/\s*(?<ctag>b|strong|i|em|u)\s*>)|(?<mdBold>\*\*(?<mdBoldText>[^*]+)\*\*)|(?<mdItalic>(?<!\*)\*(?<mdItalicText>[^*\r\n]+)\*(?!\*))",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex StripRemainingHtmlTagsRegex = new(
        @"<[^>]+>",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses raw clause/contract text (containing optional HTML tags like &lt;b&gt;, &lt;strong&gt;, &lt;i&gt;, &lt;em&gt;, &lt;u&gt;, &lt;ul&gt;, &lt;ol&gt;, &lt;li&gt;, &lt;br&gt;,
    /// markdown **bold** / *italic*, and Vietnamese legal numbering 1.1., a), -, +) into structured paragraph blocks.
    /// </summary>
    public static List<ContractParagraphBlock> ParseParagraphs(string? rawContent)
    {
        var result = new List<ContractParagraphBlock>();
        if (string.IsNullOrWhiteSpace(rawContent))
            return result;

        var normalized = rawContent
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

        // Convert <li>...</li> into newline bullet items
        normalized = ListItemOpenRegex.Replace(normalized, "\n- ");
        normalized = ListItemCloseRegex.Replace(normalized, "\n");
        normalized = BlockTagBreakRegex.Replace(normalized, "\n");

        var lines = normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var rawLine in lines)
        {
            var trimmed = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
                continue;

            var block = new ContractParagraphBlock();

            // Check if the line starts with legal numbering or bullet (1.1., 1., a), -, +, •)
            var match = SubClausePrefixRegex.Match(trimmed);
            string bodyToParse = trimmed;
            if (match.Success)
            {
                var prefix = match.Groups["prefix"].Value;
                bodyToParse = match.Groups["rest"].Value;
                block.IsListItem = true;

                if (prefix.StartsWith('-') || prefix.StartsWith('+') || prefix.StartsWith('•'))
                {
                    block.BulletPrefix = prefix == "+" ? "+ " : "- ";
                    block.IndentLevel = prefix == "+" ? 2 : 1;
                }
                else if (prefix.EndsWith(')'))
                {
                    block.BulletPrefix = prefix + " ";
                    block.IndentLevel = 1;
                }
                else
                {
                    // Numbered clause like 1.1. or 1.
                    block.BulletPrefix = prefix.EndsWith('.') ? prefix + " " : prefix + ". ";
                    block.IndentLevel = 0;
                }
            }

            block.Spans = ParseInlineSpans(bodyToParse);
            if (block.Spans.Count > 0 || !string.IsNullOrEmpty(block.BulletPrefix))
            {
                result.Add(block);
            }
        }

        return result;
    }

    public static List<ContractTextSpan> ParseInlineSpans(string? text)
    {
        var spans = new List<ContractTextSpan>();
        if (string.IsNullOrEmpty(text))
            return spans;

        bool bold = false;
        bool italic = false;
        bool underline = false;
        int currentIndex = 0;

        var matches = InlineTokenRegex.Matches(text);
        foreach (Match m in matches)
        {
            if (m.Index > currentIndex)
            {
                var plainSegment = CleanPlainSegment(text.Substring(currentIndex, m.Index - currentIndex));
                if (!string.IsNullOrEmpty(plainSegment))
                {
                    spans.Add(new ContractTextSpan
                    {
                        Text = plainSegment,
                        Bold = bold,
                        Italic = italic,
                        Underline = underline
                    });
                }
            }

            if (m.Groups["openTag"].Success)
            {
                var tag = m.Groups["tag"].Value.ToLowerInvariant();
                if (tag is "b" or "strong") bold = true;
                else if (tag is "i" or "em") italic = true;
                else if (tag == "u") underline = true;
            }
            else if (m.Groups["closeTag"].Success)
            {
                var ctag = m.Groups["ctag"].Value.ToLowerInvariant();
                if (ctag is "b" or "strong") bold = false;
                else if (ctag is "i" or "em") italic = false;
                else if (ctag == "u") underline = false;
            }
            else if (m.Groups["mdBold"].Success)
            {
                var inner = CleanPlainSegment(m.Groups["mdBoldText"].Value);
                if (!string.IsNullOrEmpty(inner))
                {
                    spans.Add(new ContractTextSpan
                    {
                        Text = inner,
                        Bold = true,
                        Italic = italic,
                        Underline = underline
                    });
                }
            }
            else if (m.Groups["mdItalic"].Success)
            {
                var inner = CleanPlainSegment(m.Groups["mdItalicText"].Value);
                if (!string.IsNullOrEmpty(inner))
                {
                    spans.Add(new ContractTextSpan
                    {
                        Text = inner,
                        Bold = bold,
                        Italic = true,
                        Underline = underline
                    });
                }
            }

            currentIndex = m.Index + m.Length;
        }

        if (currentIndex < text.Length)
        {
            var tail = CleanPlainSegment(text.Substring(currentIndex));
            if (!string.IsNullOrEmpty(tail))
            {
                spans.Add(new ContractTextSpan
                {
                    Text = tail,
                    Bold = bold,
                    Italic = italic,
                    Underline = underline
                });
            }
        }

        return spans;
    }

    private static string CleanPlainSegment(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        var stripped = StripRemainingHtmlTagsRegex.Replace(raw, string.Empty);
        return WebUtility.HtmlDecode(stripped);
    }

    /// <summary>
    /// Renders structured paragraphs into safe, legally formatted HTML for A4 Preview and Print.
    /// Preserves bold, italic, underline, sub-clause numbering (1.1., a), -, +), and indentation.
    /// </summary>
    public static string ToPreviewHtml(string? rawContent)
    {
        var blocks = ParseParagraphs(rawContent);
        if (blocks.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var block in blocks)
        {
            sb.Append(RenderParagraphBlockHtml(block));
        }

        return sb.ToString();
    }

    public static string RenderParagraphBlockHtml(ContractParagraphBlock? block)
    {
        if (block == null)
            return string.Empty;

        var indentClass = block.IndentLevel switch
        {
            2 => "tf-doc-para tf-doc-indent-2",
            1 => "tf-doc-para tf-doc-indent-1",
            _ => "tf-doc-para"
        };

        var sb = new StringBuilder();
        sb.Append($"<div class=\"{indentClass}\">");

        if (!string.IsNullOrEmpty(block.BulletPrefix))
        {
            var isNumbered = char.IsDigit(block.BulletPrefix[0]);
            if (isNumbered)
            {
                sb.Append($"<span class=\"tf-doc-subnum\">{EscapeHtmlPreservingUnicode(block.BulletPrefix)}</span>");
            }
            else
            {
                sb.Append($"<span class=\"tf-doc-bullet\">{EscapeHtmlPreservingUnicode(block.BulletPrefix)}</span>");
            }
        }

        foreach (var span in block.Spans)
        {
            var encoded = EscapeHtmlPreservingUnicode(span.Text);
            if (span.Bold) encoded = $"<strong>{encoded}</strong>";
            if (span.Italic) encoded = $"<em>{encoded}</em>";
            if (span.Underline) encoded = $"<u>{encoded}</u>";
            sb.Append(encoded);
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    /// <summary>
    /// Renders inline text (such as legal basis or notes) into safe HTML preserving bold/italic/underline.
    /// </summary>
    public static string ToInlineHtml(string? rawText, bool defaultItalic = false)
    {
        var spans = ParseInlineSpans(rawText);
        if (spans.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var span in spans)
        {
            var encoded = EscapeHtmlPreservingUnicode(span.Text);
            if (span.Bold) encoded = $"<strong>{encoded}</strong>";
            if (span.Italic || defaultItalic) encoded = $"<em>{encoded}</em>";
            if (span.Underline) encoded = $"<u>{encoded}</u>";
            sb.Append(encoded);
        }
        return sb.ToString();
    }

    private static string EscapeHtmlPreservingUnicode(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    /// <summary>
    /// Detects if a contract specifies a 3rd signing party (Bên C / Bên thứ ba / Đơn vị tư vấn giám sát / Bên làm chứng)
    /// in Notes, GeneralTerms, or Clauses so that 3-party contracts render a balanced 3-column signature block.
    /// </summary>
    public static bool TryExtractThirdParty(ContractDto? contract, out ContractThirdPartyInfo? thirdParty)
    {
        thirdParty = null;
        if (contract == null)
            return false;

        var sources = new List<string>();
        if (!string.IsNullOrWhiteSpace(contract.GeneralTerms))
            sources.Add(contract.GeneralTerms!);
        if (!string.IsNullOrWhiteSpace(contract.Notes))
            sources.Add(contract.Notes!);
        if (contract.Clauses != null)
        {
            foreach (var c in contract.Clauses)
            {
                if (!string.IsNullOrWhiteSpace(c.Title) &&
                    (c.Title.Contains("Bên C", StringComparison.OrdinalIgnoreCase) ||
                     c.Title.Contains("Bên thứ ba", StringComparison.OrdinalIgnoreCase) ||
                     c.Title.Contains("Bên làm chứng", StringComparison.OrdinalIgnoreCase) ||
                     c.Title.Contains("Đơn vị giám sát", StringComparison.OrdinalIgnoreCase)))
                {
                    sources.Add($"{c.Title}\n{c.Content}");
                }
                else if (!string.IsNullOrWhiteSpace(c.Content) &&
                         (c.Content.Contains("ĐẠI DIỆN BÊN C", StringComparison.OrdinalIgnoreCase) ||
                          c.Content.Contains("BÊN C:", StringComparison.OrdinalIgnoreCase) ||
                          c.Content.Contains("BÊN THỨ BA:", StringComparison.OrdinalIgnoreCase)))
                {
                    sources.Add(c.Content);
                }
            }
        }

        foreach (var src in sources)
        {
            var mName = Regex.Match(src, @"(?:BÊN\s*C|BÊN\s*THỨ\s*BA|ĐƠN\s*VỊ\s*TƯ\s*VẤN\s*GIÁM\s*SÁT|BÊN\s*LÀM\s*CHỨNG)\s*(?:\([^)]*\))?\s*[:\-]\s*(?<name>[^\r\n;|]+)", RegexOptions.IgnoreCase);
            if (mName.Success)
            {
                var info = new ContractThirdPartyInfo
                {
                    Name = StripRemainingHtmlTagsRegex.Replace(mName.Groups["name"].Value, "").Trim()
                };

                var mRep = Regex.Match(src, @"(?:Đại diện(?:\s*Bên\s*C)?|Người đại diện)\s*[:\-]\s*(?<rep>[^\r\n;|,]+)", RegexOptions.IgnoreCase);
                if (mRep.Success)
                    info.Representative = StripRemainingHtmlTagsRegex.Replace(mRep.Groups["rep"].Value, "").Trim();

                var mPos = Regex.Match(src, @"Chức vụ\s*[:\-]\s*(?<pos>[^\r\n;|]+)", RegexOptions.IgnoreCase);
                if (mPos.Success)
                    info.Position = StripRemainingHtmlTagsRegex.Replace(mPos.Groups["pos"].Value, "").Trim();

                var mTax = Regex.Match(src, @"Mã số thuế\s*[:\-]\s*(?<tax>[^\r\n;|]+)", RegexOptions.IgnoreCase);
                if (mTax.Success)
                    info.TaxCode = StripRemainingHtmlTagsRegex.Replace(mTax.Groups["tax"].Value, "").Trim();

                var mAddr = Regex.Match(src, @"Địa chỉ\s*[:\-]\s*(?<addr>[^\r\n;|]+)", RegexOptions.IgnoreCase);
                if (mAddr.Success)
                    info.Address = StripRemainingHtmlTagsRegex.Replace(mAddr.Groups["addr"].Value, "").Trim();

                thirdParty = info;
                return true;
            }
        }

        return false;
    }
}
