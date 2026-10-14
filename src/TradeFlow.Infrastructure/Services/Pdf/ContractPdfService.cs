using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Contracts;

namespace TradeFlow.Infrastructure.Services.Pdf;

public class ContractPdfService : IContractPdfService
{
    static ContractPdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePdf(ContractDto contract, ContractPartyASettingsDto? partyASettings)
    {
        var document = new ContractPdfDocument(contract, partyASettings);
        return document.GeneratePdf();
    }
}
