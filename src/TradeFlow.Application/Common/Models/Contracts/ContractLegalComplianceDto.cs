using System;
using System.Collections.Generic;

namespace TradeFlow.Application.Common.Models.Contracts;

public enum LegalComplianceStatus
{
    DataComplete = 1,        // Đạt kiểm tra dữ liệu
    MissingInfo = 2,         // Thiếu thông tin
    RequiresLegalReview = 3, // Cần người có chuyên môn pháp lý xem xét
    PotentialLegalRisk = 4   // Có dấu hiệu không phù hợp pháp luật
}

public enum LegalCheckItemStatus
{
    Passed = 1,
    Warning = 2,
    Failed = 3,
    NotApplicable = 4
}

public class LegalCheckItemDto
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LegalReference { get; set; } = string.Empty;
    public LegalCheckItemStatus Status { get; set; } = LegalCheckItemStatus.Passed;
    public string Details { get; set; } = string.Empty;
}

public class ContractLegalComplianceReportDto
{
    public LegalComplianceStatus OverallStatus { get; set; } = LegalComplianceStatus.DataComplete;
    public string StatusText => OverallStatus switch
    {
        LegalComplianceStatus.DataComplete => "Đạt kiểm tra dữ liệu cơ bản",
        LegalComplianceStatus.MissingInfo => "Thiếu thông tin bắt buộc",
        LegalComplianceStatus.RequiresLegalReview => "Cần người có chuyên môn pháp lý xem xét",
        LegalComplianceStatus.PotentialLegalRisk => "Có dấu hiệu không phù hợp pháp luật",
        _ => "Chưa xác định"
    };

    public string StatusBadgeClass => OverallStatus switch
    {
        LegalComplianceStatus.DataComplete => "bg-success-subtle text-success border border-success-subtle",
        LegalComplianceStatus.MissingInfo => "bg-warning-subtle text-warning-emphasis border border-warning-subtle",
        LegalComplianceStatus.RequiresLegalReview => "bg-info-subtle text-info-emphasis border border-info-subtle",
        LegalComplianceStatus.PotentialLegalRisk => "bg-danger-subtle text-danger border border-danger-subtle",
        _ => "bg-secondary-subtle text-secondary"
    };

    public string SummaryNotes { get; set; } = string.Empty;
    public List<LegalCheckItemDto> Items { get; set; } = new();

    public int PassedCount => Items.FindAll(x => x.Status == LegalCheckItemStatus.Passed).Count;
    public int WarningCount => Items.FindAll(x => x.Status == LegalCheckItemStatus.Warning).Count;
    public int FailedCount => Items.FindAll(x => x.Status == LegalCheckItemStatus.Failed).Count;
    public bool IsFullyCompliant => FailedCount == 0 && WarningCount == 0;
    public int ComplianceScore => Items.Count == 0 ? 100 : (int)Math.Round((double)PassedCount / Items.Count * 100);

    public string Disclaimer { get; set; } = 
        "Hệ thống kiểm tra tính đầy đủ kỹ thuật và đối chiếu quy định pháp luật. Việc xác định hiệu lực pháp lý chính thức cần được người có thẩm quyền hoặc chuyên gia pháp lý của doanh nghiệp phê duyệt trước khi ký kết.";
}
