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

    // Palette Colors for Professional ClosedXML Styling
    private static readonly XLColor BrandDark = XLColor.FromHtml("#0F172A");       // Slate 900
    private static readonly XLColor BrandNavy = XLColor.FromHtml("#1E293B");       // Slate 800
    private static readonly XLColor BrandIndigo = XLColor.FromHtml("#4F46E5");     // Indigo Primary
    private static readonly XLColor TableHeaderBg = XLColor.FromHtml("#1E293B");   // Navy Table Header
    private static readonly XLColor SectionHeaderBg = XLColor.FromHtml("#E2E8F0"); // Slate 200
    private static readonly XLColor SubHeaderBg = XLColor.FromHtml("#F8FAFC");     // Slate 50
    private static readonly XLColor TotalRowBg = XLColor.FromHtml("#EEF2F6");      // Cool Light Gray
    private static readonly XLColor BorderGray = XLColor.FromHtml("#CBD5E1");      // Slate 300
    private static readonly XLColor BorderLight = XLColor.FromHtml("#E2E8F0");     // Slate 200
    private static readonly XLColor DangerRed = XLColor.FromHtml("#DC2626");       // Overdue / Receivable Red
    private static readonly XLColor SuccessGreen = XLColor.FromHtml("#16A34A");    // Settled Green
    private static readonly XLColor WarningAmber = XLColor.FromHtml("#D97706");    // Warning Amber
    private static readonly XLColor MutedText = XLColor.FromHtml("#64748B");       // Slate 500

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
    // 1. “XUẤT FILE CÔNG NỢ” — BÁO CÁO CÔNG NỢ ĐỐI TÁC (1 HOẶC NHIỀU ĐỐI TÁC ĐƯỢC CHỌN)
    // =========================================================================

    public async Task<byte[]> ExportPartnerDebtStatementAsync(
        PartnerDebtStatementExportRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _authService.AuthorizeAsync(user, "Permission:Customers:View");
        if (!authResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập hoặc xuất dữ liệu công nợ đối tác.");
        }

        var company = await _context.CompanySettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
                      ?? new CompanySettings("CÔNG TY CỔ PHẦN TRADEFLOW");

        var dataset = await LoadExportDatasetAsync(
            scope: request.Scope,
            partnerType: request.PartnerType,
            partnerIds: request.PartnerIds,
            filter: null,
            fromDate: request.FromDate,
            toDate: request.ToDate,
            datePresetTitle: request.DatePresetTitle,
            cancellationToken: cancellationToken);

        using var workbook = new XLWorkbook();
        var existingSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool isSinglePartner = dataset.Partners.Count == 1;
        string reportTitle = isSinglePartner
            ? $"BÁO CÁO CHI TIẾT CÔNG NỢ — [{dataset.Partners[0].Code}] {dataset.Partners[0].Name.ToUpper()}"
            : "BÁO CÁO TỔNG HỢP VÀ CHI TIẾT CÔNG NỢ ĐỐI TÁC";

        // SHEET A: Tổng hợp công nợ
        var wsA = workbook.Worksheets.Add(SafeSheetName("Tổng hợp công nợ", existingSheetNames));
        BuildSheetA_Summary(wsA, dataset, company, request.DatePresetTitle, reportTitle, isSinglePartner);

        // SHEET B: Chi tiết công nợ theo chứng từ
        var wsB = workbook.Worksheets.Add(SafeSheetName("Chi tiết theo chứng từ", existingSheetNames));
        BuildSheetB_Documents(wsB, dataset, company, request.DatePresetTitle, isSinglePartner);

        // SHEET C: Chi tiết từng mặt hàng / dịch vụ
        var wsC = workbook.Worksheets.Add(SafeSheetName("Chi tiết từng mặt hàng", existingSheetNames));
        BuildSheetC_LineItems(wsC, dataset, company, request.DatePresetTitle, request, isSinglePartner);

        // SHEET D: Lịch sử thu, chi và điều chỉnh công nợ
        var wsD = workbook.Worksheets.Add(SafeSheetName("Lịch sử thu, chi, điều chỉnh", existingSheetNames));
        BuildSheetD_Transactions(wsD, dataset, company, request.DatePresetTitle, isSinglePartner);

        // Optional individual sheets for multi-partner selection if requested
        if (!isSinglePartner && dataset.Partners.Count <= 20)
        {
            foreach (var p in dataset.Partners)
            {
                var pDataset = FilterDatasetForSinglePartner(dataset, p.PartnerId, p.PartnerType);
                string pSheetName = SafeSheetName($"{p.Code} - {p.Name}", existingSheetNames);
                var wsP = workbook.Worksheets.Add(pSheetName);
                BuildSheetB_Documents(wsP, pDataset, company, request.DatePresetTitle, true);
            }
        }

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);

        await _auditService.LogAsync(
            AuditEventType.DebtExported,
            user.Identity?.Name ?? "System",
            "DebtStatement",
            string.Join(",", dataset.Partners.Select(p => p.Code).Take(10)),
            $"Xuất file công nợ ({dataset.Partners.Count} đối tác, {dataset.Documents.Count} chứng từ, {dataset.LineItems.Count} dòng hàng). Kỳ: {request.DatePresetTitle ?? "Tùy chọn"}.",
            cancellationToken: cancellationToken);

        return memoryStream.ToArray();
    }

    // =========================================================================
    // 2. “XUẤT FILE QUẢN LÝ CÔNG NỢ EXCEL” — BÁO CÁO DOANH NGHIỆP TOÀN HỆ THỐNG
    // =========================================================================

    public async Task<byte[]> ExportDebtManagementWorkbookAsync(
        DebtManagementExportRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _authService.AuthorizeAsync(user, "Permission:Customers:View");
        if (!authResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền truy cập hoặc xuất sổ quản lý công nợ.");
        }

        var company = await _context.CompanySettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken)
                      ?? new CompanySettings("CÔNG TY CỔ PHẦN TRADEFLOW");

        var targetIds = new List<int>();
        if (request.Scope == DebtExportScope.SelectedPartners)
        {
            targetIds.AddRange(request.SelectedCustomerIds);
            targetIds.AddRange(request.SelectedSupplierIds);
        }

        var dataset = await LoadExportDatasetAsync(
            scope: request.Scope,
            partnerType: request.PartnerType,
            partnerIds: targetIds,
            filter: request.Filter,
            fromDate: request.FromDate,
            toDate: request.ToDate,
            datePresetTitle: request.DatePresetTitle,
            cancellationToken: cancellationToken);

        using var workbook = new XLWorkbook();
        var existingSheetNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string reportTitle = "BÁO CÁO QUẢN LÝ TỔNG HỢP VÀ CHI TIẾT CÔNG NỢ DOANH NGHIỆP";

        // SHEET A: Tổng hợp công nợ
        var wsA = workbook.Worksheets.Add(SafeSheetName("Tổng hợp công nợ", existingSheetNames));
        BuildSheetA_Summary(wsA, dataset, company, request.DatePresetTitle, reportTitle, false);

        // SHEET B: Chi tiết công nợ theo chứng từ
        var wsB = workbook.Worksheets.Add(SafeSheetName("Chi tiết theo chứng từ", existingSheetNames));
        BuildSheetB_Documents(wsB, dataset, company, request.DatePresetTitle, false);

        // SHEET C: Chi tiết từng mặt hàng / dịch vụ
        var wsC = workbook.Worksheets.Add(SafeSheetName("Chi tiết từng mặt hàng", existingSheetNames));
        var dummyLineReq = new PartnerDebtStatementExportRequest { IncludeLineDetails = true };
        BuildSheetC_LineItems(wsC, dataset, company, request.DatePresetTitle, dummyLineReq, false);

        // SHEET D: Lịch sử thu, chi và điều chỉnh công nợ
        var wsD = workbook.Worksheets.Add(SafeSheetName("Lịch sử thu, chi, điều chỉnh", existingSheetNames));
        BuildSheetD_Transactions(wsD, dataset, company, request.DatePresetTitle, false);

        // Optional individual sheets for top debtor partners if requested
        if (request.IncludeIndividualPartnerSheets)
        {
            var topPartners = dataset.Partners.Where(p => p.ClosingBalance > 0)
                .OrderByDescending(p => p.ClosingBalance)
                .Take(25)
                .ToList();

            foreach (var p in topPartners)
            {
                var pDataset = FilterDatasetForSinglePartner(dataset, p.PartnerId, p.PartnerType);
                string pSheetName = SafeSheetName($"{p.Code} - {p.Name}", existingSheetNames);
                var wsP = workbook.Worksheets.Add(pSheetName);
                BuildSheetB_Documents(wsP, pDataset, company, request.DatePresetTitle, true);
            }
        }

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);

        await _auditService.LogAsync(
            AuditEventType.DebtExported,
            user.Identity?.Name ?? "System",
            "DebtManagement",
            "Consolidated",
            $"Xuất file quản lý công nợ Excel ({dataset.Partners.Count} đối tác, {dataset.Documents.Count} chứng từ, {dataset.LineItems.Count} dòng hàng). Kỳ: {request.DatePresetTitle ?? "Toàn bộ"}.",
            cancellationToken: cancellationToken);

        return memoryStream.ToArray();
    }

    // =========================================================================
    // SHEET A BUILDER — TỔNG HỢP CÔNG NỢ
    // =========================================================================

    private void BuildSheetA_Summary(
        IXLWorksheet ws,
        DebtExportDataset dataset,
        CompanySettings company,
        string? datePresetTitle,
        string reportTitle,
        bool isSinglePartner)
    {
        ws.ShowGridLines = true;
        SetupPageLayout(ws);

        int row = 1;
        row = RenderCompanyHeader(ws, row, company);

        // Title Block
        ws.Cell(row, 1).Value = reportTitle;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 15;
        ws.Cell(row, 1).Style.Font.FontColor = BrandIndigo;
        row++;

        string periodText = !string.IsNullOrEmpty(datePresetTitle)
            ? $"Kỳ đối soát / báo cáo: {datePresetTitle}"
            : "Kỳ đối soát / báo cáo: Toàn thời gian";
        ws.Cell(row, 1).Value = $"{periodText}  |  Ngày xuất file: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;
        row += 2;

        if (isSinglePartner)
        {
            var p = dataset.Partners.FirstOrDefault();
            if (p != null)
            {
                // Single Partner Profile Box
                ws.Range(row, 1, row, 7).Merge();
                ws.Cell(row, 1).Value = "THÔNG TIN ĐỐI TÁC ĐỐI SOÁT";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = SectionHeaderBg;
                ws.Cell(row, 1).Style.Font.FontColor = BrandNavy;
                row++;

                AddKeyValueRow(ws, ref row, "Mã đối tác:", p.Code, "Loại đối tác:", p.PartnerType == PartnerType.Customer ? "Khách hàng" : "Nhà cung cấp");
                AddKeyValueRow(ws, ref row, "Tên đối tác:", p.Name, "Số điện thoại:", p.Phone ?? "-");
                if (!string.IsNullOrEmpty(p.CompanyName))
                {
                    AddKeyValueRow(ws, ref row, "Tên doanh nghiệp / Pháp nhân:", p.CompanyName, "Mã số thuế:", p.TaxCode ?? "-");
                }
                AddKeyValueRow(ws, ref row, "Địa chỉ:", p.Address ?? "-", "Email:", p.Email ?? "-");
                row++;

                // Financial Summary Cards
                ws.Range(row, 1, row, 7).Merge();
                ws.Cell(row, 1).Value = "TỔNG HỢP CÂN ĐỐI DƯ NỢ TRONG KỲ";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = SectionHeaderBg;
                ws.Cell(row, 1).Style.Font.FontColor = BrandNavy;
                row++;

                AddFinancialMetricRow(ws, ref row, "1. Số dư nợ đầu kỳ:", p.OpeningBalance, p.PartnerType == PartnerType.Customer ? "Nợ trước kỳ cần thu" : "Nợ trước kỳ cần trả");
                AddFinancialMetricRow(ws, ref row, "2. Tổng phát sinh nợ trong kỳ (+):", p.TotalDebits, p.PartnerType == PartnerType.Customer ? "Doanh số hàng hóa bán phát sinh" : "Tiền nhập hàng / nghĩa vụ phát sinh");
                AddFinancialMetricRow(ws, ref row, "3. Tổng thanh toán / thu chi trong kỳ (-):", p.TotalCredits, "Đã thanh toán (Sổ quỹ / Chuyển khoản)");
                if (p.TotalDiscounts > 0)
                {
                    AddFinancialMetricRow(ws, ref row, "4. Chiết khấu & Giảm trừ trong kỳ (-):", p.TotalDiscounts, "Chiết khấu thanh toán / Điều chỉnh giảm");
                }

                // Closing Balance Row
                ws.Cell(row, 1).Value = "5. SỐ DƯ NỢ CUỐI KỲ:";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 11;
                ws.Cell(row, 3).Value = (double)p.ClosingBalance;
                ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0 ₫";
                ws.Cell(row, 3).Style.Font.Bold = true;
                ws.Cell(row, 3).Style.Font.FontSize = 12;
                ws.Cell(row, 3).Style.Font.FontColor = p.ClosingBalance > 0 ? DangerRed : SuccessGreen;
                ws.Cell(row, 4).Value = p.BalanceDirectionDescription;
                ws.Cell(row, 4).Style.Font.Italic = true;
                ws.Cell(row, 4).Style.Font.FontColor = MutedText;

                ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = TotalRowBg;
                ws.Range(row, 1, row, 7).Style.Border.TopBorder = XLBorderStyleValues.Thin;
                ws.Range(row, 1, row, 7).Style.Border.BottomBorder = XLBorderStyleValues.Double;
                row += 2;

                // Overdue status breakdown
                if (p.ClosingBalance > 0)
                {
                    AddKeyValueRow(ws, ref row, "Nợ trong hạn (chưa đến hạn):", p.NotYetDueBalance.ToString("N0") + " ₫", "Hạn thanh toán gần nhất:", p.NextDueDate?.ToString("dd/MM/yyyy") ?? "Theo thỏa thuận");
                    AddKeyValueRow(ws, ref row, "Nợ quá hạn:", p.OverdueBalance.ToString("N0") + " ₫", "Trạng thái công nợ:", p.DebtStatusDescription);
                    row++;
                }
            }
        }
        else
        {
            // Executive KPI Block (Multi-partner or Consolidated)
            ws.Range(row, 1, row, 8).Merge();
            ws.Cell(row, 1).Value = "CHỈ SỐ CÔNG NỢ TRỌNG YẾU TOÀN DOANH NGHIỆP (EXECUTIVE SUMMARY)";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = SectionHeaderBg;
            ws.Cell(row, 1).Style.Font.FontColor = BrandNavy;
            row++;

            decimal totalCustomerRec = dataset.Partners.Where(p => p.PartnerType == PartnerType.Customer).Sum(p => p.ClosingBalance);
            decimal totalSupplierPay = dataset.Partners.Where(p => p.PartnerType == PartnerType.Supplier).Sum(p => p.ClosingBalance);
            decimal netDiff = totalCustomerRec - totalSupplierPay;

            int custWithDebt = dataset.Partners.Count(p => p.PartnerType == PartnerType.Customer && p.ClosingBalance > 0);
            int supWithDebt = dataset.Partners.Count(p => p.PartnerType == PartnerType.Supplier && p.ClosingBalance > 0);

            AddFinancialMetricRow(ws, ref row, "1. Tổng công nợ phải thu từ khách hàng:", totalCustomerRec, $"{custWithDebt} khách hàng hiện có số dư nợ");
            AddFinancialMetricRow(ws, ref row, "2. Tổng công nợ phải trả cho nhà cung cấp:", totalSupplierPay, $"{supWithDebt} nhà cung cấp có nghĩa vụ nợ");
            AddFinancialMetricRow(ws, ref row, "3. Chênh lệch công nợ (Đối soát tham chiếu):", netDiff, "Chỉ tiêu phân tích tổng hợp (Phải thu - Phải trả), không cấn trừ giữa các pháp nhân");
            row++;
        }

        // Render Table: CUSTOMER RECEIVABLES
        var customers = dataset.Partners.Where(p => p.PartnerType == PartnerType.Customer).ToList();
        if (customers.Any())
        {
            ws.Cell(row, 1).Value = "I. BẢNG TỔNG HỢP CÔNG NỢ PHẢI THU KHÁCH HÀNG (ACCOUNTS RECEIVABLE)";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 11;
            ws.Cell(row, 1).Style.Font.FontColor = BrandIndigo;
            row++;

            row = RenderPartnerSummaryTable(ws, row, customers, DebtDirection.Receivable);
            row += 2;
        }

        // Render Table: SUPPLIER PAYABLES
        var suppliers = dataset.Partners.Where(p => p.PartnerType == PartnerType.Supplier).ToList();
        if (suppliers.Any())
        {
            ws.Cell(row, 1).Value = "II. BẢNG TỔNG HỢP CÔNG NỢ PHẢI TRẢ NHÀ CUNG CẤP (ACCOUNTS PAYABLE)";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Font.FontSize = 11;
            ws.Cell(row, 1).Style.Font.FontColor = BrandIndigo;
            row++;

            row = RenderPartnerSummaryTable(ws, row, suppliers, DebtDirection.Payable);
            row += 2;
        }

        // Signature Section
        RenderSignatureSection(ws, row);
        ws.Columns(1, 16).AdjustToContents(8.0, 45.0);
    }

    private int RenderPartnerSummaryTable(
        IXLWorksheet ws,
        int startRow,
        List<PartnerStatementSummaryDto> partners,
        DebtDirection direction)
    {
        int row = startRow;
        int tableHeaderRow = row;

        string[] headers = new[]
        {
            "STT", "Chiều công nợ", "Mã đối tác", "Tên đối tác", "Tên công ty / Pháp nhân",
            "Mã số thuế", "Số điện thoại", "Dư nợ đầu kỳ", "Phát sinh tăng trong kỳ", "Đã thanh toán / Thu chi",
            "Chiết khấu & Điều chỉnh", "Dư nợ cuối kỳ", "Nợ quá hạn", "Nợ chưa đến hạn", "Hạn TT gần nhất", "Trạng thái công nợ"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(tableHeaderRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TableHeaderBg;
            cell.Style.Alignment.Horizontal = (i == 0 || i == 1 || i == 2 || i == 5 || i == 6 || i == 14 || i == 15)
                ? XLAlignmentHorizontalValues.Center
                : (i >= 7 && i <= 13)
                    ? XLAlignmentHorizontalValues.Right
                    : XLAlignmentHorizontalValues.Left;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = BorderGray;
        }
        row++;

        int stt = 1;
        int firstDataRow = row;

        foreach (var p in partners)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 2).Value = direction == DebtDirection.Receivable ? "Khách hàng nợ (Phải thu)" : "Nợ NCC (Phải trả)";
            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 3).Value = p.Code;
            ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 4).Value = p.Name;
            ws.Cell(row, 5).Value = p.CompanyName ?? "-";
            ws.Cell(row, 6).Value = p.TaxCode ?? "-";
            ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 7).Value = p.Phone ?? "-";
            ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Numeric values
            SetNumericCell(ws.Cell(row, 8), (double)p.OpeningBalance);
            SetNumericCell(ws.Cell(row, 9), (double)p.TotalDebits);
            SetNumericCell(ws.Cell(row, 10), (double)p.TotalCredits);
            SetNumericCell(ws.Cell(row, 11), (double)p.TotalDiscounts);

            var closeCell = ws.Cell(row, 12);
            SetNumericCell(closeCell, (double)p.ClosingBalance);
            closeCell.Style.Font.Bold = true;
            if (p.ClosingBalance > 0)
            {
                closeCell.Style.Font.FontColor = direction == DebtDirection.Receivable ? DangerRed : WarningAmber;
            }

            SetNumericCell(ws.Cell(row, 13), (double)p.OverdueBalance);
            if (p.OverdueBalance > 0) ws.Cell(row, 13).Style.Font.FontColor = DangerRed;

            SetNumericCell(ws.Cell(row, 14), (double)p.NotYetDueBalance);

            ws.Cell(row, 15).Value = p.NextDueDate.HasValue ? p.NextDueDate.Value.ToString("dd/MM/yyyy") : "-";
            ws.Cell(row, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 16).Value = p.DebtStatusDescription;
            ws.Cell(row, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            for (int col = 1; col <= 16; col++)
            {
                ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(row, col).Style.Border.OutsideBorderColor = BorderLight;
            }
            row++;
        }

        // Summary Total Row
        int totalRow = row;
        ws.Cell(totalRow, 1).Value = "TỔNG CỘNG";
        ws.Cell(totalRow, 1).Style.Font.Bold = true;
        ws.Cell(totalRow, 4).Value = $"Tổng cộng {partners.Count} đối tác ({ (direction == DebtDirection.Receivable ? "Phải thu" : "Phải trả") })";
        ws.Cell(totalRow, 4).Style.Font.Bold = true;

        for (int col = 8; col <= 14; col++)
        {
            var cell = ws.Cell(totalRow, col);
            string colLetter = XLHelper.GetColumnLetterFromNumber(col);
            cell.FormulaA1 = $"SUM({colLetter}{firstDataRow}:{colLetter}{totalRow - 1})";
            cell.Style.NumberFormat.Format = "#,##0 ₫";
            cell.Style.Font.Bold = true;
        }

        ws.Cell(totalRow, 12).Style.Font.FontColor = DangerRed;

        ws.Range(totalRow, 1, totalRow, 16).Style.Fill.BackgroundColor = TotalRowBg;
        ws.Range(totalRow, 1, totalRow, 16).Style.Border.TopBorder = XLBorderStyleValues.Thin;
        ws.Range(totalRow, 1, totalRow, 16).Style.Border.BottomBorder = XLBorderStyleValues.Double;

        ws.Range(tableHeaderRow, 1, totalRow, 16).SetAutoFilter();
        return totalRow;
    }

    // =========================================================================
    // SHEET B BUILDER — CHI TIẾT CÔNG NỢ THEO CHỨNG TỪ
    // =========================================================================

    private void BuildSheetB_Documents(
        IXLWorksheet ws,
        DebtExportDataset dataset,
        CompanySettings company,
        string? datePresetTitle,
        bool isSinglePartner)
    {
        ws.ShowGridLines = true;
        SetupPageLayout(ws);

        int row = 1;
        row = RenderCompanyHeader(ws, row, company);

        // Title Block
        ws.Cell(row, 1).Value = "BÁO CÁO CÔNG NỢ — SHEET B: CHI TIẾT CÔNG NỢ THEO CHỨNG TỪ";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 1).Style.Font.FontColor = BrandIndigo;
        row++;

        string periodText = !string.IsNullOrEmpty(datePresetTitle)
            ? $"Kỳ đối soát: {datePresetTitle}"
            : "Kỳ đối soát: Toàn thời gian";
        ws.Cell(row, 1).Value = $"{periodText}  |  Tổng số chứng từ: {dataset.Documents.Count}  |  Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;
        row += 2;

        int tableHeaderRow = row;
        string[] headers = new[]
        {
            "STT", "Chiều công nợ", "Mã đối tác", "Tên đối tác", "Loại chứng từ",
            "Số chứng từ", "Chứng từ gốc / Mã ĐH", "Ngày chứng từ", "Hạn thanh toán",
            "Giá trị chứng từ", "Đã thanh toán / Đối trừ", "Chiết khấu / Giảm trừ", "Dư nợ còn lại",
            "Trạng thái thanh toán", "Ghi chú", "Mã định danh truy vết (ID)"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(tableHeaderRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TableHeaderBg;
            cell.Style.Alignment.Horizontal = (i == 0 || i == 1 || i == 2 || i == 5 || i == 6 || i == 7 || i == 8 || i == 13 || i == 15)
                ? XLAlignmentHorizontalValues.Center
                : (i >= 9 && i <= 12)
                    ? XLAlignmentHorizontalValues.Right
                    : XLAlignmentHorizontalValues.Left;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = BorderGray;
        }
        row++;

        int firstDataRow = row;
        int stt = 1;

        foreach (var doc in dataset.Documents)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 2).Value = doc.DirectionDescription;
            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 3).Value = doc.PartnerCode;
            ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 4).Value = doc.PartnerName;
            ws.Cell(row, 5).Value = doc.DocumentType;

            ws.Cell(row, 6).Value = doc.DocumentCode;
            ws.Cell(row, 6).Style.Font.Bold = true;
            ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 7).Value = !string.IsNullOrEmpty(doc.ReferenceCode) ? doc.ReferenceCode : "-";
            ws.Cell(row, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 8).Value = doc.DocumentDate.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 9).Value = doc.DueDate.HasValue ? doc.DueDate.Value.ToString("dd/MM/yyyy") : "-";
            ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            SetNumericCell(ws.Cell(row, 10), (double)doc.OriginalAmount);
            SetNumericCell(ws.Cell(row, 11), (double)doc.PaidAmount);
            if (doc.PaidAmount > 0) ws.Cell(row, 11).Style.Font.FontColor = SuccessGreen;

            SetNumericCell(ws.Cell(row, 12), (double)doc.DiscountAmount);

            var outCell = ws.Cell(row, 13);
            SetNumericCell(outCell, (double)doc.OutstandingAmount);
            outCell.Style.Font.Bold = true;
            if (doc.OutstandingAmount > 0) outCell.Style.Font.FontColor = DangerRed;

            ws.Cell(row, 14).Value = doc.StatusDescription;
            ws.Cell(row, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 15).Value = doc.Notes ?? "-";

            ws.Cell(row, 16).Value = doc.TraceId;
            ws.Cell(row, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 16).Style.Font.FontColor = MutedText;

            for (int col = 1; col <= 16; col++)
            {
                ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(row, col).Style.Border.OutsideBorderColor = BorderLight;
            }
            row++;
        }

        // Summary Total Row
        if (dataset.Documents.Any())
        {
            int totalRow = row;
            ws.Cell(totalRow, 1).Value = "TỔNG CỘNG";
            ws.Cell(totalRow, 1).Style.Font.Bold = true;
            ws.Cell(totalRow, 4).Value = $"Tổng cộng {dataset.Documents.Count} chứng từ";
            ws.Cell(totalRow, 4).Style.Font.Bold = true;

            for (int col = 10; col <= 13; col++)
            {
                var cell = ws.Cell(totalRow, col);
                string colLetter = XLHelper.GetColumnLetterFromNumber(col);
                cell.FormulaA1 = $"SUM({colLetter}{firstDataRow}:{colLetter}{totalRow - 1})";
                cell.Style.NumberFormat.Format = "#,##0 ₫";
                cell.Style.Font.Bold = true;
            }

            ws.Cell(totalRow, 11).Style.Font.FontColor = SuccessGreen;
            ws.Cell(totalRow, 13).Style.Font.FontColor = DangerRed;

            ws.Range(totalRow, 1, totalRow, 16).Style.Fill.BackgroundColor = TotalRowBg;
            ws.Range(totalRow, 1, totalRow, 16).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            ws.Range(totalRow, 1, totalRow, 16).Style.Border.BottomBorder = XLBorderStyleValues.Double;

            ws.Range(tableHeaderRow, 1, totalRow, 16).SetAutoFilter();
        }

        ws.SheetView.FreezeRows(tableHeaderRow);
        ws.Columns(1, 16).AdjustToContents(8.0, 45.0);
    }

    // =========================================================================
    // SHEET C BUILDER — CHI TIẾT TỪNG MẶT HÀNG / DỊCH VỤ
    // =========================================================================

    private void BuildSheetC_LineItems(
        IXLWorksheet ws,
        DebtExportDataset dataset,
        CompanySettings company,
        string? datePresetTitle,
        PartnerDebtStatementExportRequest request,
        bool isSinglePartner)
    {
        ws.ShowGridLines = true;
        SetupPageLayout(ws);

        int row = 1;
        row = RenderCompanyHeader(ws, row, company);

        // Title Block
        ws.Cell(row, 1).Value = "BÁO CÁO CÔNG NỢ — SHEET C: CHI TIẾT TỪNG MẶT HÀNG / DỊCH VỤ";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 1).Style.Font.FontColor = BrandIndigo;
        row++;

        string periodText = !string.IsNullOrEmpty(datePresetTitle)
            ? $"Kỳ đối soát: {datePresetTitle}"
            : "Kỳ đối soát: Toàn thời gian";
        ws.Cell(row, 1).Value = $"{periodText}  |  Tổng số dòng mặt hàng: {dataset.LineItems.Count}  |  Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;
        row += 2;

        // CRITICAL ACCOUNTING ALLOCATION NOTICE BANNER
        ws.Range(row, 1, row, 17).Merge();
        ws.Cell(row, 1).Value = "LƯU Ý KẾ TOÁN QUAN TRỌNG: Hệ thống TradeFlow quản lý thanh toán và theo dõi dư nợ theo cấp độ Hóa đơn / Chứng từ (Document-level settlement). Các khoản thanh toán và dư nợ còn lại không phân bổ riêng lẻ cho từng dòng sản phẩm nhằm bảo toàn tính chính xác và nguyên vẹn của dữ liệu kế toán thực tế.";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = XLColor.FromHtml("#92400E"); // Dark amber
        ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7"); // Light amber
        ws.Range(row, 1, row, 17).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        ws.Range(row, 1, row, 17).Style.Border.OutsideBorderColor = XLColor.FromHtml("#F59E0B");
        row += 2;

        int tableHeaderRow = row;
        string[] headers = new[]
        {
            "STT", "Số chứng từ", "Ngày chứng từ", "Chiều công nợ", "Mã đối tác",
            "Tên đối tác", "Mã mặt hàng", "Tên sản phẩm / dịch vụ", "Quy cách / Mô tả", "Đơn vị tính",
            "Số lượng", "Đơn giá", "Chiết khấu dòng", "Tiền thuế VAT", "Thành tiền",
            "Đã TT (Cấp HĐ)", "Còn nợ (Cấp HĐ)", "Ghi chú phân bổ"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(tableHeaderRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TableHeaderBg;
            cell.Style.Alignment.Horizontal = (i == 0 || i == 1 || i == 2 || i == 3 || i == 4 || i == 9 || i == 17)
                ? XLAlignmentHorizontalValues.Center
                : (i >= 10 && i <= 16)
                    ? XLAlignmentHorizontalValues.Right
                    : XLAlignmentHorizontalValues.Left;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = BorderGray;
        }
        row++;

        int firstDataRow = row;
        int stt = 1;

        foreach (var item in dataset.LineItems)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 2).Value = item.DocumentCode;
            ws.Cell(row, 2).Style.Font.Bold = true;
            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 3).Value = item.DocumentDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 4).Value = item.DirectionDescription;
            ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 5).Value = item.PartnerCode;
            ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 6).Value = item.PartnerName;

            ws.Cell(row, 7).Value = item.ProductCode;
            ws.Cell(row, 7).Style.Font.Bold = true;

            ws.Cell(row, 8).Value = item.ProductName;
            ws.Cell(row, 9).Value = item.Specifications ?? "-";

            ws.Cell(row, 10).Value = item.UnitName;
            ws.Cell(row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Numeric values
            var qtyCell = ws.Cell(row, 11);
            qtyCell.Value = (double)item.Quantity;
            qtyCell.Style.NumberFormat.Format = "#,##0.##";
            qtyCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            SetNumericCell(ws.Cell(row, 12), (double)item.UnitPrice);
            SetNumericCell(ws.Cell(row, 13), (double)item.DiscountAmount);
            SetNumericCell(ws.Cell(row, 14), (double)item.TaxAmount);

            var totalCell = ws.Cell(row, 15);
            SetNumericCell(totalCell, (double)item.LineTotal);
            totalCell.Style.Font.Bold = true;

            // Invoice-level settlement columns
            SetNumericCell(ws.Cell(row, 16), (double)item.DocumentPaidAmount);
            if (item.DocumentPaidAmount > 0) ws.Cell(row, 16).Style.Font.FontColor = SuccessGreen;

            var docOutCell = ws.Cell(row, 17);
            SetNumericCell(docOutCell, (double)item.DocumentOutstandingAmount);
            if (item.DocumentOutstandingAmount > 0) docOutCell.Style.Font.FontColor = DangerRed;

            ws.Cell(row, 18).Value = item.AllocationNote;
            ws.Cell(row, 18).Style.Font.Italic = true;
            ws.Cell(row, 18).Style.Font.FontSize = 8;
            ws.Cell(row, 18).Style.Font.FontColor = MutedText;

            for (int col = 1; col <= 18; col++)
            {
                ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(row, col).Style.Border.OutsideBorderColor = BorderLight;
            }
            row++;
        }

        // Summary Total Row
        if (dataset.LineItems.Any())
        {
            int totalRow = row;
            ws.Cell(totalRow, 1).Value = "TỔNG CỘNG";
            ws.Cell(totalRow, 1).Style.Font.Bold = true;
            ws.Cell(totalRow, 8).Value = $"Tổng cộng {dataset.LineItems.Count} dòng sản phẩm/dịch vụ";
            ws.Cell(totalRow, 8).Style.Font.Bold = true;

            // Quantity Sum
            var qtyTotalCell = ws.Cell(totalRow, 11);
            qtyTotalCell.FormulaA1 = $"SUM(K{firstDataRow}:K{totalRow - 1})";
            qtyTotalCell.Style.NumberFormat.Format = "#,##0.##";
            qtyTotalCell.Style.Font.Bold = true;

            // Discount, Tax, LineTotal Sums
            for (int col = 13; col <= 15; col++)
            {
                var cell = ws.Cell(totalRow, col);
                string colLetter = XLHelper.GetColumnLetterFromNumber(col);
                cell.FormulaA1 = $"SUM({colLetter}{firstDataRow}:{colLetter}{totalRow - 1})";
                cell.Style.NumberFormat.Format = "#,##0 ₫";
                cell.Style.Font.Bold = true;
            }

            ws.Cell(totalRow, 16).Value = "-";
            ws.Cell(totalRow, 16).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(totalRow, 17).Value = "-";
            ws.Cell(totalRow, 17).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Range(totalRow, 1, totalRow, 18).Style.Fill.BackgroundColor = TotalRowBg;
            ws.Range(totalRow, 1, totalRow, 18).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            ws.Range(totalRow, 1, totalRow, 18).Style.Border.BottomBorder = XLBorderStyleValues.Double;

            ws.Range(tableHeaderRow, 1, totalRow, 18).SetAutoFilter();
        }

        ws.SheetView.FreezeRows(tableHeaderRow);
        ws.Columns(1, 18).AdjustToContents(8.0, 45.0);
    }

    // =========================================================================
    // SHEET D BUILDER — LỊCH SỬ THU, CHI VÀ ĐIỀU CHỈNH CÔNG NỢ
    // =========================================================================

    private void BuildSheetD_Transactions(
        IXLWorksheet ws,
        DebtExportDataset dataset,
        CompanySettings company,
        string? datePresetTitle,
        bool isSinglePartner)
    {
        ws.ShowGridLines = true;
        SetupPageLayout(ws);

        int row = 1;
        row = RenderCompanyHeader(ws, row, company);

        // Title Block
        ws.Cell(row, 1).Value = "BÁO CÁO CÔNG NỢ — SHEET D: LỊCH SỬ THU, CHI VÀ ĐIỀU CHỈNH CÔNG NỢ";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        ws.Cell(row, 1).Style.Font.FontColor = BrandIndigo;
        row++;

        string periodText = !string.IsNullOrEmpty(datePresetTitle)
            ? $"Kỳ đối soát: {datePresetTitle}"
            : "Kỳ đối soát: Toàn thời gian";
        ws.Cell(row, 1).Value = $"{periodText}  |  Tổng số nghiệp vụ tài chính: {dataset.Transactions.Count}  |  Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(row, 1).Style.Font.Italic = true;
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;
        row += 2;

        int tableHeaderRow = row;
        string[] headers = new[]
        {
            "STT", "Loại nghiệp vụ", "Số phiếu", "Ngày giao dịch", "Chiều công nợ",
            "Mã đối tác", "Tên đối tác", "Chứng từ liên quan (HĐ/ĐH)", "Số tiền giao dịch", "Hình thức thanh toán",
            "Loại chiết khấu / Điều chỉnh", "Ghi chú diễn giải", "Người lập phiếu", "Thời điểm tạo", "Mã tham chiếu đối ứng"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(tableHeaderRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = TableHeaderBg;
            cell.Style.Alignment.Horizontal = (i == 0 || i == 1 || i == 2 || i == 3 || i == 4 || i == 5 || i == 7 || i == 9 || i == 12 || i == 13 || i == 14)
                ? XLAlignmentHorizontalValues.Center
                : (i == 8)
                    ? XLAlignmentHorizontalValues.Right
                    : XLAlignmentHorizontalValues.Left;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = BorderGray;
        }
        row++;

        int firstDataRow = row;
        int stt = 1;

        foreach (var t in dataset.Transactions)
        {
            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 2).Value = t.TransactionTypeDescription;
            ws.Cell(row, 2).Style.Font.Bold = true;
            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            if (t.TransactionType == DebtTransactionType.Payment)
            {
                ws.Cell(row, 2).Style.Font.FontColor = SuccessGreen;
            }
            else if (t.TransactionType == DebtTransactionType.PaymentDiscount)
            {
                ws.Cell(row, 2).Style.Font.FontColor = BrandIndigo;
            }

            ws.Cell(row, 3).Value = t.PaymentCode;
            ws.Cell(row, 3).Style.Font.Bold = true;
            ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 4).Value = t.TransactionDate.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 5).Value = t.DirectionDescription;
            ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 6).Value = t.PartnerCode;
            ws.Cell(row, 6).Style.Font.Bold = true;
            ws.Cell(row, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 7).Value = t.PartnerName;

            ws.Cell(row, 8).Value = !string.IsNullOrEmpty(t.ReferenceCode) ? t.ReferenceCode : "-";
            ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            var amountCell = ws.Cell(row, 9);
            SetNumericCell(amountCell, (double)t.Amount);
            amountCell.Style.Font.Bold = true;
            if (t.TransactionType == DebtTransactionType.Payment)
            {
                amountCell.Style.Font.FontColor = SuccessGreen;
            }

            ws.Cell(row, 10).Value = t.PaymentMethod;
            ws.Cell(row, 10).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 11).Value = t.DiscountOrAdjustmentType ?? "-";
            ws.Cell(row, 12).Value = t.Notes ?? "-";

            ws.Cell(row, 13).Value = t.CreatedBy ?? "Hệ thống";
            ws.Cell(row, 13).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 14).Value = t.CreatedAt.ToString("dd/MM/yyyy HH:mm");
            ws.Cell(row, 14).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(row, 15).Value = t.CounterReference ?? "-";
            ws.Cell(row, 15).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            for (int col = 1; col <= 15; col++)
            {
                ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(row, col).Style.Border.OutsideBorderColor = BorderLight;
            }
            row++;
        }

        // Summary Total Row
        if (dataset.Transactions.Any())
        {
            int totalRow = row;
            ws.Cell(totalRow, 1).Value = "TỔNG CỘNG";
            ws.Cell(totalRow, 1).Style.Font.Bold = true;
            ws.Cell(totalRow, 7).Value = $"Tổng cộng {dataset.Transactions.Count} giao dịch thanh toán / điều chỉnh";
            ws.Cell(totalRow, 7).Style.Font.Bold = true;

            var sumCell = ws.Cell(totalRow, 9);
            string colLetter = XLHelper.GetColumnLetterFromNumber(9);
            sumCell.FormulaA1 = $"SUM({colLetter}{firstDataRow}:{colLetter}{totalRow - 1})";
            sumCell.Style.NumberFormat.Format = "#,##0 ₫";
            sumCell.Style.Font.Bold = true;
            sumCell.Style.Font.FontColor = SuccessGreen;

            ws.Range(totalRow, 1, totalRow, 15).Style.Fill.BackgroundColor = TotalRowBg;
            ws.Range(totalRow, 1, totalRow, 15).Style.Border.TopBorder = XLBorderStyleValues.Thin;
            ws.Range(totalRow, 1, totalRow, 15).Style.Border.BottomBorder = XLBorderStyleValues.Double;

            ws.Range(tableHeaderRow, 1, totalRow, 15).SetAutoFilter();
        }

        ws.SheetView.FreezeRows(tableHeaderRow);
        ws.Columns(1, 15).AdjustToContents(8.0, 45.0);
    }

    // =========================================================================
    // DATASET LOADER & DOMAIN RECONCILIATION ENGINE
    // =========================================================================

    private async Task<DebtExportDataset> LoadExportDatasetAsync(
        DebtExportScope scope,
        PartnerType? partnerType,
        List<int>? partnerIds,
        DebtFilterDto? filter,
        DateTime? fromDate,
        DateTime? toDate,
        string? datePresetTitle,
        CancellationToken cancellationToken)
    {
        var targetCustIds = new HashSet<int>();
        var targetSupIds = new HashSet<int>();

        bool loadCustomers = !partnerType.HasValue || partnerType.Value == PartnerType.Customer;
        bool loadSuppliers = !partnerType.HasValue || partnerType.Value == PartnerType.Supplier;

        if (scope == DebtExportScope.CurrentPartner || scope == DebtExportScope.SelectedPartners)
        {
            if (partnerIds != null && partnerIds.Any())
            {
                if (partnerType == PartnerType.Customer)
                {
                    targetCustIds = partnerIds.ToHashSet();
                }
                else if (partnerType == PartnerType.Supplier)
                {
                    targetSupIds = partnerIds.ToHashSet();
                }
                else
                {
                    // Detect customer vs supplier IDs from DB
                    var foundCusts = await _context.Customers.AsNoTracking().Where(c => partnerIds.Contains(c.Id)).Select(c => c.Id).ToListAsync(cancellationToken);
                    var foundSups = await _context.Suppliers.AsNoTracking().Where(s => partnerIds.Contains(s.Id)).Select(s => s.Id).ToListAsync(cancellationToken);
                    targetCustIds = foundCusts.ToHashSet();
                    targetSupIds = foundSups.ToHashSet();
                }
            }
        }
        else
        {
            // All filtered or all partners in system
            if (loadCustomers)
            {
                var custQuery = _context.Customers.AsNoTracking();
                if (filter != null && !string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    string term = filter.SearchTerm.Trim().ToLower();
                    custQuery = custQuery.Where(c => c.Code.ToLower().Contains(term)
                                                  || c.Name.ToLower().Contains(term)
                                                  || (c.CompanyName != null && c.CompanyName.ToLower().Contains(term))
                                                  || (c.Phone != null && c.Phone.Contains(term))
                                                  || (c.TaxCode != null && c.TaxCode.Contains(term)));
                }
                var custIds = await custQuery.Select(c => c.Id).ToListAsync(cancellationToken);
                targetCustIds = custIds.ToHashSet();
            }

            if (loadSuppliers)
            {
                var supQuery = _context.Suppliers.AsNoTracking();
                if (filter != null && !string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    string term = filter.SearchTerm.Trim().ToLower();
                    supQuery = supQuery.Where(s => s.Code.ToLower().Contains(term)
                                                || s.Name.ToLower().Contains(term)
                                                || (s.CompanyName != null && s.CompanyName.ToLower().Contains(term))
                                                || (s.Phone != null && s.Phone.Contains(term))
                                                || (s.TaxCode != null && s.TaxCode.Contains(term)));
                }
                var supIds = await supQuery.Select(s => s.Id).ToListAsync(cancellationToken);
                targetSupIds = supIds.ToHashSet();
            }
        }

        DateTime? fromDateUtc = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc) : null;
        DateTime? toDateUtc = toDate.HasValue ? DateTime.SpecifyKind(toDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc) : null;

        var dataset = new DebtExportDataset();

        // ---------------------------------------------------------------------
        // 1. PROCESS CUSTOMERS (Công nợ phải thu)
        // ---------------------------------------------------------------------
        if (targetCustIds.Any())
        {
            var customers = await _context.Customers.AsNoTracking()
                .Where(c => targetCustIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

            var invoices = await _context.Invoices.AsNoTracking()
                .Include(i => i.Items)
                .Where(i => i.CustomerId.HasValue && targetCustIds.Contains(i.CustomerId.Value) && i.Status != InvoiceStatus.Cancelled)
                .OrderBy(i => i.InvoiceDate)
                .ToListAsync(cancellationToken);

            var invoicedOrderIds = invoices.Where(i => i.SalesOrderId.HasValue).Select(i => i.SalesOrderId!.Value).ToHashSet();

            var salesOrders = await _context.SalesOrders.AsNoTracking()
                .Include(o => o.Items)
                .Where(o => targetCustIds.Contains(o.CustomerId) && o.Status == SalesOrderStatus.Confirmed && !invoicedOrderIds.Contains(o.Id))
                .OrderBy(o => o.OrderDate)
                .ToListAsync(cancellationToken);

            var custPayments = await _context.PartnerPayments.AsNoTracking()
                .Where(p => p.PartnerType == (int)PartnerType.Customer && targetCustIds.Contains(p.PartnerId))
                .OrderBy(p => p.PaymentDate)
                .ToListAsync(cancellationToken);

            // Group payments by invoice (strictly payments, excluding discounts)
            var paymentsByInvoice = custPayments.Where(p => p.InvoiceId.HasValue && p.TransactionType == DebtTransactionType.Payment)
                .GroupBy(p => p.InvoiceId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var discountsByInvoice = custPayments.Where(p => p.InvoiceId.HasValue && p.TransactionType == DebtTransactionType.PaymentDiscount)
                .GroupBy(p => p.InvoiceId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            // General unallocated payments per customer
            var generalPaymentsByCust = custPayments.Where(p => !p.InvoiceId.HasValue && p.TransactionType == DebtTransactionType.Payment)
                .GroupBy(p => p.PartnerId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            foreach (var cust in customers)
            {
                var custInvs = invoices.Where(i => i.CustomerId == cust.Id).ToList();
                var custOrds = salesOrders.Where(o => o.CustomerId == cust.Id).ToList();
                var custPays = custPayments.Where(p => p.PartnerId == cust.Id).ToList();

                // Compute Opening Balance (prior to fromDateUtc)
                decimal openingDebits = 0m;
                decimal openingCredits = 0m;
                decimal openingDiscounts = 0m;

                if (fromDateUtc.HasValue)
                {
                    openingDebits += custInvs.Where(i => i.InvoiceDate < fromDateUtc.Value).Sum(i => i.GrandTotal);
                    openingDebits += custOrds.Where(o => o.OrderDate < fromDateUtc.Value).Sum(o => o.GrandTotal);

                    openingCredits += custPays.Where(p => p.PaymentDate < fromDateUtc.Value && p.TransactionType == DebtTransactionType.Payment).Sum(p => p.Amount);
                    openingDiscounts += custPays.Where(p => p.PaymentDate < fromDateUtc.Value && p.TransactionType == DebtTransactionType.PaymentDiscount).Sum(p => p.Amount);
                }

                decimal openingBalance = Math.Max(0m, openingDebits - openingCredits - openingDiscounts);

                // In-period documents & general payment allocation
                decimal runningGeneralCredit = generalPaymentsByCust.GetValueOrDefault(cust.Id, 0m);

                var periodInvs = custInvs.Where(i => (!fromDateUtc.HasValue || i.InvoiceDate >= fromDateUtc.Value) &&
                                                     (!toDateUtc.HasValue || i.InvoiceDate <= toDateUtc.Value)).ToList();

                var periodOrds = custOrds.Where(o => (!fromDateUtc.HasValue || o.OrderDate >= fromDateUtc.Value) &&
                                                     (!toDateUtc.HasValue || o.OrderDate <= toDateUtc.Value)).ToList();

                var periodPays = custPays.Where(p => (!fromDateUtc.HasValue || p.PaymentDate >= fromDateUtc.Value) &&
                                                     (!toDateUtc.HasValue || p.PaymentDate <= toDateUtc.Value)).ToList();

                decimal totalDebits = periodInvs.Sum(i => i.GrandTotal) + periodOrds.Sum(o => o.GrandTotal);
                decimal totalCredits = periodPays.Where(p => p.TransactionType == DebtTransactionType.Payment).Sum(p => p.Amount);
                decimal totalDiscounts = periodPays.Where(p => p.TransactionType == DebtTransactionType.PaymentDiscount).Sum(p => p.Amount);

                decimal closingBalance = Math.Max(0m, openingBalance + totalDebits - totalCredits - totalDiscounts);

                // Calculate Overdue vs Not-Yet-Due
                decimal overdue = 0m;
                decimal notYetDue = 0m;
                DateTime? nextDueDate = null;

                foreach (var inv in periodInvs)
                {
                    bool isMarkedCredit = inv.PaymentMethod != null &&
                                         (inv.PaymentMethod.Contains("Công nợ") ||
                                          inv.PaymentMethod.Contains("Ghi nợ") ||
                                          inv.PaymentMethod.Contains("Chưa thanh toán") ||
                                          inv.Status == InvoiceStatus.Pending);

                    decimal specificPaid = paymentsByInvoice.GetValueOrDefault(inv.Id, 0m);
                    decimal specificDiscount = discountsByInvoice.GetValueOrDefault(inv.Id, 0m);

                    decimal paid = !isMarkedCredit ? inv.GrandTotal : specificPaid;

                    // Allocate general payment if outstanding
                    decimal outstanding = Math.Max(0m, inv.GrandTotal - paid - specificDiscount);
                    if (isMarkedCredit && outstanding > 0 && runningGeneralCredit > 0)
                    {
                        decimal alloc = Math.Min(outstanding, runningGeneralCredit);
                        paid += alloc;
                        outstanding -= alloc;
                        runningGeneralCredit -= alloc;
                    }

                    // Default payment term: 30 days
                    DateTime dueDate = inv.InvoiceDate.AddDays(30);
                    if (outstanding > 0)
                    {
                        if (DateTime.UtcNow > dueDate) overdue += outstanding;
                        else notYetDue += outstanding;

                        if (!nextDueDate.HasValue || dueDate < nextDueDate.Value)
                        {
                            nextDueDate = dueDate;
                        }
                    }

                    var docStatus = outstanding == 0m ? DebtStatus.Settled
                        : (paid > 0m ? DebtStatus.PartiallyPaid : DebtStatus.Outstanding);

                    string docType = inv.Type == InvoiceType.VatInvoice ? "Hóa đơn VAT" : "Hóa đơn bán hàng";
                    string docCode = !string.IsNullOrEmpty(inv.InvoiceNo) ? inv.InvoiceNo : inv.InvoiceNumber;

                    dataset.Documents.Add(new DebtDocumentExportRowDto
                    {
                        Direction = DebtDirection.Receivable,
                        PartnerCode = cust.Code,
                        PartnerName = cust.Name,
                        DocumentType = docType,
                        DocumentCode = docCode,
                        ReferenceCode = inv.SalesOrderId.HasValue ? $"DH-{inv.SalesOrderId}" : null,
                        DocumentDate = inv.InvoiceDate,
                        DueDate = dueDate,
                        OriginalAmount = inv.GrandTotal,
                        PaidAmount = paid,
                        DiscountAmount = specificDiscount,
                        OutstandingAmount = outstanding,
                        DebtStatus = docStatus,
                        Notes = inv.Notes,
                        TraceId = $"INV-{inv.Id}"
                    });

                    // Line Items for Sheet C
                    foreach (var it in inv.Items.OrderBy(x => x.SortOrder))
                    {
                        dataset.LineItems.Add(new DebtLineItemExportRowDto
                        {
                            DocumentCode = docCode,
                            DocumentDate = inv.InvoiceDate,
                            Direction = DebtDirection.Receivable,
                            PartnerCode = cust.Code,
                            PartnerName = cust.Name,
                            ProductCode = it.ProductCode,
                            ProductName = it.ProductName,
                            Specifications = null,
                            UnitName = it.UnitName,
                            Quantity = it.Quantity,
                            UnitPrice = it.UnitPrice,
                            DiscountAmount = it.DiscountAmount,
                            TaxRate = it.TaxRate,
                            TaxAmount = it.TaxAmount,
                            LineTotal = it.LineTotal,
                            DocumentPaidAmount = paid,
                            DocumentOutstandingAmount = outstanding,
                            AllocationNote = "Phân bổ theo hóa đơn (Document-level settlement)"
                        });
                    }
                }

                // Add Uninvoiced Orders
                foreach (var ord in periodOrds)
                {
                    decimal ordPaid = 0m;
                    decimal ordOutstanding = ord.GrandTotal;
                    if (runningGeneralCredit > 0)
                    {
                        decimal alloc = Math.Min(ordOutstanding, runningGeneralCredit);
                        ordPaid += alloc;
                        ordOutstanding -= alloc;
                        runningGeneralCredit -= alloc;
                    }

                    DateTime dueDate = ord.DeliveryDate ?? ord.OrderDate.AddDays(15);
                    if (ordOutstanding > 0)
                    {
                        if (DateTime.UtcNow > dueDate) overdue += ordOutstanding;
                        else notYetDue += ordOutstanding;

                        if (!nextDueDate.HasValue || dueDate < nextDueDate.Value) nextDueDate = dueDate;
                    }

                    var ordStatus = ordOutstanding == 0m ? DebtStatus.Settled
                        : (ordPaid > 0m ? DebtStatus.PartiallyPaid : DebtStatus.Outstanding);

                    dataset.Documents.Add(new DebtDocumentExportRowDto
                    {
                        Direction = DebtDirection.Receivable,
                        PartnerCode = cust.Code,
                        PartnerName = cust.Name,
                        DocumentType = "Đơn bán hàng",
                        DocumentCode = ord.Code,
                        ReferenceCode = null,
                        DocumentDate = ord.OrderDate,
                        DueDate = dueDate,
                        OriginalAmount = ord.GrandTotal,
                        PaidAmount = ordPaid,
                        DiscountAmount = 0m,
                        OutstandingAmount = ordOutstanding,
                        DebtStatus = ordStatus,
                        Notes = ord.Notes,
                        TraceId = $"SO-{ord.Id}"
                    });

                    foreach (var it in ord.Items.OrderBy(x => x.Id))
                    {
                        dataset.LineItems.Add(new DebtLineItemExportRowDto
                        {
                            DocumentCode = ord.Code,
                            DocumentDate = ord.OrderDate,
                            Direction = DebtDirection.Receivable,
                            PartnerCode = cust.Code,
                            PartnerName = cust.Name,
                            ProductCode = it.ProductCode,
                            ProductName = it.ProductName,
                            Specifications = it.Notes,
                            UnitName = it.UnitName,
                            Quantity = it.Quantity,
                            UnitPrice = it.UnitPrice,
                            DiscountAmount = it.DiscountAmount,
                            TaxRate = it.TaxRate,
                            TaxAmount = it.TaxAmount,
                            LineTotal = it.LineTotal,
                            DocumentPaidAmount = ordPaid,
                            DocumentOutstandingAmount = ordOutstanding,
                            AllocationNote = "Phân bổ theo đơn hàng (Document-level settlement)"
                        });
                    }
                }

                // Add Financial Transactions for Sheet D
                foreach (var pay in periodPays)
                {
                    string txDesc = pay.TransactionType switch
                    {
                        DebtTransactionType.Payment => "Phiếu thu tiền (Thu nợ KH)",
                        DebtTransactionType.PaymentDiscount => "Chiết khấu thanh toán KH",
                        DebtTransactionType.Adjustment => "Điều chỉnh công nợ KH",
                        _ => "Phiếu thu"
                    };

                    dataset.Transactions.Add(new DebtTransactionExportRowDto
                    {
                        TransactionType = pay.TransactionType,
                        TransactionTypeDescription = txDesc,
                        PaymentCode = pay.PaymentCode,
                        TransactionDate = pay.PaymentDate,
                        Direction = DebtDirection.Receivable,
                        PartnerCode = cust.Code,
                        PartnerName = cust.Name,
                        ReferenceCode = pay.ReferenceCode ?? (pay.InvoiceId.HasValue ? $"INV-{pay.InvoiceId}" : null),
                        Amount = pay.Amount,
                        PaymentMethod = pay.PaymentMethod,
                        DiscountOrAdjustmentType = pay.TransactionType == DebtTransactionType.PaymentDiscount ? "Chiết khấu sớm" : (pay.TransactionType == DebtTransactionType.Adjustment ? "Điều chỉnh kế toán" : "Thu tiền"),
                        Notes = pay.Notes,
                        CreatedBy = pay.CreatedBy,
                        CreatedAt = pay.CreatedAt,
                        CounterReference = null
                    });
                }

                string statusDesc = closingBalance == 0m ? "Đã thanh toán đủ"
                    : (overdue > 0m ? $"Quá hạn ({overdue:N0} ₫)" : "Trong hạn");

                dataset.Partners.Add(new PartnerStatementSummaryDto
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
                    OverdueBalance = overdue,
                    NotYetDueBalance = notYetDue,
                    NextDueDate = nextDueDate,
                    DebtStatusDescription = statusDesc,
                    BalanceDirectionDescription = closingBalance > 0
                        ? "Số dư phản ánh số tiền khách hàng còn nợ doanh nghiệp cần thu hồi"
                        : "Khách hàng đã hoàn tất nghĩa vụ thanh toán"
                });
            }
        }

        // ---------------------------------------------------------------------
        // 2. PROCESS SUPPLIERS (Công nợ phải trả)
        // ---------------------------------------------------------------------
        if (targetSupIds.Any())
        {
            var suppliers = await _context.Suppliers.AsNoTracking()
                .Where(s => targetSupIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            var supPayments = await _context.PartnerPayments.AsNoTracking()
                .Where(p => p.PartnerType == (int)PartnerType.Supplier && targetSupIds.Contains(p.PartnerId))
                .OrderBy(p => p.PaymentDate)
                .ToListAsync(cancellationToken);

            foreach (var sup in suppliers)
            {
                var pPayments = supPayments.Where(p => p.PartnerId == sup.Id).ToList();

                decimal openingDebits = 0m;
                decimal openingCredits = 0m;

                if (fromDateUtc.HasValue)
                {
                    openingDebits += pPayments.Where(p => p.PaymentDate < fromDateUtc.Value && p.TransactionType == DebtTransactionType.Adjustment).Sum(p => p.Amount);
                    openingCredits += pPayments.Where(p => p.PaymentDate < fromDateUtc.Value && p.TransactionType != DebtTransactionType.Adjustment).Sum(p => p.Amount);
                }

                decimal openingBalance = Math.Max(0m, openingDebits - openingCredits);

                var periodTx = pPayments.Where(p => (!fromDateUtc.HasValue || p.PaymentDate >= fromDateUtc.Value) &&
                                                    (!toDateUtc.HasValue || p.PaymentDate <= toDateUtc.Value)).ToList();

                decimal totalDebits = periodTx.Where(p => p.TransactionType == DebtTransactionType.Adjustment).Sum(p => p.Amount);
                decimal totalCredits = periodTx.Where(p => p.TransactionType == DebtTransactionType.Payment).Sum(p => p.Amount);
                decimal totalDiscounts = periodTx.Where(p => p.TransactionType == DebtTransactionType.PaymentDiscount).Sum(p => p.Amount);

                decimal closingBalance = Math.Max(0m, openingBalance + totalDebits - totalCredits - totalDiscounts);

                // Allocate payment to adjustments
                decimal remainingPayCredit = periodTx.Where(p => p.TransactionType != DebtTransactionType.Adjustment).Sum(p => p.Amount);
                var adjustments = periodTx.Where(p => p.TransactionType == DebtTransactionType.Adjustment).OrderBy(p => p.PaymentDate).ToList();

                decimal overdue = 0m;
                decimal notYetDue = 0m;
                DateTime? nextDueDate = null;

                foreach (var adj in adjustments)
                {
                    decimal paid = Math.Min(adj.Amount, Math.Max(0m, remainingPayCredit));
                    remainingPayCredit -= paid;
                    decimal outstanding = Math.Max(0m, adj.Amount - paid);

                    DateTime dueDate = adj.PaymentDate.AddDays(30);
                    if (outstanding > 0)
                    {
                        if (DateTime.UtcNow > dueDate) overdue += outstanding;
                        else notYetDue += outstanding;

                        if (!nextDueDate.HasValue || dueDate < nextDueDate.Value) nextDueDate = dueDate;
                    }

                    var status = outstanding == 0m ? DebtStatus.Settled
                        : (paid > 0m ? DebtStatus.PartiallyPaid : DebtStatus.Outstanding);

                    dataset.Documents.Add(new DebtDocumentExportRowDto
                    {
                        Direction = DebtDirection.Payable,
                        PartnerCode = sup.Code,
                        PartnerName = sup.Name,
                        DocumentType = "Điều chỉnh nợ NCC",
                        DocumentCode = adj.PaymentCode,
                        ReferenceCode = adj.ReferenceCode,
                        DocumentDate = adj.PaymentDate,
                        DueDate = dueDate,
                        OriginalAmount = adj.Amount,
                        PaidAmount = paid,
                        DiscountAmount = 0m,
                        OutstandingAmount = outstanding,
                        DebtStatus = status,
                        Notes = adj.Notes,
                        TraceId = $"PP-{adj.Id}"
                    });
                }

                // If supplier has pure payment records (without adjustment obligation)
                var paymentsOnly = periodTx.Where(p => p.TransactionType != DebtTransactionType.Adjustment).ToList();
                foreach (var pay in paymentsOnly)
                {
                    dataset.Transactions.Add(new DebtTransactionExportRowDto
                    {
                        TransactionType = pay.TransactionType,
                        TransactionTypeDescription = pay.TransactionType == DebtTransactionType.Payment ? "Phiếu chi thanh toán NCC" : "Chiết khấu thanh toán NCC",
                        PaymentCode = pay.PaymentCode,
                        TransactionDate = pay.PaymentDate,
                        Direction = DebtDirection.Payable,
                        PartnerCode = sup.Code,
                        PartnerName = sup.Name,
                        ReferenceCode = pay.ReferenceCode,
                        Amount = pay.Amount,
                        PaymentMethod = pay.PaymentMethod,
                        DiscountOrAdjustmentType = pay.TransactionType == DebtTransactionType.PaymentDiscount ? "Chiết khấu NCC" : "Thanh toán",
                        Notes = pay.Notes,
                        CreatedBy = pay.CreatedBy,
                        CreatedAt = pay.CreatedAt,
                        CounterReference = null
                    });
                }

                string statusDesc = closingBalance == 0m ? "Không có nợ đọng"
                    : (overdue > 0m ? $"Quá hạn ({overdue:N0} ₫)" : "Còn nợ nhà cung cấp");

                dataset.Partners.Add(new PartnerStatementSummaryDto
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
                    TotalDiscounts = totalDiscounts,
                    ClosingBalance = closingBalance,
                    OverdueBalance = overdue,
                    NotYetDueBalance = notYetDue,
                    NextDueDate = nextDueDate,
                    DebtStatusDescription = statusDesc,
                    BalanceDirectionDescription = closingBalance > 0
                        ? "Doanh nghiệp còn nghĩa vụ nợ phải trả cho nhà cung cấp"
                        : "Không có dư nợ phải trả"
                });
            }
        }

        // Apply additional balance threshold filters if provided in DebtFilterDto
        if (filter != null)
        {
            if (filter.MinBalance.HasValue)
            {
                dataset.Partners = dataset.Partners.Where(p => p.ClosingBalance >= filter.MinBalance.Value).ToList();
            }
            if (filter.MaxBalance.HasValue)
            {
                dataset.Partners = dataset.Partners.Where(p => p.ClosingBalance <= filter.MaxBalance.Value).ToList();
            }
            if (filter.DebtStatus.HasValue)
            {
                if (filter.DebtStatus.Value == DebtStatus.Settled)
                {
                    dataset.Partners = dataset.Partners.Where(p => p.ClosingBalance == 0).ToList();
                }
                else if (filter.DebtStatus.Value == DebtStatus.Outstanding)
                {
                    dataset.Partners = dataset.Partners.Where(p => p.ClosingBalance > 0 && p.TotalCredits == 0).ToList();
                }
                else if (filter.DebtStatus.Value == DebtStatus.PartiallyPaid)
                {
                    dataset.Partners = dataset.Partners.Where(p => p.ClosingBalance > 0 && p.TotalCredits > 0).ToList();
                }
            }
        }

        // Sort Documents and Transactions chronologically descending
        dataset.Documents = dataset.Documents.OrderByDescending(d => d.DocumentDate).ToList();
        dataset.LineItems = dataset.LineItems.OrderByDescending(l => l.DocumentDate).ToList();
        dataset.Transactions = dataset.Transactions.OrderByDescending(t => t.TransactionDate).ToList();

        return dataset;
    }

    private static DebtExportDataset FilterDatasetForSinglePartner(DebtExportDataset source, int partnerId, PartnerType type)
    {
        var partner = source.Partners.FirstOrDefault(p => p.PartnerId == partnerId && p.PartnerType == type);
        string code = partner?.Code ?? "";

        return new DebtExportDataset
        {
            Partners = partner != null ? new List<PartnerStatementSummaryDto> { partner } : new(),
            Documents = source.Documents.Where(d => d.PartnerCode == code).ToList(),
            LineItems = source.LineItems.Where(l => l.PartnerCode == code).ToList(),
            Transactions = source.Transactions.Where(t => t.PartnerCode == code).ToList()
        };
    }

    // =========================================================================
    // EXCEL HELPER FORMATTERS
    // =========================================================================

    private static void SetupPageLayout(IXLWorksheet ws)
    {
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.PaperSize = XLPaperSize.A4Paper;
        ws.PageSetup.FitToPages(1, 0); // Fit 1 page wide
    }

    private static int RenderCompanyHeader(IXLWorksheet ws, int startRow, CompanySettings company)
    {
        int row = startRow;
        ws.Cell(row, 1).Value = company.CompanyName.ToUpper();
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 11;
        ws.Cell(row, 1).Style.Font.FontColor = BrandDark;
        row++;

        ws.Cell(row, 1).Value = $"Địa chỉ: {company.Address ?? "Trụ sở chính TradeFlow"}  |  MST: {company.TaxCode ?? "0319205643"}  |  SĐT: {company.Phone ?? "0901234567"}";
        ws.Cell(row, 1).Style.Font.FontSize = 9;
        ws.Cell(row, 1).Style.Font.FontColor = MutedText;
        row += 2;

        return row;
    }

    private static void RenderSignatureSection(IXLWorksheet ws, int startRow)
    {
        int sigRow = startRow + 2;
        ws.Cell(sigRow, 2).Value = "NGƯỜI LẬP BIỂU";
        ws.Cell(sigRow, 2).Style.Font.Bold = true;
        ws.Cell(sigRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(sigRow, 6).Value = "KẾ TOÁN CÔNG NỢ";
        ws.Cell(sigRow, 6).Style.Font.Bold = true;
        ws.Cell(sigRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(sigRow, 11).Value = "KẾ TOÁN TRƯỞNG / GIÁM ĐỐC";
        ws.Cell(sigRow, 11).Style.Font.Bold = true;
        ws.Cell(sigRow, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(sigRow + 1, 2).Value = "(Ký và ghi rõ họ tên)";
        ws.Cell(sigRow + 1, 2).Style.Font.Italic = true;
        ws.Cell(sigRow + 1, 2).Style.Font.FontSize = 9;
        ws.Cell(sigRow + 1, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(sigRow + 1, 6).Value = "(Ký và ghi rõ họ tên)";
        ws.Cell(sigRow + 1, 6).Style.Font.Italic = true;
        ws.Cell(sigRow + 1, 6).Style.Font.FontSize = 9;
        ws.Cell(sigRow + 1, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        ws.Cell(sigRow + 1, 11).Value = "(Ký, đóng dấu và ghi rõ họ tên)";
        ws.Cell(sigRow + 1, 11).Style.Font.Italic = true;
        ws.Cell(sigRow + 1, 11).Style.Font.FontSize = 9;
        ws.Cell(sigRow + 1, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void SetNumericCell(IXLCell cell, double value)
    {
        cell.Value = value;
        cell.Style.NumberFormat.Format = "#,##0 ₫";
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static void AddFinancialMetricRow(IXLWorksheet ws, ref int row, string label, decimal amount, string hint)
    {
        ws.Cell(row, 1).Value = label;
        ws.Cell(row, 1).Style.Font.Bold = true;

        var amountCell = ws.Cell(row, 3);
        SetNumericCell(amountCell, (double)amount);
        amountCell.Style.Font.Bold = true;

        ws.Cell(row, 4).Value = hint;
        ws.Cell(row, 4).Style.Font.Italic = true;
        ws.Cell(row, 4).Style.Font.FontColor = MutedText;

        ws.Range(row, 1, row, 7).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ws.Range(row, 1, row, 7).Style.Border.BottomBorderColor = BorderLight;
        row++;
    }

    private static void AddKeyValueRow(IXLWorksheet ws, ref int row, string key1, string val1, string key2, string val2)
    {
        ws.Cell(row, 1).Value = key1;
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = val1;

        ws.Cell(row, 4).Value = key2;
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 5).Value = val2;

        ws.Range(row, 1, row, 7).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
        ws.Range(row, 1, row, 7).Style.Border.BottomBorderColor = BorderLight;
        row++;
    }

    public static string SafeSheetName(string desiredName, HashSet<string> existingNames)
    {
        if (string.IsNullOrWhiteSpace(desiredName)) desiredName = "Sheet";

        var invalidChars = new[] { '\\', '/', '?', '*', '[', ']', ':', '\'', '"' };
        var clean = new StringBuilder();
        foreach (var ch in desiredName)
        {
            clean.Append(invalidChars.Contains(ch) ? '-' : ch);
        }

        var trimmed = clean.ToString().Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) trimmed = "Sheet";

        if (trimmed.Length > 31)
        {
            trimmed = trimmed.Substring(0, 31).TrimEnd('-', '_', ' ');
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
}

/// <summary>
/// Internal container for loaded export data across sheets
/// </summary>
public class DebtExportDataset
{
    public List<PartnerStatementSummaryDto> Partners { get; set; } = new();
    public List<DebtDocumentExportRowDto> Documents { get; set; } = new();
    public List<DebtLineItemExportRowDto> LineItems { get; set; } = new();
    public List<DebtTransactionExportRowDto> Transactions { get; set; } = new();
}
