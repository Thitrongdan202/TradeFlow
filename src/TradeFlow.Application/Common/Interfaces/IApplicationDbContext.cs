namespace TradeFlow.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the database context, used by Application layer.
/// Concrete implementation lives in Infrastructure.
/// This interface avoids direct EF Core DbSet dependency in the Application layer.
/// </summary>
public interface IApplicationDbContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
