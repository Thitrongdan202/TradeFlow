using System;
using System.Collections.Generic;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Contracts;

public class CreateContractDto
{
    public string Title { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = "B2B_SALE";
    public DateTime ContractDate { get; set; } = DateTime.UtcNow;
    public string ContractPlace { get; set; } = "Cần Thơ";
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    public ContractPartyBType PartyBType { get; set; } = ContractPartyBType.Company;
    public int? PartnerId { get; set; }

    // Party A (from PartyASettings with optional overrides)
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

    // Party B
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

    // Financial
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TaxRate { get; set; } = 10;
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string CurrencyCode { get; set; } = "VND";
    public string? AmountInWords { get; set; }

    // Legal Terms
    public string? PaymentTerms { get; set; }
    public string? DeliveryTerms { get; set; }
    public string? InstallationTerms { get; set; }
    public string? WarrantyTerms { get; set; }
    public string? BreachPenaltyTerms { get; set; }
    public string? DisputeResolutionTerms { get; set; }
    public string? GeneralTerms { get; set; }
    public List<ContractClauseDto> Clauses { get; set; } = new();

    public int? ParentContractId { get; set; }
    public string? Notes { get; set; }

    public List<ContractItemDto> Items { get; set; } = new();
}

public class UpdateContractDto : CreateContractDto
{
    public int Id { get; set; }
}

public class ContractFilterDto
{
    public string? SearchTerm { get; set; }
    public ContractStatus? Status { get; set; }
    public string? TemplateCode { get; set; }
    public ContractPartyBType? PartyBType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class SignContractDto
{
    public string SignerParty { get; set; } = "PartyA"; // "PartyA" or "PartyB"
    public string SignerName { get; set; } = string.Empty;
    public string? SignatureData { get; set; } // Base64 data URL
    public string? Pin { get; set; }
}

public class RecordPaperSignedDto
{
    public int AttachmentId { get; set; }
    public string SignedBy { get; set; } = string.Empty;
    public DateTime SignedDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
