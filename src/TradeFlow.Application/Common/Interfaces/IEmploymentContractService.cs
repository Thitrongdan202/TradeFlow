using TradeFlow.Application.Common.Models.Contracts;

namespace TradeFlow.Application.Common.Interfaces;

public interface IEmploymentContractService
{
    Task<List<EmploymentContractDto>> GetContractsAsync(EmploymentContractFilterDto filter, bool canViewSensitiveData = true, CancellationToken cancellationToken = default);
    Task<EmploymentContractDto?> GetContractByIdAsync(int id, bool canViewSensitiveData = true, CancellationToken cancellationToken = default);
    Task<EmploymentContractDto> CreateContractAsync(CreateEmploymentContractDto dto, string userId, string userName, CancellationToken cancellationToken = default);
    Task<EmploymentContractDto> UpdateContractAsync(UpdateEmploymentContractDto dto, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> FinalizeContractAsync(int id, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> SignContractAsync(SignEmploymentContractDto dto, string userId, string userName, CancellationToken cancellationToken = default);
    Task<EmploymentContractDto> RenewContractAsync(RenewEmploymentContractDto dto, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> TerminateContractAsync(int id, DateTime terminationDate, string reason, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> CancelContractAsync(int id, string reason, string userId, string userName, CancellationToken cancellationToken = default);
    Task<bool> DeleteContractAsync(int id, string userId, string userName, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePdfAsync(int id, CancellationToken cancellationToken = default);
    byte[] GeneratePdfFromDto(EmploymentContractDto dto);
    Task<EmploymentContractStatsDto> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

public class EmploymentContractStatsDto
{
    public int TotalCount { get; set; }
    public int DraftCount { get; set; }
    public int PendingSignatureCount { get; set; }
    public int ActiveCount { get; set; }
    public int ExpiringSoonCount { get; set; }
    public int ExpiredCount { get; set; }
    public int TerminatedCount { get; set; }
    public int IndefiniteTermCount { get; set; }
    public int FixedTermCount { get; set; }
    public int ProbationCount { get; set; }
}
