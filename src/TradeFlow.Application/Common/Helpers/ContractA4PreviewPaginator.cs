using System;
using System.Collections.Generic;
using System.Linq;
using TradeFlow.Application.Common.Models.Contracts;

namespace TradeFlow.Application.Common.Helpers;

public enum ContractPreviewBlockKind
{
    DocumentHeaderAndParties,
    ItemsTableAndSummary,
    ClauseHeadingAndFirstParagraph,
    ClauseContinuationParagraph,
    ClosingCopiesNote,
    SignatureSection
}

public sealed class ContractPreviewRenderBlock
{
    public ContractPreviewBlockKind Kind { get; set; }
    public string? ClauseHeading { get; set; }
    public ContractParagraphBlock? Paragraph { get; set; }
    public List<ContractItemDto>? TableItems { get; set; }
    public bool IncludeTableHeader { get; set; }
    public bool IncludeFinancialSummary { get; set; }
    public int StartItemIndex { get; set; }
    public string? CopiesText { get; set; }
    public double EstimatedLines { get; set; }
}

public sealed class ContractPreviewPage
{
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public List<ContractPreviewRenderBlock> Blocks { get; set; } = new();
}

public static class ContractA4PreviewPaginator
{
    // A4 Portrait (297mm height - 18mm top - 16mm bottom - 10mm footer = 253mm usable height).
    // At 12.5pt Times New Roman with 1.22 line-height (~5.4mm per line), calibrated for 44.5 lines capacity.
    private const double MaxLinesPerPage = 44.5;
    private const double CharsPerLine = 84.0;

    public static List<ContractPreviewPage> PaginateCommercialContract(
        ContractDto contract,
        bool isB2C,
        bool hasThirdParty)
    {
        var blocks = new List<ContractPreviewRenderBlock>();

        // 1. Page 1 Header + Legal Basis + Party A + Party B (+ Party C if present)
        double headerAndPartiesLines = hasThirdParty ? 30.0 : (isB2C ? 18.0 : 22.0);
        blocks.Add(new ContractPreviewRenderBlock
        {
            Kind = ContractPreviewBlockKind.DocumentHeaderAndParties,
            EstimatedLines = headerAndPartiesLines
        });

        // 2. Items Table (chunked if very large so table header repeats across pages) + Financial Summary
        if (contract.Items != null && contract.Items.Count > 0)
        {
            const int maxRowsPerChunk = 12;
            int totalItems = contract.Items.Count;
            int offset = 0;
            while (offset < totalItems)
            {
                var chunk = contract.Items.Skip(offset).Take(maxRowsPerChunk).ToList();
                bool isLastChunk = (offset + chunk.Count) >= totalItems;
                double tableLines = 3.5 + (chunk.Count * 2.8) + (isLastChunk ? 7.5 : 1.0);

                blocks.Add(new ContractPreviewRenderBlock
                {
                    Kind = ContractPreviewBlockKind.ItemsTableAndSummary,
                    TableItems = chunk,
                    StartItemIndex = offset,
                    IncludeTableHeader = true,
                    IncludeFinancialSummary = isLastChunk,
                    EstimatedLines = tableLines
                });

                offset += chunk.Count;
            }
        }
        else if (contract.TotalAmount > 0)
        {
            blocks.Add(new ContractPreviewRenderBlock
            {
                Kind = ContractPreviewBlockKind.ItemsTableAndSummary,
                TableItems = new List<ContractItemDto>(),
                IncludeTableHeader = false,
                IncludeFinancialSummary = true,
                EstimatedLines = 2.5
            });
        }

        // 3. Clauses (Heading + First Paragraph kept together; subsequent paragraphs flow naturally)
        bool hasClauses = contract.Clauses != null && contract.Clauses.Count > 0;
        if (hasClauses)
        {
            foreach (var clause in contract.Clauses!)
            {
                var num = (clause.ClauseNumber ?? string.Empty).Trim().TrimEnd('.', ':');
                var title = (clause.Title ?? string.Empty).Trim();
                var heading = string.IsNullOrWhiteSpace(title) ? $"{num}." : $"{num}. {title}";

                AppendClauseBlocks(blocks, heading, clause.Content);
            }
        }
        else
        {
            // Only render separate term fields when structured Clauses list is absent
            if (!string.IsNullOrWhiteSpace(contract.PaymentTerms))
                AppendClauseBlocks(blocks, "Điều khoản thanh toán cụ thể", contract.PaymentTerms);
            if (!string.IsNullOrWhiteSpace(contract.DeliveryTerms))
                AppendClauseBlocks(blocks, "Điều kiện và địa điểm giao nhận", contract.DeliveryTerms);
            if (!string.IsNullOrWhiteSpace(contract.InstallationTerms))
                AppendClauseBlocks(blocks, "Điều kiện thi công lắp đặt và nghiệm thu", contract.InstallationTerms);
            if (!string.IsNullOrWhiteSpace(contract.WarrantyTerms))
                AppendClauseBlocks(blocks, "Chính sách bảo hành và hỗ trợ kỹ thuật", contract.WarrantyTerms);
            if (!string.IsNullOrWhiteSpace(contract.GeneralTerms))
                AppendClauseBlocks(blocks, "Điều khoản chung bổ sung", contract.GeneralTerms);
        }

        // 4. Closing Copies Note (if not already in clauses)
        bool alreadyHasCopiesClause = contract.Clauses != null && contract.Clauses.Any(c =>
            (c.Title != null && c.Title.Contains("số bản", StringComparison.OrdinalIgnoreCase)) ||
            (c.Content != null && c.Content.Contains("lập thành", StringComparison.OrdinalIgnoreCase)));

        if (!alreadyHasCopiesClause)
        {
            int copies = hasThirdParty ? 3 : (isB2C ? 2 : 4);
            int eachCopies = hasThirdParty ? 1 : (isB2C ? 1 : 2);
            string copiesText = hasThirdParty
                ? $"Hợp đồng này có hiệu lực kể từ ngày ký và được lập thành 0{copies} bản chính bằng tiếng Việt có giá trị pháp lý như nhau, mỗi bên giữ 0{eachCopies} bản để cùng thực hiện."
                : $"Hợp đồng này có hiệu lực kể từ ngày ký và được lập thành 0{copies} bản chính bằng tiếng Việt có giá trị pháp lý như nhau, Bên A giữ 0{eachCopies} bản, Bên B giữ 0{eachCopies} bản để cùng thực hiện.";

            blocks.Add(new ContractPreviewRenderBlock
            {
                Kind = ContractPreviewBlockKind.ClosingCopiesNote,
                CopiesText = copiesText,
                EstimatedLines = 2.4
            });
        }

        // 5. Signature Section (Always kept intact as a single unit: ~8.5 lines)
        blocks.Add(new ContractPreviewRenderBlock
        {
            Kind = ContractPreviewBlockKind.SignatureSection,
            EstimatedLines = 8.5
        });

        return DistributeBlocksIntoPages(blocks);
    }

    public static List<ContractPreviewPage> PaginateEmploymentContract(EmploymentContractDto contract)
    {
        var blocks = new List<ContractPreviewRenderBlock>
        {
            new ContractPreviewRenderBlock
            {
                Kind = ContractPreviewBlockKind.DocumentHeaderAndParties,
                EstimatedLines = 22.0
            }
        };

        var clausesToRender = contract.Clauses != null && contract.Clauses.Any()
            ? contract.Clauses.OrderBy(c => c.DisplayOrder).ToList()
            : EmploymentContractClauseDefaults.GenerateDefaultClauses(contract);

        // Defensive normalization to prevent any duplicate article numbering
        EmploymentContractClauseDefaults.NormalizeArticleNumbersIfDuplicate(clausesToRender);

        foreach (var clause in clausesToRender)
        {
            var art = (clause.ArticleNumber ?? string.Empty).Trim().TrimEnd('.', ':');
            var title = (clause.Title ?? string.Empty).Trim();
            var heading = string.IsNullOrWhiteSpace(art)
                ? title
                : (string.IsNullOrWhiteSpace(title) ? $"{art}." : $"{art}. {title}");

            AppendClauseBlocks(blocks, heading, clause.Content);
        }

        blocks.Add(new ContractPreviewRenderBlock
        {
            Kind = ContractPreviewBlockKind.SignatureSection,
            EstimatedLines = 7.5
        });

        return DistributeBlocksIntoPages(blocks);
    }

    private static void AppendClauseBlocks(
        List<ContractPreviewRenderBlock> target,
        string heading,
        string? rawContent)
    {
        var paragraphs = ContractRichTextFormatter.ParseParagraphs(rawContent);
        if (paragraphs.Count == 0)
        {
            target.Add(new ContractPreviewRenderBlock
            {
                Kind = ContractPreviewBlockKind.ClauseHeadingAndFirstParagraph,
                ClauseHeading = heading,
                Paragraph = null,
                EstimatedLines = 1.35
            });
            return;
        }

        // Split overly long single paragraphs (> 650 chars) so heading + opening sentences stay together
        // while the remainder flows naturally across pages
        paragraphs = NormalizeLongParagraphs(paragraphs);

        double firstParaLines = EstimateParagraphLines(paragraphs[0]);
        target.Add(new ContractPreviewRenderBlock
        {
            Kind = ContractPreviewBlockKind.ClauseHeadingAndFirstParagraph,
            ClauseHeading = heading,
            Paragraph = paragraphs[0],
            EstimatedLines = 1.15 + firstParaLines
        });

        for (int i = 1; i < paragraphs.Count; i++)
        {
            target.Add(new ContractPreviewRenderBlock
            {
                Kind = ContractPreviewBlockKind.ClauseContinuationParagraph,
                Paragraph = paragraphs[i],
                EstimatedLines = EstimateParagraphLines(paragraphs[i])
            });
        }
    }

    private static List<ContractParagraphBlock> NormalizeLongParagraphs(List<ContractParagraphBlock> blocks)
    {
        var result = new List<ContractParagraphBlock>();
        foreach (var block in blocks)
        {
            var totalChars = block.Spans.Sum(s => s.Text.Length);
            if (totalChars <= 560 || block.Spans.Count != 1)
            {
                result.Add(block);
                continue;
            }

            // Split long single-span text into ~350-char sentence chunks for natural page flow
            var text = block.Spans[0].Text;
            var style = block.Spans[0];
            int pos = 0;
            bool isFirstChunk = true;

            while (pos < text.Length)
            {
                int remaining = text.Length - pos;
                if (remaining <= 420)
                {
                    result.Add(CreateChunk(text[pos..].Trim(), style, isFirstChunk ? block.BulletPrefix : null, block.IndentLevel));
                    break;
                }

                int splitAt = -1;
                int searchEnd = Math.Min(pos + 420, text.Length - 40);
                for (int i = pos + 220; i < searchEnd; i++)
                {
                    if (text[i] == '.' && i + 1 < text.Length && text[i + 1] == ' ')
                    {
                        splitAt = i + 1;
                        break;
                    }
                }

                if (splitAt <= pos)
                {
                    // Fallback to space boundary
                    splitAt = text.LastIndexOf(' ', Math.Min(pos + 380, text.Length - 1));
                    if (splitAt <= pos) splitAt = pos + 380;
                }

                var chunkText = text[pos..splitAt].Trim();
                result.Add(CreateChunk(chunkText, style, isFirstChunk ? block.BulletPrefix : null, block.IndentLevel));
                pos = splitAt;
                isFirstChunk = false;
            }
        }

        return result;
    }

    private static ContractParagraphBlock CreateChunk(
        string chunkText,
        ContractTextSpan templateSpan,
        string? bulletPrefix,
        int indentLevel)
    {
        return new ContractParagraphBlock
        {
            BulletPrefix = bulletPrefix,
            IndentLevel = indentLevel,
            IsListItem = !string.IsNullOrEmpty(bulletPrefix),
            Spans = new List<ContractTextSpan>
            {
                new ContractTextSpan
                {
                    Text = chunkText,
                    Bold = templateSpan.Bold,
                    Italic = templateSpan.Italic,
                    Underline = templateSpan.Underline
                }
            }
        };
    }

    private static double EstimateParagraphLines(ContractParagraphBlock para)
    {
        int chars = (para.BulletPrefix?.Length ?? 0) + para.Spans.Sum(s => s.Text.Length);
        double effectiveWidth = CharsPerLine - (para.IndentLevel * 2.0);
        double rawLines = Math.Max(1.0, Math.Ceiling((double)(chars - 6) / effectiveWidth));
        return rawLines + 0.15; // paragraph spacing
    }

    private static List<ContractPreviewPage> DistributeBlocksIntoPages(List<ContractPreviewRenderBlock> blocks)
    {
        var pages = new List<ContractPreviewPage>();
        var currentPage = new ContractPreviewPage { PageNumber = 1 };
        double currentLines = 0;

        foreach (var block in blocks)
        {
            if (currentPage.Blocks.Count > 0 && (currentLines + block.EstimatedLines) > MaxLinesPerPage)
            {
                pages.Add(currentPage);
                currentPage = new ContractPreviewPage { PageNumber = pages.Count + 1 };
                currentLines = 0;
            }

            currentPage.Blocks.Add(block);
            currentLines += block.EstimatedLines;
        }

        if (currentPage.Blocks.Count > 0)
        {
            pages.Add(currentPage);
        }

        int total = pages.Count;
        for (int i = 0; i < total; i++)
        {
            pages[i].PageNumber = i + 1;
            pages[i].TotalPages = total;
        }

        return pages;
    }
}
