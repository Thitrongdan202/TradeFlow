using TradeFlow.Domain.Common;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Contracts;

/// <summary>
/// Mẫu hợp đồng trong thư viện mẫu hợp đồng thương mại của hệ thống TradeFlow.
/// </summary>
public class ContractTemplate : AuditableEntity<int>
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Thương mại";
    public string Description { get; set; } = string.Empty;
    public string TypicalUseCase { get; set; } = string.Empty;
    public ContractPartyBType? TargetPartyType { get; set; }
    public string RequiredInfo { get; set; } = string.Empty;
    public string Icon { get; set; } = "file-text";
    public string Version { get; set; } = "1.0";
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public string LegalBasis { get; set; } = string.Empty;
    public string KnownLimitations { get; set; } = string.Empty;
    public bool IsReviewedByLegal { get; set; } = false;
    public string DefaultClausesJson { get; set; } = "[]";
}
