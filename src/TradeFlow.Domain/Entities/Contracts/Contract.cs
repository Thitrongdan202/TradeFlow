using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Contracts;

/// <summary>
/// Hợp đồng kinh tế / Hợp đồng thương mại & phụ lục trong hệ thống TradeFlow.
/// </summary>
public class Contract : AuditableEntity<int>
{
    public Contract() { }

    /// <summary>Số hợp đồng độc nhất (ví dụ: HD-202610-0001, HDMB/2026/001)</summary>
    public string ContractNumber { get; set; } = string.Empty;

    /// <summary>Tiêu đề hợp đồng</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Mã loại mẫu hợp đồng đã sử dụng (B2B_SALE, B2C_RETAIL, SUPPLY_INSTALL, PRINCIPLE, DEPOSIT, LIQUIDATION, APPENDIX)</summary>
    public string TemplateCode { get; set; } = "B2B_SALE";

    /// <summary>Phiên bản mẫu hợp đồng tại thời điểm tạo</summary>
    public string TemplateVersion { get; set; } = "1.0";

    /// <summary>Trạng thái hiện tại của hợp đồng</summary>
    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    /// <summary>Ngày ký kết / Ngày lập hợp đồng</summary>
    public DateTime ContractDate { get; set; } = DateTime.UtcNow;

    /// <summary>Địa điểm lập / ký kết hợp đồng (ví dụ: Cần Thơ, TP.HCM)</summary>
    public string ContractPlace { get; set; } = "Cần Thơ";

    /// <summary>Ngày bắt đầu có hiệu lực</summary>
    public DateTime? EffectiveDate { get; set; }

    /// <summary>Ngày hết hạn hiệu lực (nếu có)</summary>
    public DateTime? ExpirationDate { get; set; }

    /// <summary>Loại chủ thể Bên B (Doanh nghiệp hoặc Cá nhân)</summary>
    public ContractPartyBType PartyBType { get; set; } = ContractPartyBType.Company;

    /// <summary>ID khách hàng / nhà cung cấp liên kết trong MasterData (nếu có)</summary>
    public int? PartnerId { get; set; }
    public Customer? PartnerCustomer { get; set; }

    // ==========================================
    // BÊN A (ĐƠN VỊ BÁN HÀNG / CUNG CẤP) - SNAPSHOT
    // ==========================================
    public string PartyAName { get; set; } = string.Empty;
    public string? PartyATaxCode { get; set; }
    public string? PartyAAddress { get; set; }
    public string? PartyAContactAddress { get; set; }
    public string? PartyARepresentative { get; set; }
    public string? PartyAPosition { get; set; }
    public string? PartyAAuthority { get; set; }
    public string? PartyAPhone { get; set; }
    public string? PartyAEmail { get; set; }
    public string? PartyABankName { get; set; }
    public string? PartyABankAccount { get; set; }
    public string? PartyABankAccountHolder { get; set; }
    public string? PartyABankBranch { get; set; }

    // ==========================================
    // BÊN B (BÊN MUA / KHÁCH HÀNG / ĐỐI TÁC) - SNAPSHOT
    // ==========================================
    public string PartyBName { get; set; } = string.Empty;
    public string? PartyBTaxCode { get; set; }
    public string? PartyBIdCardNumber { get; set; }
    public string? PartyBIdCardDate { get; set; }
    public string? PartyBIdCardPlace { get; set; }
    public string? PartyBAddress { get; set; }
    public string? PartyBDeliveryAddress { get; set; }
    public string? PartyBPhone { get; set; }
    public string? PartyBEmail { get; set; }
    public string? PartyBRepresentative { get; set; }
    public string? PartyBPosition { get; set; }
    public string? PartyBAuthority { get; set; }
    public string? PartyBBankName { get; set; }
    public string? PartyBBankAccount { get; set; }

    // ==========================================
    // GIÁ TRỊ TÀI CHÍNH
    // ==========================================
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public string? AmountInWords { get; set; }

    // ==========================================
    // NỘI DUNG ĐIỀU KHOẢN THƯƠNG MẠI & PHÁP LÝ
    // ==========================================
    public string? PaymentTerms { get; set; }
    public string? DeliveryTerms { get; set; }
    public string? InstallationTerms { get; set; }
    public string? WarrantyTerms { get; set; }
    public string? BreachPenaltyTerms { get; set; }
    public string? DisputeResolutionTerms { get; set; }
    public string? GeneralTerms { get; set; }
    public string CustomClausesJson { get; set; } = "[]";

    // ==========================================
    // PHƯƠNG THỨC KÝ & XÁC THỰC
    // ==========================================
    public ContractExecutionMethod ExecutionMethod { get; set; } = ContractExecutionMethod.NotSpecified;
    
    // Chữ ký Bên A
    public DateTime? PartyASignedAt { get; set; }
    public string? PartyASignedBy { get; set; }
    public string? PartyASignatureData { get; set; } // Base64 signature image or digital signature hash

    // Chữ ký Bên B
    public DateTime? PartyBSignedAt { get; set; }
    public string? PartyBSignedBy { get; set; }
    public string? PartyBSignatureData { get; set; }

    /// <summary>Mã băm SHA-256 toàn vẹn của văn bản hợp đồng</summary>
    public string DocumentHash { get; set; } = string.Empty;

    // Ký giấy / In ấn
    public bool IsPaperPrinted { get; set; }
    public DateTime? PrintedAt { get; set; }
    public string? PrintedBy { get; set; }
    public int? PaperSignedScanAttachmentId { get; set; }

    // ==========================================
    // DANH TÍNH NGƯỜI TẠO & KIỂM TOÁN BẮT BUỘC
    // ==========================================
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime? FinalizedAt { get; set; }
    public string? FinalizedBy { get; set; }

    /// <summary>Liên kết hợp đồng gốc (dùng cho Phụ lục hợp đồng hoặc Biên bản thanh lý)</summary>
    public int? ParentContractId { get; set; }
    public Contract? ParentContract { get; set; }
    public ICollection<Contract> ChildContracts { get; set; } = new List<Contract>();

    public string? Notes { get; set; }

    /// <summary>Danh mục sản phẩm / dịch vụ theo hợp đồng</summary>
    public ICollection<ContractItem> Items { get; set; } = new List<ContractItem>();
}
