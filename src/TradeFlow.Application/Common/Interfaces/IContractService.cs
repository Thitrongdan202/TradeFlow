using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TradeFlow.Application.Common.Models.Contracts;

namespace TradeFlow.Application.Common.Interfaces;

public interface IContractService
{
    Task<ContractPartyASettingsDto> GetPartyASettingsAsync(CancellationToken cancellationToken = default);
    Task<ContractPartyASettingsDto> UpdatePartyASettingsAsync(ContractPartyASettingsDto dto, string updatedBy, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ContractTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<ContractTemplateDto?> GetTemplateByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<List<ContractDto>> GetContractsAsync(ContractFilterDto? filter = null, CancellationToken cancellationToken = default);
    Task<ContractDto?> GetContractByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ContractDto> CreateContractAsync(CreateContractDto dto, string userId, string userName, CancellationToken cancellationToken = default);
    Task<ContractDto> UpdateContractAsync(int id, UpdateContractDto dto, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> DeleteContractAsync(int id, string userId, string userName, CancellationToken cancellationToken = default);

    Task<ContractDto> FinalizeContractAsync(int id, string userId, string userName, CancellationToken cancellationToken = default);
    Task<ContractDto> SignContractAsync(int id, SignContractDto dto, string userId, string userName, CancellationToken cancellationToken = default);
    Task<ContractDto> RecordPrintedAsync(int id, string printedBy, CancellationToken cancellationToken = default);
    Task<ContractDto> RecordPaperSignedAsync(int id, int attachmentId, string signedBy, CancellationToken cancellationToken = default);
    Task<ContractDto> LiquidateContractAsync(int id, string reason, string userId, string userName, CancellationToken cancellationToken = default);
    Task<ContractDto> CancelContractAsync(int id, string reason, string userId, string userName, CancellationToken cancellationToken = default);

    Task<byte[]> GenerateContractPdfAsync(int id, CancellationToken cancellationToken = default);
}
