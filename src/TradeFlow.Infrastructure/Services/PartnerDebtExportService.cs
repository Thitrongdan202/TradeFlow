using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Partners;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Partners;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class PartnerDebtExportService : IPartnerDebtExportService
{
    private readonly TradeFlowDbContext _context;
    private readonly IAuthorizationService _authService;
    private readonly IAuditService _auditService;
    private readonly ILogger<PartnerDebtExportService> _logger;

    // Palette Colors
    private static readonly XLColor BrandDark = XLColor.FromHtml("#1E293B");     // Slate Navy
    private static readonly XLColor BrandIndigo = XLColor.FromHtml("#4F46E5");   // Indigo Primary
    private static readonly XLColor BrandHeaderBg = XLColor.FromHtml("#0F172A"); // Deep Navy Table Header
    private static readonly XLColor TableSubHeaderBg = XLColor.FromHtml("#F1F5F9");
    private static readonly XLColor TotalRowBg = XLColor.FromHtml("#EEF2F6");    // Neutral Light
    private static readonly XLColor BorderGray = XLColor.FromHtml("#CBD5E1");    // Light Border
    private static readonly XLColor ReceivableRed = XLColor.FromHtml("#DC2626"); // Danger
    private static readonly XLColor SuccessGreen = XLColor.FromHtml("#16A34A");  // Green
    private static readonly XLColor MutedText = XLColor.FromHtml("#64748B");

    public PartnerDebtExportService(
        TradeFlowDbContext context,
        IAuthorizationService authService,
        IAuditService auditService,
        ILogger<PartnerDebtExportService> logger)
    {
        _context = context;
        _authService = authService;
        _auditService = auditService;
        _logger = logger;
    }

    // =========================================================================
    // 1. “XUẤT FILE CÔNG NỢ” — BÁO CÁO CHI TIẾT CÔNG NỢ ĐỐI TÁC
    // =========================================================================

    public async Task<byte[]> ExportPartnerDebtStatementAsync(
        PartnerDebtStatementExportRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _authService.AuthorizeAsync(user, "Permission:Customers:View");
        if (!authResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xuất dữ liệu công nợ đối tác.");
        }

        var company = await _context.CompanySettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
                      ?? new CompanySettings("CÔNG TY CỔ PHẦN TRADEFLOW");

        var partnerSummaries = await LoadPartnerStatementsAsync(request, cancellationToken);
        var existingSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var workbook = new XLWorkbook();

        if (partnerSummaries.Count == 1)
        {
            // Single Partner Statement: Sheet 1 = Tổng hợp công nợ, Sheet 2 = Chi tiết giao dịch
            var summary = partnerSummaries[0];
            var sheet1Name = SafeSheetName("Tổng hợp công nợ", existingSheetNames);
            var ws1 = workbook.Worksheets.Add(sheet1Name);
            BuildSinglePartnerSummarySheet(ws1, summary, company, request);

            var sheet2Name = SafeSheetName("Chi tiết giao dịch", existingSheetNames);
            var ws2 = workbook.Worksheets.Add(sheet2Name);
            BuildPartnerDetailTransactionsSheet(ws2, summary, company, request);
        }
        else
        {
            // Multi-Partner Statement: Sheet 1 = Tổng hợp công nợ (Consolidated), Worksheets 2..N = Detail per partner
            var sheet1Name = SafeSheetName("Tổng hợp công nợ", existingSheetNames);
            var ws1 = workbook.Worksheets.Add(sheet1Name);
            BuildMultiPartnerSummarySheet(ws1, partnerSummaries, company, request);

            foreach (var summary in partnerSummaries)
            {
                var partnerSheetName = SafeSheetName($"{summary.Code} - {summary.Name}", existingSheetNames);
                var wsPartner = workbook.Worksheets.Add(partnerSheetName);
                BuildPartnerDetailTransactionsSheet(wsPartner, summary, company, request);
            }
        }

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);

        await _auditService.LogAsync(
            AuditEventType.DebtExported,
            user.Identity?.Name ?? "System",
            "DebtStatement",
            string.Join(",", partnerSummaries.Select(p => p.Code)),
            $"Xuất file công nợ cho {partnerSummaries.Count} đối tác. Kỳ: {request.DatePresetTitle ?? "Tùy chọn"}.",
            cancellationToken: cancellationToken);

        return memoryStream.ToArray();
    }

    // =========================================================================
    // 2. “XUẤT FILE QUẢN LÝ CÔNG NỢ EXCEL” — BÁO CÁO QUẢN LÝ DOANH NGHIỆP
    // =========================================================================

    public async Task<byte[]> ExportDebtManagementWorkbookAsync(
        DebtManagementExportRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _authService.AuthorizeAsync(user, "Permission:Customers:View");
        if (!authResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xuất sổ quản lý công nợ.");
        }

        var company = await _context.CompanySettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
                      ?? new CompanySettings("CÔNG TY CỔ PHẦN TRADEFLOW");

        // Load customers and suppliers with debt and movements
        var (customersData, suppliersData, allMovements) = await LoadManagementDatasetAsync(request, cancellationToken);
        var existingSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var workbook = new XLWorkbook();

        // Sheet 1: Tổng quan công nợ
        var ws1 = workbook.Worksheets.Add(SafeSheetName("Tổng quan công nợ", existingSheetNames));
        BuildManagementOverviewSheet(ws1, customersData, suppliersData, company, request);

        // Sheet 2: Tổng hợp phải thu khách hàng
        var ws2 = workbook.Worksheets.Add(SafeSheetName("Tổng hợp phải thu KH", existingSheetNames));
        BuildManagementCustomersSheet(ws2, customersData, company, request);

        // Sheet 3: Tổng hợp phải trả nhà cung cấp
        var ws3 = workbook.Worksheets.Add(SafeSheetName("Tổng hợp phải trả NCC", existingSheetNames));
        BuildManagementSuppliersSheet(ws3, suppliersData, company, request);

        // Sheet 4: Chi tiết giao dịch công nợ
        var ws4 = workbook.Worksheets.Add(SafeSheetName("Chi tiết giao dịch", existingSheetNames));
        BuildManagementAllMovementsSheet(ws4, allMovements, company, request);

        // Optional individual sheets for partners if requested
        if (request.IncludeIndividualPartnerSheets)
        {
            List<int> targetIds;
            if (request.SelectedCustomerIds.Any() || request.SelectedSupplierIds.Any())
            {
                targetIds = request.SelectedCustomerIds.Concat(request.SelectedSupplierIds).Distinct().ToList();
            }
            else
            {
                // Include partners that have closing balance or transactions in this period (up to 30)
                targetIds = customersData.Where(c => c.ClosingReceivable > 0).Select(c => c.Id)
                    .Concat(suppliersData.Where(s => s.ClosingPayable > 0).Select(s => s.Id))
                    .Distinct()
                    .Take(30)
                    .ToList();
            }

            if (targetIds.Any())
            {
                var selectedStatementReq = new PartnerDebtStatementExportRequest
                {
                    Scope = DebtExportScope.SelectedPartners,
                    PartnerIds = targetIds,
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    DatePresetTitle = request.DatePresetTitle,
                    IncludeLineDetails = true
                };
                var partnerSummaries = await LoadPartnerStatementsAsync(selectedStatementReq, cancellationToken);
                foreach (var summary in partnerSummaries.Take(30)) // safe guard limit
                {
                    var sheetName = SafeSheetName($"{summary.Code} - {summary.Name}", existingSheetNames);
                    var wsPartner = workbook.Worksheets.Add(sheetName);
                    BuildPartnerDetailTransactionsSheet(wsPartner, summary, company, selectedStatementReq);
                }
            }
        }

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);

        await _auditService.LogAsync(
            AuditEventType.DebtExported,
            user.Identity?.Name ?? "System",
            "DebtManagement",
            "Consolidated",
            $"Xuất file quản lý công nợ Excel ({customersData.Count} KH, {suppliersData.Count} NCC). Kỳ: {request.DatePresetTitle ?? "Toàn bộ"}.",
            cancellationToken: cancellationToken);

        return memoryStream.ToArray();
    }

    // =========================================================================
    // SHEET BUILDERS: SINGLE PARTNER STATEMENT
    // =========================================================================

    private void BuildSinglePartnerSummarySheet(
        IXLWorksheet ws,
        PartnerStatementSummaryDto summary,
        CompanySettings company,
        PartnerDebtStatementExportRequest request)
    {
        ws.ShowGridLines = true;

        // 1. Company Branding Header
        int row = 1;
        ws.Cell(row, 1).Value = company.CompanyName.ToUpper();
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 12;
        ws.Cell(row, 1).Style.Font.FontColor = BrandDark;
        row++;

        ws.Cell(row, 1).Value = $"Địa chỉ: {company.Address ?? "Trụ sở chính TradeFlow"} | MST: {company.TaxCode ?? "0319205643"} | SĐT: {company.Phone ?? "0901234567"}";
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;
        row += 2;

        // 2. Report Title
        ws.Cell(row, 1).Value = "BÁO CÁO CHI TIẾT CÔNG NỢ";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 16;
        ws.Cell(row, 1).Style.Font.FontColor = BrandIndigo;
        row++;

        string periodText = !string.IsNullOrEmpty(request.DatePresetTitle)
            ? $"Kỳ đối soát: {request.DatePresetTitle}"
            : (request.FromDate.HasValue && request.ToDate.HasValue
                ? $"Kỳ đối soát: Từ {request.FromDate.Value:dd/MM/yyyy} đến {request.ToDate.Value:dd/MM/yyyy}"
                : "Kỳ đối soát: Toàn thời gian");

        ws.Cell(row, 1).Value = $"{periodText} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 10;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;
        row += 2;

        // 3. Partner Profile Block
        int profileStartRow = row;
        ws.Range(row, 1, row, 6).Merge();
        ws.Cell(row, 1).Value = "THÔNG TIN ĐỐI TÁC";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = TableSubHeaderBg;
        ws.Cell(row, 1).Style.Font.FontColor = BrandDark;
        row++;

        AddInfoRow(ws, ref row, "Mã đối tác:", summary.Code, "Loại đối tác:", summary.PartnerType == PartnerType.Customer ? "Khách hàng" : "Nhà cung cấp");
        AddInfoRow(ws, ref row, "Tên đối tác:", summary.Name, "Số điện thoại:", summary.Phone ?? "-");
        if (!string.IsNullOrEmpty(summary.CompanyName))
        {
            AddInfoRow(ws, ref row, "Tên doanh nghiệp:", summary.CompanyName, "Mã số thuế:", summary.TaxCode ?? "-");
        }
        AddInfoRow(ws, ref row, "Địa chỉ:", summary.Address ?? "-", "Email:", summary.Email ?? "-");
        row++;

        // 4. Financial Balance Card
        ws.Range(row, 1, row, 6).Merge();
        ws.Cell(row, 1).Value = "TỔNG HỢP CÂN ĐỐI CÔNG NỢ TRONG KỲ";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = TableSubHeaderBg;
        ws.Cell(row, 1).Style.Font.FontColor = BrandDark;
        row++;

        int balanceTableStart = row;
        AddMetricRow(ws, ref row, "1. Số dư nợ đầu kỳ:", summary.OpeningBalance, summary.PartnerType == PartnerType.Customer ? "Nợ trước kỳ cần thu" : "Nợ trước kỳ cần trả");
        AddMetricRow(ws, ref row, "2. Tổng phát sinh tăng trong kỳ (+):", summary.TotalDebits, summary.PartnerType == PartnerType.Customer ? "Tiền hàng bán phát sinh" : "Tiền nhập hàng phát sinh");
        AddMetricRow(ws, ref row, "3. Tổng thanh toán / thu chi trong kỳ (-):", summary.TotalCredits, "Đã thanh toán (Sổ quỹ/CK)");
        if (summary.TotalDiscounts > 0)
        {
            AddMetricRow(ws, ref row, "4. Chiết khấu & Giảm trừ trong kỳ (-):", summary.TotalDiscounts, "Chiết khấu thanh toán / giảm nợ");
        }

        // Closing Balance Row
        ws.Cell(row, 1).Value = "5. SỐ DƯ NỢ CUỐI KỲ:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 11;

        ws.Cell(row, 3).Value = (double)summary.ClosingBalance;
        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 3).Style.Font.FontSize = 12;
        ws.Cell(row, 3).Style.Font.FontColor = summary.ClosingBalance > 0 ? ReceivableRed : SuccessGreen;

        ws.Cell(row, 4).Value = summary.BalanceDirectionDescription;
        ws.Cell(row, 4).Style.Font.Italic = true;
        ws.Cell(row, 4).Style.Font.FontColor = MutedText;

        ws.Range(row, 1, row, 6).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        ws.Range(row, 1, row, 6).Style.Border.BottomBorder = XLBorderStyleValues.Double;
        ws.Range(row, 1, row, 6).Style.Fill.BackgroundColor = TotalRowBg;
        row += 3;

        // 5. Signature Section
        int sigRow = row;
        ws.Cell(sigRow, 2).Value = "NGƯỜI LẬP BIỂU";
        ws.Cell(sigRow, 2).Style.Font.Bold = true;
        ws.Cell(sigRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(sigRow, 4).Value = "KẾ TOÁN CÔNG NỢ";
        ws.Cell(sigRow, 4).Style.Font.Bold = true;
        ws.Cell(sigRow, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(sigRow, 6).Value = "ĐẠI DIỆN ĐỐI TÁC";
        ws.Cell(sigRow, 6).Style.Font.Bold = true;
        ws.Cell(sigRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(sigRow + 1, 2).Value = "(Ký và ghi rõ họ tên)";
        ws.Cell(sigRow + 1, 2).Style.Font.Italic = true;
        ws.Cell(sigRow + 1, 2).Style.Font.FontSize = 9;
        ws.Cell(sigRow + 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(sigRow + 1, 4).Value = "(Ký và ghi rõ họ tên)";
        ws.Cell(sigRow + 1, 4).Style.Font.Italic = true;
        ws.Cell(sigRow + 1, 4).Style.Font.FontSize = 9;
        ws.Cell(sigRow + 1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(sigRow + 1, 6).Value = "(Ký, đóng dấu và ghi rõ họ tên)";
        ws.Cell(sigRow + 1, 6).Style.Font.Italic = true;
        ws.Cell(sigRow + 1, 6).Style.Font.FontSize = 9;
        ws.Cell(sigRow + 1, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Columns(1, 6).AdjustToContents(10.0, 50.0);
    }

    private void BuildPartnerDetailTransactionsSheet(
        IXLWorksheet ws,
        PartnerStatementSummaryDto summary,
        CompanySettings company,
        PartnerDebtStatementExportRequest request)
    {
        ws.ShowGridLines = true;

        int row = 1;
        ws.Cell(row, 1).Value = company.CompanyName.ToUpper();
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontColor = BrandDark;
        row++;

        ws.Cell(row, 1).Value = $"BẢNG KÊ CHI TIẾT GIAO DỊCH CÔNG NỢ — [{summary.Code}] {summary.Name.ToUpper()}";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 1).Style.Font.FontColor = BrandIndigo;
        row++;

        string periodText = !string.IsNullOrEmpty(request.DatePresetTitle)
            ? $"Kỳ: {request.DatePresetTitle}"
            : (request.FromDate.HasValue && request.ToDate.HasValue
                ? $"Kỳ: {request.FromDate.Value:dd/MM/yyyy} - {request.ToDate.Value:dd/MM/yyyy}"
                : "Kỳ: Toàn thời gian");

        ws.Cell(row, 1).Value = $"{periodText} | Dư đầu kỳ: {summary.OpeningBalance:N0} ₫ | Dư cuối kỳ: {summary.ClosingBalance:N0} ₫ | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;
        row += 2;

        int tableHeaderRow = row;
        string[] headers = new[]
        {
            "STT", "Thời gian", "Mã chứng từ", "Loại giao dịch", "Chứng từ gốc / TC",
            "Diễn giải", "Giá trị chứng từ", "Phát sinh tăng (Nợ)", "Đã thanh toán (Có)", "Dư nợ sau GD",
            "Hình thức", "Ghi chú"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(tableHeaderRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = BrandHeaderBg;
            cell.Style.Alignment.Horizontal = (i == 0 || i == 1) ? XLAlignmentHorizontalValues.Center
                : (i >= 6 && i <= 9) ? XLAlignmentHorizontalValues.Right
                : XLAlignmentHorizontalValues.Left;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = BorderGray;
        }
        row++;

        // Row 0: Số dư đầu kỳ
        ws.Cell(row, 1).Value = "-";
        ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 2).Value = request.FromDate.HasValue ? request.FromDate.Value.ToString("dd/MM/yyyy") : "-";
        ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 3).Value = "DU_DAU_KY";
        ws.Cell(row, 4).Value = "Số dư nợ đầu kỳ";
        ws.Cell(row, 5).Value = "-";
        ws.Cell(row, 6).Value = "Số dư chuyển từ kỳ trước sang";
        ws.Cell(row, 7).Value = (double)summary.OpeningBalance;
        ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(row, 8).Value = 0.0;
        ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(row, 9).Value = 0.0;
        ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(row, 10).Value = (double)summary.OpeningBalance;
        ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(row, 10).Style.Font.Bold = true;
        ws.Cell(row, 11).Value = "-";
        ws.Cell(row, 12).Value = summary.OpeningBalance > 0 ? "Còn nợ trước kỳ" : "Không có nợ đọng";

        for (int c = 1; c <= 12; c++)
        {
            ws.Cell(row, c).Style.Fill.BackgroundColor = TableSubHeaderBg;
            ws.Cell(row, c).Style.Font.Italic = true;
            ws.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            ws.Cell(row, c).Style.Border.OutsideBorderColor = BorderGray;
        }
        row++;

        // Transaction Rows
        int stt = 1;
        decimal runningBal = summary.OpeningBalance;

        foreach (var t in summary.Transactions)
        {
            runningBal += t.DebitAmount - t.CreditAmount;

            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 2).Value = t.DocumentDate.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 3).Value = t.DocumentCode;
            ws.Cell(row, 3).Style.Font.Bold = true;

            ws.Cell(row, 4).Value = t.DocumentType;
            ws.Cell(row, 5).Value = !string.IsNullOrEmpty(t.ReferenceCode) ? t.ReferenceCode : "-";
            ws.Cell(row, 6).Value = !string.IsNullOrEmpty(t.Notes) ? t.Notes : t.DocumentType;

            ws.Cell(row, 7).Value = (double)t.Amount;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 8).Value = (double)t.DebitAmount;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0 ₫";
            if (t.DebitAmount > 0) ws.Cell(row, 8).Style.Font.Bold = true;

            ws.Cell(row, 9).Value = (double)t.CreditAmount;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0 ₫";
            if (t.CreditAmount > 0)
            {
                ws.Cell(row, 9).Style.Font.Bold = true;
                ws.Cell(row, 9).Style.Font.FontColor = SuccessGreen;
            }

            ws.Cell(row, 10).Value = (double)runningBal;
            ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0 ₫";
            ws.Cell(row, 10).Style.Font.Bold = true;
            if (runningBal > 0) ws.Cell(row, 10).Style.Font.FontColor = ReceivableRed;

            ws.Cell(row, 11).Value = !string.IsNullOrEmpty(t.PaymentMethod) ? t.PaymentMethod : "-";
            ws.Cell(row, 12).Value = t.Status;

            for (int c = 1; c <= 12; c++)
            {
                ws.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(row, c).Style.Border.OutsideBorderColor = BorderGray;
            }
            row++;

            // If line items are requested and available for this invoice
            if (request.IncludeLineDetails && t.LineItems.Any())
            {
                foreach (var line in t.LineItems)
                {
                    ws.Cell(row, 1).Value = "";
                    ws.Cell(row, 2).Value = "";
                    ws.Cell(row, 3).Value = $"   ↳ {line.ProductCode}";
                    ws.Cell(row, 3).Style.Font.Italic = true;
                    ws.Cell(row, 3).Style.Font.FontSize = 9;

                    ws.Cell(row, 4).Value = line.ProductName;
                    ws.Cell(row, 4).Style.Font.Italic = true;
                    ws.Cell(row, 4).Style.Font.FontSize = 9;

                    ws.Cell(row, 5).Value = request.IncludeUnit ? line.UnitName : "";
                    ws.Cell(row, 5).Style.Font.Italic = true;
                    ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    ws.Cell(row, 6).Value = request.IncludeQuantity ? (double)line.Quantity : 0.0;
                    if (request.IncludeQuantity)
                    {
                        ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.##";
                        ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }

                    ws.Cell(row, 7).Value = request.IncludeUnitPrice ? (double)line.UnitPrice : 0.0;
                    if (request.IncludeUnitPrice) ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0 ₫";

                    ws.Cell(row, 8).Value = request.IncludeDiscount ? (double)line.DiscountAmount : 0.0;
                    if (request.IncludeDiscount) ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0 ₫";

                    ws.Cell(row, 9).Value = request.IncludeVat ? (double)line.TaxAmount : 0.0;
                    if (request.IncludeVat) ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0 ₫";

                    ws.Cell(row, 10).Value = request.IncludeLineTotal ? (double)line.LineTotal : 0.0;
                    if (request.IncludeLineTotal)
                    {
                        ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0 ₫";
                        ws.Cell(row, 10).Style.Font.Italic = true;
                    }

                    ws.Cell(row, 11).Value = $"VAT {line.TaxRate}%";
                    ws.Cell(row, 12).Value = "Chi tiết dòng hàng";

                    for (int c = 1; c <= 12; c++)
                    {
                        ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                        ws.Cell(row, c).Style.Font.FontSize = 9;
                        ws.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                        ws.Cell(row, c).Style.Border.OutsideBorderColor = BorderGray;
                    }
                    row++;
                }
            }
        }

        // Summary Total Row at the bottom
        int totalRow = row;
        ws.Cell(totalRow, 1).Value = "TỔNG CỘNG";
        ws.Cell(totalRow, 1).Style.Font.Bold = true;
        ws.Cell(totalRow, 6).Value = $"Tổng phát sinh trong kỳ ({summary.Transactions.Count} giao dịch)";
        ws.Cell(totalRow, 6).Style.Font.Bold = true;

        ws.Cell(totalRow, 8).Value = (double)summary.TotalDebits;
        ws.Cell(totalRow, 8).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 8).Style.Font.Bold = true;

        ws.Cell(totalRow, 9).Value = (double)summary.TotalCredits;
        ws.Cell(totalRow, 9).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 9).Style.Font.Bold = true;
        ws.Cell(totalRow, 9).Style.Font.FontColor = SuccessGreen;

        ws.Cell(totalRow, 10).Value = (double)summary.ClosingBalance;
        ws.Cell(totalRow, 10).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 10).Style.Font.Bold = true;
        ws.Cell(totalRow, 10).Style.Font.FontColor = summary.ClosingBalance > 0 ? ReceivableRed : SuccessGreen;

        ws.Range(totalRow, 1, totalRow, 12).Style.Fill.BackgroundColor = TotalRowBg;
        ws.Range(totalRow, 1, totalRow, 12).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        ws.Range(totalRow, 1, totalRow, 12).Style.Border.BottomBorder = XLBorderStyleValues.Double;

        // Auto-filter and freeze pane
        ws.Range(tableHeaderRow, 1, totalRow, 12).SetAutoFilter();
        ws.SheetView.FreezeRows(tableHeaderRow);
        ws.Columns(1, 12).AdjustToContents(8.0, 45.0);
    }

    // =========================================================================
    // SHEET BUILDERS: MULTI-PARTNER STATEMENT SUMMARY
    // =========================================================================

    private void BuildMultiPartnerSummarySheet(
        IXLWorksheet ws,
        List<PartnerStatementSummaryDto> partners,
        CompanySettings company,
        PartnerDebtStatementExportRequest request)
    {
        ws.ShowGridLines = true;

        int row = 1;
        ws.Cell(row, 1).Value = company.CompanyName.ToUpper();
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontColor = BrandDark;
        row++;

        ws.Cell(row, 1).Value = "TỔNG HỢP CÔNG NỢ ĐỐI TÁC ĐƯỢC CHỌN";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 1).Style.Font.FontColor = BrandIndigo;
        row++;

        string periodText = !string.IsNullOrEmpty(request.DatePresetTitle)
            ? $"Kỳ: {request.DatePresetTitle}"
            : (request.FromDate.HasValue && request.ToDate.HasValue
                ? $"Kỳ: {request.FromDate.Value:dd/MM/yyyy} - {request.ToDate.Value:dd/MM/yyyy}"
                : "Kỳ: Toàn thời gian");

        ws.Cell(row, 1).Value = $"{periodText} | Số đối tác: {partners.Count} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;
        row += 2;

        int tableHeaderRow = row;
        string[] headers = new[]
        {
            "STT", "Mã đối tác", "Tên đối tác / Pháp nhân", "Số điện thoại", "Loại đối tác",
            "Dư nợ đầu kỳ", "Phát sinh tăng (Nợ)", "Thanh toán (Có)", "Giảm trừ / CK", "Dư nợ cuối kỳ", "Trạng thái công nợ"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(tableHeaderRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = BrandHeaderBg;
            cell.Style.Alignment.Horizontal = (i == 0 || i == 3 || i == 4) ? XLAlignmentHorizontalValues.Center
                : (i >= 5 && i <= 9) ? XLAlignmentHorizontalValues.Right
                : XLAlignmentHorizontalValues.Left;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = BorderGray;
        }
        row++;

        int stt = 1;
        foreach (var p in partners)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 2).Value = p.Code;
            ws.Cell(row, 2).Style.Font.Bold = true;

            ws.Cell(row, 3).Value = !string.IsNullOrEmpty(p.CompanyName) ? $"{p.Name} ({p.CompanyName})" : p.Name;
            ws.Cell(row, 4).Value = p.Phone ?? "-";
            ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 5).Value = p.PartnerType == PartnerType.Customer ? "Khách hàng" : "Nhà cung cấp";
            ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 6).Value = (double)p.OpeningBalance;
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 7).Value = (double)p.TotalDebits;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 8).Value = (double)p.TotalCredits;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 9).Value = (double)p.TotalDiscounts;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 10).Value = (double)p.ClosingBalance;
            ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0 ₫";
            ws.Cell(row, 10).Style.Font.Bold = true;
            if (p.ClosingBalance > 0) ws.Cell(row, 10).Style.Font.FontColor = ReceivableRed;

            ws.Cell(row, 11).Value = p.ClosingBalance > 0 ? "Còn nợ" : "Đã thanh toán đủ";

            for (int c = 1; c <= 11; c++)
            {
                ws.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(row, c).Style.Border.OutsideBorderColor = BorderGray;
            }
            row++;
        }

        // Total Row
        int totalRow = row;
        ws.Cell(totalRow, 1).Value = "TỔNG CỘNG";
        ws.Cell(totalRow, 1).Style.Font.Bold = true;
        ws.Cell(totalRow, 3).Value = $"Tổng cộng {partners.Count} đối tác được chọn";
        ws.Cell(totalRow, 3).Style.Font.Bold = true;

        ws.Cell(totalRow, 6).Value = (double)partners.Sum(x => x.OpeningBalance);
        ws.Cell(totalRow, 6).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 6).Style.Font.Bold = true;

        ws.Cell(totalRow, 7).Value = (double)partners.Sum(x => x.TotalDebits);
        ws.Cell(totalRow, 7).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 7).Style.Font.Bold = true;

        ws.Cell(totalRow, 8).Value = (double)partners.Sum(x => x.TotalCredits);
        ws.Cell(totalRow, 8).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 8).Style.Font.Bold = true;

        ws.Cell(totalRow, 9).Value = (double)partners.Sum(x => x.TotalDiscounts);
        ws.Cell(totalRow, 9).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 9).Style.Font.Bold = true;

        ws.Cell(totalRow, 10).Value = (double)partners.Sum(x => x.ClosingBalance);
        ws.Cell(totalRow, 10).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 10).Style.Font.Bold = true;
        ws.Cell(totalRow, 10).Style.Font.FontColor = ReceivableRed;

        ws.Range(totalRow, 1, totalRow, 11).Style.Fill.BackgroundColor = TotalRowBg;
        ws.Range(totalRow, 1, totalRow, 11).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        ws.Range(totalRow, 1, totalRow, 11).Style.Border.BottomBorder = XLBorderStyleValues.Double;

        ws.Range(tableHeaderRow, 1, totalRow, 11).SetAutoFilter();
        ws.SheetView.FreezeRows(tableHeaderRow);
        ws.Columns(1, 11).AdjustToContents(8.0, 45.0);
    }

    // =========================================================================
    // SHEET BUILDERS: MANAGEMENT WORKBOOK SHEETS
    // =========================================================================

    private void BuildManagementOverviewSheet(
        IXLWorksheet ws,
        List<CustomerDebtSummaryRowDto> customers,
        List<SupplierDebtSummaryRowDto> suppliers,
        CompanySettings company,
        DebtManagementExportRequest request)
    {
        ws.ShowGridLines = true;

        int row = 1;
        ws.Cell(row, 1).Value = company.CompanyName.ToUpper();
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 12;
        ws.Cell(row, 1).Style.Font.FontColor = BrandDark;
        row++;

        ws.Cell(row, 1).Value = "BÁO CÁO QUẢN LÝ CÔNG NỢ DOANH NGHIỆP";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 16;
        ws.Cell(row, 1).Style.Font.FontColor = BrandIndigo;
        row++;

        string periodText = !string.IsNullOrEmpty(request.DatePresetTitle)
            ? $"Kỳ báo cáo: {request.DatePresetTitle}"
            : (request.FromDate.HasValue && request.ToDate.HasValue
                ? $"Kỳ: {request.FromDate.Value:dd/MM/yyyy} - {request.ToDate.Value:dd/MM/yyyy}"
                : "Kỳ báo cáo: Toàn thời gian");

        ws.Cell(row, 1).Value = $"{periodText} | Thời điểm xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 10;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;
        row += 2;

        // Executive KPI Cards Table
        ws.Range(row, 1, row, 6).Merge();
        ws.Cell(row, 1).Value = "CÁC CHỈ SỐ CÔNG NỢ TRỌNG YẾU (EXECUTIVE SUMMARY)";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = TableSubHeaderBg;
        ws.Cell(row, 1).Style.Font.FontColor = BrandDark;
        row++;

        decimal totalCustRec = customers.Sum(x => x.ClosingReceivable);
        decimal totalSupPay = suppliers.Sum(x => x.ClosingPayable);
        decimal netDiff = totalCustRec - totalSupPay;

        int custWithDebt = customers.Count(x => x.ClosingReceivable > 0);
        int supWithDebt = suppliers.Count(x => x.ClosingPayable > 0);

        AddMetricRow(ws, ref row, "1. Tổng công nợ phải thu từ khách hàng:", totalCustRec, $"{custWithDebt} khách hàng đang có dư nợ");
        AddMetricRow(ws, ref row, "2. Tổng công nợ phải trả cho nhà cung cấp:", totalSupPay, $"{supWithDebt} nhà cung cấp có công nợ");
        
        // Net difference with clear analytical label
        ws.Cell(row, 1).Value = "3. Chênh lệch công nợ (Đối soát tham chiếu):";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 3).Value = (double)netDiff;
        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(row, 3).Style.Font.Bold = true;
        ws.Cell(row, 3).Style.Font.FontSize = 11;
        ws.Cell(row, 4).Value = "Chỉ tiêu phân tích tổng hợp (Phải thu - Phải trả), không phải số dư cấn trừ thanh toán";
        ws.Cell(row, 4).Style.Font.Italic = true;
        ws.Cell(row, 4).Style.Font.FontColor = MutedText;
        row += 2;

        // Counts Table
        ws.Range(row, 1, row, 6).Merge();
        ws.Cell(row, 1).Value = "THỐNG KÊ QUY MÔ ĐỐI TÁC";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Fill.BackgroundColor = TableSubHeaderBg;
        ws.Cell(row, 1).Style.Font.FontColor = BrandDark;
        row++;

        AddInfoRow(ws, ref row, "Tổng số khách hàng:", customers.Count.ToString(), "Khách hàng còn nợ:", $"{custWithDebt} KH ({((customers.Count > 0 ? (double)custWithDebt / customers.Count * 100 : 0)):N1}%)");
        AddInfoRow(ws, ref row, "Tổng số nhà cung cấp:", suppliers.Count.ToString(), "Nhà cung cấp có công nợ:", $"{supWithDebt} NCC");
        row += 2;

        ws.Cell(row, 1).Value = "Ghi chú quản lý: Dữ liệu được trích xuất trực tiếp từ hệ thống chứng từ kế toán của TradeFlow ERP.";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;

        ws.Columns(1, 6).AdjustToContents(10.0, 50.0);
    }

    private void BuildManagementCustomersSheet(
        IXLWorksheet ws,
        List<CustomerDebtSummaryRowDto> customers,
        CompanySettings company,
        DebtManagementExportRequest request)
    {
        ws.ShowGridLines = true;

        int row = 1;
        ws.Cell(row, 1).Value = "TỔNG HỢP CÔNG NỢ PHẢI THU KHÁCH HÀNG";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 1).Style.Font.FontColor = BrandIndigo;
        row++;

        ws.Cell(row, 1).Value = $"Tổng số khách hàng: {customers.Count} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;
        row += 2;

        int headerRow = row;
        string[] headers = new[]
        {
            "STT", "Mã KH", "Tên khách hàng", "Tên công ty / Pháp nhân", "Mã số thuế",
            "Số điện thoại", "Loại KH", "Dư nợ đầu kỳ", "Bán trong kỳ (+)", "Đã thu tiền (-)",
            "Chiết khấu/Giảm (-)", "Dư nợ cuối kỳ", "Trạng thái"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = BrandHeaderBg;
            cell.Style.Alignment.Horizontal = (i == 0 || i == 5 || i == 6 || i == 12) ? XLAlignmentHorizontalValues.Center
                : (i >= 7 && i <= 11) ? XLAlignmentHorizontalValues.Right
                : XLAlignmentHorizontalValues.Left;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = BorderGray;
        }
        row++;

        int stt = 1;
        foreach (var c in customers)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 2).Value = c.Code;
            ws.Cell(row, 2).Style.Font.Bold = true;

            ws.Cell(row, 3).Value = c.Name;
            ws.Cell(row, 4).Value = c.CompanyName ?? "-";
            ws.Cell(row, 5).Value = c.TaxCode ?? "-";

            ws.Cell(row, 6).Value = c.Phone ?? "-";
            ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 7).Value = c.IsCompany ? "Doanh nghiệp" : "Cá nhân";
            ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 8).Value = (double)c.OpeningReceivable;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 9).Value = (double)c.PeriodSales;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 10).Value = (double)c.PaymentsReceived;
            ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 11).Value = (double)c.DiscountsOrAdjustments;
            ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 12).Value = (double)c.ClosingReceivable;
            ws.Cell(row, 12).Style.NumberFormat.Format = "#,##0 ₫";
            ws.Cell(row, 12).Style.Font.Bold = true;
            if (c.ClosingReceivable > 0) ws.Cell(row, 12).Style.Font.FontColor = ReceivableRed;

            ws.Cell(row, 13).Value = c.ClosingReceivable > 0 ? "Còn nợ" : "Đã thanh toán đủ";

            for (int col = 1; col <= 13; col++)
            {
                ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(row, col).Style.Border.OutsideBorderColor = BorderGray;
            }
            row++;
        }

        // Summary row
        int totalRow = row;
        ws.Cell(totalRow, 1).Value = "TỔNG CỘNG";
        ws.Cell(totalRow, 1).Style.Font.Bold = true;
        ws.Cell(totalRow, 3).Value = $"Tổng cộng {customers.Count} khách hàng";
        ws.Cell(totalRow, 3).Style.Font.Bold = true;

        ws.Cell(totalRow, 8).Value = (double)customers.Sum(x => x.OpeningReceivable);
        ws.Cell(totalRow, 8).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 8).Style.Font.Bold = true;

        ws.Cell(totalRow, 9).Value = (double)customers.Sum(x => x.PeriodSales);
        ws.Cell(totalRow, 9).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 9).Style.Font.Bold = true;

        ws.Cell(totalRow, 10).Value = (double)customers.Sum(x => x.PaymentsReceived);
        ws.Cell(totalRow, 10).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 10).Style.Font.Bold = true;

        ws.Cell(totalRow, 11).Value = (double)customers.Sum(x => x.DiscountsOrAdjustments);
        ws.Cell(totalRow, 11).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 11).Style.Font.Bold = true;

        ws.Cell(totalRow, 12).Value = (double)customers.Sum(x => x.ClosingReceivable);
        ws.Cell(totalRow, 12).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 12).Style.Font.Bold = true;
        ws.Cell(totalRow, 12).Style.Font.FontColor = ReceivableRed;

        ws.Range(totalRow, 1, totalRow, 13).Style.Fill.BackgroundColor = TotalRowBg;
        ws.Range(totalRow, 1, totalRow, 13).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        ws.Range(totalRow, 1, totalRow, 13).Style.Border.BottomBorder = XLBorderStyleValues.Double;

        ws.Range(headerRow, 1, totalRow, 13).SetAutoFilter();
        ws.SheetView.FreezeRows(headerRow);
        ws.Columns(1, 13).AdjustToContents(8.0, 45.0);
    }

    private void BuildManagementSuppliersSheet(
        IXLWorksheet ws,
        List<SupplierDebtSummaryRowDto> suppliers,
        CompanySettings company,
        DebtManagementExportRequest request)
    {
        ws.ShowGridLines = true;

        int row = 1;
        ws.Cell(row, 1).Value = "TỔNG HỢP CÔNG NỢ PHẢI TRẢ NHÀ CUNG CẤP";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 1).Style.Font.FontColor = BrandIndigo;
        row++;

        ws.Cell(row, 1).Value = $"Tổng số nhà cung cấp: {suppliers.Count} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;
        row += 2;

        int headerRow = row;
        string[] headers = new[]
        {
            "STT", "Mã NCC", "Tên nhà cung cấp", "Tên pháp nhân công ty", "Mã số thuế",
            "Số điện thoại", "Dư nợ đầu kỳ", "Mua trong kỳ (+)", "Đã thanh toán (-)",
            "Giảm trừ / Điều chỉnh (-)", "Dư nợ cuối kỳ", "Trạng thái"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = BrandHeaderBg;
            cell.Style.Alignment.Horizontal = (i == 0 || i == 5 || i == 11) ? XLAlignmentHorizontalValues.Center
                : (i >= 6 && i <= 10) ? XLAlignmentHorizontalValues.Right
                : XLAlignmentHorizontalValues.Left;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = BorderGray;
        }
        row++;

        int stt = 1;
        foreach (var s in suppliers)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 2).Value = s.Code;
            ws.Cell(row, 2).Style.Font.Bold = true;

            ws.Cell(row, 3).Value = s.Name;
            ws.Cell(row, 4).Value = s.CompanyName ?? "-";
            ws.Cell(row, 5).Value = s.TaxCode ?? "-";

            ws.Cell(row, 6).Value = s.Phone ?? "-";
            ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 7).Value = (double)s.OpeningPayable;
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 8).Value = (double)s.PeriodPurchases;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 9).Value = (double)s.PaymentsMade;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 10).Value = (double)s.AdjustmentsOrReductions;
            ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 11).Value = (double)s.ClosingPayable;
            ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0 ₫";
            ws.Cell(row, 11).Style.Font.Bold = true;
            if (s.ClosingPayable > 0) ws.Cell(row, 11).Style.Font.FontColor = ReceivableRed;

            ws.Cell(row, 12).Value = s.ClosingPayable > 0 ? "Còn nợ" : "Không có nợ đọng";

            for (int col = 1; col <= 12; col++)
            {
                ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(row, col).Style.Border.OutsideBorderColor = BorderGray;
            }
            row++;
        }

        // Summary row
        int totalRow = row;
        ws.Cell(totalRow, 1).Value = "TỔNG CỘNG";
        ws.Cell(totalRow, 1).Style.Font.Bold = true;
        ws.Cell(totalRow, 3).Value = $"Tổng cộng {suppliers.Count} nhà cung cấp";
        ws.Cell(totalRow, 3).Style.Font.Bold = true;

        ws.Cell(totalRow, 7).Value = (double)suppliers.Sum(x => x.OpeningPayable);
        ws.Cell(totalRow, 7).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 7).Style.Font.Bold = true;

        ws.Cell(totalRow, 8).Value = (double)suppliers.Sum(x => x.PeriodPurchases);
        ws.Cell(totalRow, 8).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 8).Style.Font.Bold = true;

        ws.Cell(totalRow, 9).Value = (double)suppliers.Sum(x => x.PaymentsMade);
        ws.Cell(totalRow, 9).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 9).Style.Font.Bold = true;

        ws.Cell(totalRow, 10).Value = (double)suppliers.Sum(x => x.AdjustmentsOrReductions);
        ws.Cell(totalRow, 10).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 10).Style.Font.Bold = true;

        ws.Cell(totalRow, 11).Value = (double)suppliers.Sum(x => x.ClosingPayable);
        ws.Cell(totalRow, 11).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 11).Style.Font.Bold = true;

        ws.Range(totalRow, 1, totalRow, 12).Style.Fill.BackgroundColor = TotalRowBg;
        ws.Range(totalRow, 1, totalRow, 12).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        ws.Range(totalRow, 1, totalRow, 12).Style.Border.BottomBorder = XLBorderStyleValues.Double;

        ws.Range(headerRow, 1, totalRow, 12).SetAutoFilter();
        ws.SheetView.FreezeRows(headerRow);
        ws.Columns(1, 12).AdjustToContents(8.0, 45.0);
    }

    private void BuildManagementAllMovementsSheet(
        IXLWorksheet ws,
        List<ManagementMovementRowDto> movements,
        CompanySettings company,
        DebtManagementExportRequest request)
    {
        ws.ShowGridLines = true;

        int row = 1;
        ws.Cell(row, 1).Value = "NHẬT KÝ CHI TIẾT GIAO DỊCH CÔNG NỢ TOÀN HỆ THỐNG";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 1).Style.Font.FontColor = BrandIndigo;
        row++;

        ws.Cell(row, 1).Value = $"Tổng số giao dịch: {movements.Count} | Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;
        row += 2;

        int headerRow = row;
        string[] headers = new[]
        {
            "STT", "Thời gian", "Đối tác", "Mã đối tác", "Tên đối tác",
            "Mã chứng từ", "Loại chứng từ", "Chứng từ gốc", "Phát sinh phải thu", "Phát sinh phải trả",
            "Đã thanh toán (-)", "Dư nợ sau GD", "Phương thức", "Ghi chú"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = BrandHeaderBg;
            cell.Style.Alignment.Horizontal = (i == 0 || i == 1 || i == 2) ? XLAlignmentHorizontalValues.Center
                : (i >= 8 && i <= 11) ? XLAlignmentHorizontalValues.Right
                : XLAlignmentHorizontalValues.Left;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = BorderGray;
        }
        row++;

        int stt = 1;
        foreach (var m in movements)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 2).Value = m.DocumentDate.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 3).Value = m.PartnerType == PartnerType.Customer ? "Khách hàng" : "Nhà cung cấp";
            ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 4).Value = m.PartnerCode;
            ws.Cell(row, 4).Style.Font.Bold = true;

            ws.Cell(row, 5).Value = m.PartnerName;
            ws.Cell(row, 6).Value = m.DocumentCode;
            ws.Cell(row, 6).Style.Font.Bold = true;

            ws.Cell(row, 7).Value = m.DocumentType;
            ws.Cell(row, 8).Value = m.ReferenceCode ?? "-";

            ws.Cell(row, 9).Value = (double)m.ReceivableIncrease;
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 10).Value = (double)m.PayableIncrease;
            ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0 ₫";

            ws.Cell(row, 11).Value = (double)m.PaymentOrReduction;
            ws.Cell(row, 11).Style.NumberFormat.Format = "#,##0 ₫";
            if (m.PaymentOrReduction > 0) ws.Cell(row, 11).Style.Font.FontColor = SuccessGreen;

            ws.Cell(row, 12).Value = (double)m.RunningBalance;
            ws.Cell(row, 12).Style.NumberFormat.Format = "#,##0 ₫";
            ws.Cell(row, 12).Style.Font.Bold = true;

            ws.Cell(row, 13).Value = m.PaymentMethod ?? "-";
            ws.Cell(row, 14).Value = m.Notes ?? "-";

            for (int col = 1; col <= 14; col++)
            {
                ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(row, col).Style.Border.OutsideBorderColor = BorderGray;
            }
            row++;
        }

        // Summary row
        int totalRow = row;
        ws.Cell(totalRow, 1).Value = "TỔNG CỘNG";
        ws.Cell(totalRow, 1).Style.Font.Bold = true;
        ws.Cell(totalRow, 5).Value = $"Tổng số {movements.Count} giao dịch";
        ws.Cell(totalRow, 5).Style.Font.Bold = true;

        ws.Cell(totalRow, 9).Value = (double)movements.Sum(x => x.ReceivableIncrease);
        ws.Cell(totalRow, 9).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 9).Style.Font.Bold = true;

        ws.Cell(totalRow, 10).Value = (double)movements.Sum(x => x.PayableIncrease);
        ws.Cell(totalRow, 10).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 10).Style.Font.Bold = true;

        ws.Cell(totalRow, 11).Value = (double)movements.Sum(x => x.PaymentOrReduction);
        ws.Cell(totalRow, 11).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(totalRow, 11).Style.Font.Bold = true;

        ws.Range(totalRow, 1, totalRow, 14).Style.Fill.BackgroundColor = TotalRowBg;
        ws.Range(totalRow, 1, totalRow, 14).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        ws.Range(totalRow, 1, totalRow, 14).Style.Border.BottomBorder = XLBorderStyleValues.Double;

        ws.Range(headerRow, 1, totalRow, 14).SetAutoFilter();
        ws.SheetView.FreezeRows(headerRow);
        ws.Columns(1, 14).AdjustToContents(8.0, 45.0);
    }

    // =========================================================================
    // DATA LOADERS & DOMAIN RECONCILIATION
    // =========================================================================

    private async Task<List<PartnerStatementSummaryDto>> LoadPartnerStatementsAsync(
        PartnerDebtStatementExportRequest request,
        CancellationToken cancellationToken)
    {
        var summaries = new List<PartnerStatementSummaryDto>();

        // 1. Determine partner IDs and types to load
        var targetCustomerIds = new List<int>();
        var targetSupplierIds = new List<int>();

        if (request.Scope == DebtExportScope.CurrentPartner)
        {
            if (!request.PartnerIds.Any())
            {
                throw new ArgumentException("Chưa chỉ định đối tác cần xuất báo cáo công nợ.", nameof(request.PartnerIds));
            }
            if (request.PartnerType == PartnerType.Supplier)
                targetSupplierIds.AddRange(request.PartnerIds);
            else
                targetCustomerIds.AddRange(request.PartnerIds);
        }
        else if (request.Scope == DebtExportScope.SelectedPartners)
        {
            if (!request.PartnerIds.Any())
            {
                throw new ArgumentException("Chưa chọn đối tác nào để xuất báo cáo công nợ.", nameof(request.PartnerIds));
            }
            if (request.PartnerType.HasValue)
            {
                if (request.PartnerType.Value == PartnerType.Customer) targetCustomerIds.AddRange(request.PartnerIds);
                else targetSupplierIds.AddRange(request.PartnerIds);
            }
            else
            {
                // Verify existence in Customers vs Suppliers
                var foundCusts = await _context.Customers.AsNoTracking().Where(c => request.PartnerIds.Contains(c.Id)).Select(c => c.Id).ToListAsync(cancellationToken);
                var foundSups = await _context.Suppliers.AsNoTracking().Where(s => request.PartnerIds.Contains(s.Id)).Select(s => s.Id).ToListAsync(cancellationToken);
                targetCustomerIds.AddRange(foundCusts);
                targetSupplierIds.AddRange(foundSups);
            }
        }
        else
        {
            // All filtered or all partners according to PartnerType
            if (request.PartnerType == PartnerType.Customer)
            {
                targetCustomerIds = await _context.Customers.AsNoTracking().Select(c => c.Id).ToListAsync(cancellationToken);
            }
            else if (request.PartnerType == PartnerType.Supplier)
            {
                targetSupplierIds = await _context.Suppliers.AsNoTracking().Select(s => s.Id).ToListAsync(cancellationToken);
            }
            else
            {
                targetCustomerIds = await _context.Customers.AsNoTracking().Select(c => c.Id).ToListAsync(cancellationToken);
                targetSupplierIds = await _context.Suppliers.AsNoTracking().Select(s => s.Id).ToListAsync(cancellationToken);
            }
        }

        DateTime? fromDateUtc = request.FromDate.HasValue ? DateTime.SpecifyKind(request.FromDate.Value.Date, DateTimeKind.Utc) : null;
        DateTime? toDateUtc = request.ToDate.HasValue ? DateTime.SpecifyKind(request.ToDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc) : null;

        // 2. Load Customers
        if (targetCustomerIds.Any())
        {
            var customers = await _context.Customers.AsNoTracking()
                .Where(c => targetCustomerIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

            var invoices = await _context.Invoices.AsNoTracking()
                .Include(i => i.Items)
                .Where(i => i.CustomerId.HasValue && targetCustomerIds.Contains(i.CustomerId.Value) && i.Status != InvoiceStatus.Cancelled)
                .ToListAsync(cancellationToken);

            var invoicedOrderIds = invoices.Where(i => i.SalesOrderId.HasValue).Select(i => i.SalesOrderId!.Value).ToHashSet();

            var salesOrders = await _context.SalesOrders.AsNoTracking()
                .Where(o => targetCustomerIds.Contains(o.CustomerId) && o.Status == SalesOrderStatus.Confirmed && !invoicedOrderIds.Contains(o.Id))
                .ToListAsync(cancellationToken);

            var payments = await _context.PartnerPayments.AsNoTracking()
                .Where(p => p.PartnerType == (int)PartnerType.Customer && targetCustomerIds.Contains(p.PartnerId))
                .ToListAsync(cancellationToken);

            foreach (var cust in customers)
            {
                var custInvoices = invoices.Where(i => i.CustomerId == cust.Id).ToList();
                var custOrders = salesOrders.Where(o => o.CustomerId == cust.Id).ToList();
                var custPayments = payments.Where(p => p.PartnerId == cust.Id).ToList();

                // Compute Opening Balance (prior to fromDateUtc)
                decimal openingDebits = 0m;
                decimal openingCredits = 0m;
                decimal openingDiscounts = 0m;

                if (fromDateUtc.HasValue)
                {
                    // Debits before period: Invoices + Uninvoiced Orders
                    openingDebits += custInvoices.Where(i => i.InvoiceDate < fromDateUtc.Value).Sum(i => i.GrandTotal);
                    openingDebits += custOrders.Where(o => o.OrderDate < fromDateUtc.Value).Sum(o => o.GrandTotal);

                    // Credits before period: Payments
                    openingCredits += custPayments.Where(p => p.PaymentDate < fromDateUtc.Value && p.TransactionType == DebtTransactionType.Payment).Sum(p => p.Amount);
                    openingDiscounts += custPayments.Where(p => p.PaymentDate < fromDateUtc.Value && p.TransactionType == DebtTransactionType.PaymentDiscount).Sum(p => p.Amount);
                }

                decimal openingBalance = Math.Max(0m, openingDebits - openingCredits - openingDiscounts);

                // Filter transactions within period
                var periodTx = new List<PartnerLinkedTransactionDto>();

                // Period Invoices
                var pInvoices = custInvoices.Where(i => (!fromDateUtc.HasValue || i.InvoiceDate >= fromDateUtc.Value) &&
                                                        (!toDateUtc.HasValue || i.InvoiceDate <= toDateUtc.Value));
                foreach (var inv in pInvoices)
                {
                    var lines = inv.Items.OrderBy(x => x.SortOrder).Select(it => new PartnerTransactionLineItemDto
                    {
                        SortOrder = it.SortOrder,
                        ProductCode = it.ProductCode,
                        ProductName = it.ProductName,
                        UnitName = it.UnitName,
                        Quantity = it.Quantity,
                        UnitPrice = it.UnitPrice,
                        DiscountAmount = it.DiscountAmount,
                        TaxRate = it.TaxRate,
                        TaxAmount = it.TaxAmount,
                        LineTotal = it.LineTotal
                    }).ToList();

                    periodTx.Add(new PartnerLinkedTransactionDto
                    {
                        DocumentId = inv.Id,
                        DocumentType = inv.Type == InvoiceType.VatInvoice ? "Hóa đơn VAT" : "Hóa đơn bán hàng",
                        DocumentCode = !string.IsNullOrEmpty(inv.InvoiceNo) ? inv.InvoiceNo : inv.InvoiceNumber,
                        DocumentDate = inv.InvoiceDate,
                        Amount = inv.GrandTotal,
                        DebitAmount = inv.GrandTotal,
                        CreditAmount = 0m,
                        OutstandingAmount = inv.GrandTotal,
                        PaidAmount = 0m,
                        Status = inv.Status switch { InvoiceStatus.Issued => "Đã phát hành", InvoiceStatus.Pending => "Chờ thanh toán", _ => inv.Status.ToString() },
                        PaymentMethod = inv.PaymentMethod,
                        ReferenceCode = inv.SalesOrderId.HasValue ? $"DH-{inv.SalesOrderId}" : null,
                        Notes = inv.Notes,
                        LineItems = lines
                    });
                }

                // Period Orders (confirmed uninvoiced)
                var pOrders = custOrders.Where(o => (!fromDateUtc.HasValue || o.OrderDate >= fromDateUtc.Value) &&
                                                    (!toDateUtc.HasValue || o.OrderDate <= toDateUtc.Value));
                foreach (var ord in pOrders)
                {
                    periodTx.Add(new PartnerLinkedTransactionDto
                    {
                        DocumentId = ord.Id,
                        DocumentType = "Đơn bán hàng",
                        DocumentCode = ord.Code,
                        DocumentDate = ord.OrderDate,
                        Amount = ord.GrandTotal,
                        DebitAmount = ord.GrandTotal,
                        CreditAmount = 0m,
                        OutstandingAmount = ord.GrandTotal,
                        PaidAmount = 0m,
                        Status = "Đã xác nhận",
                        PaymentMethod = "Chờ lập hóa đơn",
                        Notes = ord.Notes
                    });
                }

                // Period Payments / Adjustments / Discounts
                var pPayments = custPayments.Where(p => (!fromDateUtc.HasValue || p.PaymentDate >= fromDateUtc.Value) &&
                                                        (!toDateUtc.HasValue || p.PaymentDate <= toDateUtc.Value));
                foreach (var pay in pPayments)
                {
                    string docType = pay.TransactionType switch
                    {
                        DebtTransactionType.Payment => "Phiếu thu tiền",
                        DebtTransactionType.Adjustment => "Điều chỉnh công nợ",
                        DebtTransactionType.PaymentDiscount => "Chiết khấu thanh toán",
                        _ => "Phiếu thu"
                    };

                    periodTx.Add(new PartnerLinkedTransactionDto
                    {
                        DocumentId = pay.Id,
                        DocumentType = docType,
                        DocumentCode = pay.PaymentCode,
                        DocumentDate = pay.PaymentDate,
                        Amount = pay.Amount,
                        DebitAmount = 0m,
                        CreditAmount = pay.Amount,
                        OutstandingAmount = 0m,
                        PaidAmount = pay.Amount,
                        Status = "Đã ghi nhận",
                        PaymentMethod = pay.PaymentMethod,
                        ReferenceCode = pay.ReferenceCode,
                        Notes = pay.Notes
                    });
                }

                periodTx = periodTx.OrderBy(t => t.DocumentDate).ToList();

                decimal totalDebits = periodTx.Sum(x => x.DebitAmount);
                decimal totalCredits = periodTx.Where(x => x.DocumentType != "Chiết khấu thanh toán").Sum(x => x.CreditAmount);
                decimal totalDiscounts = periodTx.Where(x => x.DocumentType == "Chiết khấu thanh toán").Sum(x => x.CreditAmount);
                decimal closingBalance = Math.Max(0m, openingBalance + totalDebits - totalCredits - totalDiscounts);

                summaries.Add(new PartnerStatementSummaryDto
                {
                    PartnerId = cust.Id,
                    PartnerType = PartnerType.Customer,
                    Code = cust.Code,
                    Name = cust.Name,
                    CompanyName = cust.CompanyName,
                    TaxCode = cust.TaxCode,
                    Phone = cust.Phone,
                    Email = cust.Email,
                    Address = cust.Address,
                    OpeningBalance = openingBalance,
                    TotalDebits = totalDebits,
                    TotalCredits = totalCredits,
                    TotalDiscounts = totalDiscounts,
                    ClosingBalance = closingBalance,
                    BalanceDirectionDescription = closingBalance > 0
                        ? "Số dư phản ánh số tiền khách hàng còn nợ doanh nghiệp cần thu hồi"
                        : "Khách hàng đã hoàn tất nghĩa vụ thanh toán",
                    Transactions = periodTx
                });
            }
        }

        // 3. Load Suppliers
        if (targetSupplierIds.Any())
        {
            var suppliers = await _context.Suppliers.AsNoTracking()
                .Where(s => targetSupplierIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            var payments = await _context.PartnerPayments.AsNoTracking()
                .Where(p => p.PartnerType == (int)PartnerType.Supplier && targetSupplierIds.Contains(p.PartnerId))
                .ToListAsync(cancellationToken);

            foreach (var sup in suppliers)
            {
                var supPayments = payments.Where(p => p.PartnerId == sup.Id).ToList();

                decimal openingCredits = 0m;
                if (fromDateUtc.HasValue)
                {
                    openingCredits += supPayments.Where(p => p.PaymentDate < fromDateUtc.Value && p.TransactionType == DebtTransactionType.Payment).Sum(p => p.Amount);
                }
                decimal openingBalance = 0m; // Phase 6 purchasing pending

                var periodTx = new List<PartnerLinkedTransactionDto>();
                var pPayments = supPayments.Where(p => (!fromDateUtc.HasValue || p.PaymentDate >= fromDateUtc.Value) &&
                                                        (!toDateUtc.HasValue || p.PaymentDate <= toDateUtc.Value));
                foreach (var pay in pPayments)
                {
                    periodTx.Add(new PartnerLinkedTransactionDto
                    {
                        DocumentId = pay.Id,
                        DocumentType = pay.TransactionType == DebtTransactionType.Payment ? "Phiếu chi thanh toán" : "Điều chỉnh nợ NCC",
                        DocumentCode = pay.PaymentCode,
                        DocumentDate = pay.PaymentDate,
                        Amount = pay.Amount,
                        DebitAmount = 0m,
                        CreditAmount = pay.Amount,
                        OutstandingAmount = 0m,
                        PaidAmount = pay.Amount,
                        Status = "Đã chi tiền",
                        PaymentMethod = pay.PaymentMethod,
                        ReferenceCode = pay.ReferenceCode,
                        Notes = pay.Notes
                    });
                }

                periodTx = periodTx.OrderBy(t => t.DocumentDate).ToList();
                decimal totalDebits = 0m;
                decimal totalCredits = periodTx.Sum(x => x.CreditAmount);
                decimal closingBalance = 0m;

                summaries.Add(new PartnerStatementSummaryDto
                {
                    PartnerId = sup.Id,
                    PartnerType = PartnerType.Supplier,
                    Code = sup.Code,
                    Name = sup.Name,
                    CompanyName = sup.CompanyName,
                    TaxCode = sup.TaxCode,
                    Phone = sup.Phone,
                    Email = sup.Email,
                    Address = sup.Address,
                    OpeningBalance = openingBalance,
                    TotalDebits = totalDebits,
                    TotalCredits = totalCredits,
                    TotalDiscounts = 0m,
                    ClosingBalance = closingBalance,
                    BalanceDirectionDescription = "Phân hệ Mua hàng đang chuẩn bị kích hoạt; chưa phát sinh công nợ phải trả",
                    Transactions = periodTx
                });
            }
        }

        return summaries;
    }

    private async Task<(List<CustomerDebtSummaryRowDto>, List<SupplierDebtSummaryRowDto>, List<ManagementMovementRowDto>)> LoadManagementDatasetAsync(
        DebtManagementExportRequest request,
        CancellationToken cancellationToken)
    {
        var stmtReq = new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.AllFiltered,
            FromDate = request.FromDate,
            ToDate = request.ToDate,
            DatePresetTitle = request.DatePresetTitle,
            IncludeLineDetails = false
        };

        var allStatements = await LoadPartnerStatementsAsync(stmtReq, cancellationToken);

        var customersList = allStatements
            .Where(x => x.PartnerType == PartnerType.Customer)
            .Select(c => new CustomerDebtSummaryRowDto
            {
                Id = c.PartnerId,
                Code = c.Code,
                Name = c.Name,
                CompanyName = c.CompanyName,
                TaxCode = c.TaxCode,
                Phone = c.Phone,
                IsCompany = !string.IsNullOrEmpty(c.CompanyName) || !string.IsNullOrEmpty(c.TaxCode),
                OpeningReceivable = c.OpeningBalance,
                PeriodSales = c.TotalDebits,
                PaymentsReceived = c.TotalCredits,
                DiscountsOrAdjustments = c.TotalDiscounts,
                ClosingReceivable = c.ClosingBalance
            })
            .OrderByDescending(x => x.ClosingReceivable)
            .ToList();

        var suppliersList = allStatements
            .Where(x => x.PartnerType == PartnerType.Supplier)
            .Select(s => new SupplierDebtSummaryRowDto
            {
                Id = s.PartnerId,
                Code = s.Code,
                Name = s.Name,
                CompanyName = s.CompanyName,
                TaxCode = s.TaxCode,
                Phone = s.Phone,
                OpeningPayable = s.OpeningBalance,
                PeriodPurchases = s.TotalDebits,
                PaymentsMade = s.TotalCredits,
                AdjustmentsOrReductions = s.TotalDiscounts,
                ClosingPayable = s.ClosingBalance
            })
            .ToList();

        var allMovements = new List<ManagementMovementRowDto>();
        foreach (var stmt in allStatements)
        {
            decimal runBal = stmt.OpeningBalance;
            foreach (var t in stmt.Transactions)
            {
                runBal += t.DebitAmount - t.CreditAmount;
                allMovements.Add(new ManagementMovementRowDto
                {
                    PartnerType = stmt.PartnerType,
                    PartnerCode = stmt.Code,
                    PartnerName = stmt.Name,
                    Phone = stmt.Phone,
                    DocumentCode = t.DocumentCode,
                    DocumentDate = t.DocumentDate,
                    DocumentType = t.DocumentType,
                    ReferenceCode = t.ReferenceCode,
                    ReceivableIncrease = stmt.PartnerType == PartnerType.Customer ? t.DebitAmount : 0m,
                    PayableIncrease = stmt.PartnerType == PartnerType.Supplier ? t.DebitAmount : 0m,
                    PaymentOrReduction = t.CreditAmount,
                    RunningBalance = runBal,
                    PaymentMethod = t.PaymentMethod,
                    Notes = t.Notes
                });
            }
        }

        allMovements = allMovements.OrderByDescending(m => m.DocumentDate).ToList();

        return (customersList, suppliersList, allMovements);
    }

    // =========================================================================
    // EXCEL HELPER UTILITIES
    // =========================================================================

    public static string SafeSheetName(string desiredName, HashSet<string> existingNames)
    {
        if (string.IsNullOrWhiteSpace(desiredName))
        {
            desiredName = "Sheet";
        }

        var invalidChars = new[] { '\\', '/', '?', '*', '[', ']', ':', '\'', '"' };
        var clean = new StringBuilder();
        foreach (var ch in desiredName)
        {
            clean.Append(invalidChars.Contains(ch) ? '-' : ch);
        }

        var trimmed = clean.ToString().Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) trimmed = "Sheet";

        // Limit base length to 26 to allow room for unique suffixes "_1", "_2", etc. (Excel max = 31)
        if (trimmed.Length > 26)
        {
            trimmed = trimmed.Substring(0, 26).TrimEnd('-', '_', ' ');
        }

        var finalName = trimmed;
        int counter = 1;
        while (existingNames.Contains(finalName))
        {
            var suffix = $"_{counter++}";
            int maxBaseLen = 31 - suffix.Length;
            var basePart = trimmed.Length > maxBaseLen ? trimmed.Substring(0, maxBaseLen) : trimmed;
            finalName = basePart + suffix;
        }

        existingNames.Add(finalName);
        return finalName;
    }

    private static void AddMetricRow(IXLWorksheet ws, ref int row, string label, decimal amount, string hint)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;

        ws.Cell(row, 3).Value = (double)amount;
        ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0 ₫";
        ws.Cell(row, 3).Style.Font.Bold = true;

        ws.Cell(row, 4).Value = hint;
        ws.Cell(row, 4).Style.Font.Italic = true;
        ws.Cell(row, 4).Style.Font.FontColor = MutedText;

        ws.Range(row, 1, row, 6).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ws.Range(row, 1, row, 6).Style.Border.BottomBorderColor = BorderGray;
        row++;
    }

    private static void AddInfoRow(IXLWorksheet ws, ref int row, string lbl1, string val1, string lbl2, string val2)
    {
        ws.Cell(row, 1).Value = lbl1;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = val1;

        ws.Cell(row, 4).Value = lbl2;
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = val2;

        ws.Range(row, 1, row, 6).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
        ws.Range(row, 1, row, 6).Style.Border.BottomBorderColor = BorderGray;
        row++;
    }
}

// Supporting internal management models
public class CustomerDebtSummaryRowDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public bool IsCompany { get; set; }
    public decimal OpeningReceivable { get; set; }
    public decimal PeriodSales { get; set; }
    public decimal PaymentsReceived { get; set; }
    public decimal DiscountsOrAdjustments { get; set; }
    public decimal ClosingReceivable { get; set; }
}

public class SupplierDebtSummaryRowDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public decimal OpeningPayable { get; set; }
    public decimal PeriodPurchases { get; set; }
    public decimal PaymentsMade { get; set; }
    public decimal AdjustmentsOrReductions { get; set; }
    public decimal ClosingPayable { get; set; }
}

public class ManagementMovementRowDto
{
    public PartnerType PartnerType { get; set; }
    public string PartnerCode { get; set; } = string.Empty;
    public string PartnerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string DocumentCode { get; set; } = string.Empty;
    public DateTime DocumentDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? ReferenceCode { get; set; }
    public decimal ReceivableIncrease { get; set; }
    public decimal PayableIncrease { get; set; }
    public decimal PaymentOrReduction { get; set; }
    public decimal RunningBalance { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}
