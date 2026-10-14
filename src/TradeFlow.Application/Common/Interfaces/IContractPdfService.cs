using TradeFlow.Application.Common.Models.Contracts;

namespace TradeFlow.Application.Common.Interfaces;

public interface IContractPdfService
{
    byte[] GeneratePdf(ContractDto contract, ContractPartyASettingsDto? partyASettings);
}
