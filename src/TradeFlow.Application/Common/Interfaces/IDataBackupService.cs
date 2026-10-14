using System.IO;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TradeFlow.Application.Common.Models.Settings;

namespace TradeFlow.Application.Common.Interfaces;

public interface IDataBackupService
{
    Task<BackupResult> CreateBackupAsync(BackupOptions options, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<Stream?> GetPreparedBackupStreamAsync(string backupId, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<BackupMetadataDto?> GetLatestBackupInfoAsync(CancellationToken cancellationToken = default);
}
