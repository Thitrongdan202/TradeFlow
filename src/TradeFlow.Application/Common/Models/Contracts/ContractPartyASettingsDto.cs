namespace TradeFlow.Application.Common.Models.Contracts;

public class ContractPartyASettingsDto
{
    public int Id { get; set; } = 1;
    public string CompanyName { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public string TaxCode { get; set; } = string.Empty;
    public string? BusinessRegistrationNumber { get; set; }
    public string RegisteredAddress { get; set; } = string.Empty;
    public string? ContactAddress { get; set; }
    public string RepresentativeName { get; set; } = string.Empty;
    public string RepresentativePosition { get; set; } = string.Empty;
    public string? RepresentativeAuthority { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? BankName { get; set; }
    public string? BankAccount { get; set; }
    public string? BankAccountHolder { get; set; }
    public string? BankBranch { get; set; }
    public string? DefaultPaymentInstructions { get; set; }
    public string? DefaultDeliveryTerms { get; set; }
    public string? DefaultWarrantyTerms { get; set; }
}
