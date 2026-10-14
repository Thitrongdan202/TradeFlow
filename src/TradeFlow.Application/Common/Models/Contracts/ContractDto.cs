using System;
using System.Collections.Generic;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Contracts;

public class ContractDto
{
    public int Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateVersion { get; set; } = "1.0";
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    public DateTime ContractDate { get; set; } = DateTime.UtcNow;
    public string ContractPlace { get; set; } = "Cần Thơ";
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    public ContractPartyBType PartyBType { get; set; } = ContractPartyBType.Company;
    public int? PartnerId { get; set; }
    public string? PartnerCustomerName { get; set; }

    // Party A Snapshot
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

    // Party B Snapshot
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
    public decimal TaxRate { get; set; }
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
    public string CustomClausesJson { get; set; } = "[]";
    public List<ContractClauseDto> Clauses { get; set; } = new();

    // Execution & Signature
    public ContractExecutionMethod ExecutionMethod { get; set; } = ContractExecutionMethod.NotSpecified;
    public DateTime? PartyASignedAt { get; set; }
    public string? PartyASignedBy { get; set; }
    public string? PartyASignatureData { get; set; }
    public DateTime? PartyBSignedAt { get; set; }
    public string? PartyBSignedBy { get; set; }
    public string? PartyBSignatureData { get; set; }
    public string DocumentHash { get; set; } = string.Empty;

    public bool IsPaperPrinted { get; set; }
    public DateTime? PrintedAt { get; set; }
    public string? PrintedBy { get; set; }
    public int? PaperSignedScanAttachmentId { get; set; }

    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
    public DateTime? FinalizedAt { get; set; }
    public string? FinalizedBy { get; set; }

    public int? ParentContractId { get; set; }
    public string? ParentContractNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public List<ContractItemDto> Items { get; set; } = new();
}
