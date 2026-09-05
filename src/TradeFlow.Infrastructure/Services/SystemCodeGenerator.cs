using Microsoft.EntityFrameworkCore;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Dịch vụ sinh mã định danh hệ thống (SP000001, KH000001...) tuần tự, an toàn đa luồng.
/// </summary>
public class SystemCodeGenerator : ISystemCodeGenerator
{
    private readonly TradeFlowDbContext _context;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public SystemCodeGenerator(TradeFlowDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateCodeAsync(string sequenceKey, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var seq = await _context.SystemSequences
                .FirstOrDefaultAsync(s => s.SequenceKey == sequenceKey, cancellationToken);

            if (seq == null)
            {
                var prefix = sequenceKey.ToUpperInvariant();
                var description = $"Mã hệ thống cho {sequenceKey}";
                if (SystemCodeConstants.Defaults.TryGetValue(sequenceKey, out var def))
                {
                    prefix = def.Prefix;
                    description = def.Description;
                }

                seq = new SystemSequence(sequenceKey, prefix, "{Prefix}{Number:D6}", description)
                {
                    CurrentNumber = 1,
                    Step = 1,
                    CreatedAt = DateTime.UtcNow
                };
                _context.SystemSequences.Add(seq);
                await _context.SaveChangesAsync(cancellationToken);

                return FormatCode(seq.Prefix, seq.CurrentNumber, seq.FormatPattern);
            }

            seq.CurrentNumber += seq.Step;
            seq.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return FormatCode(seq.Prefix, seq.CurrentNumber, seq.FormatPattern);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string> PeekNextCodeAsync(string sequenceKey, CancellationToken cancellationToken = default)
    {
        var seq = await _context.SystemSequences
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SequenceKey == sequenceKey, cancellationToken);

        if (seq == null)
        {
            var prefix = sequenceKey.ToUpperInvariant();
            if (SystemCodeConstants.Defaults.TryGetValue(sequenceKey, out var def))
            {
                prefix = def.Prefix;
            }
            return FormatCode(prefix, 1, "{Prefix}{Number:D6}");
        }

        return FormatCode(seq.Prefix, seq.CurrentNumber + seq.Step, seq.FormatPattern);
    }

    public static string FormatCode(string prefix, long number, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            pattern = "{Prefix}{Number:D6}";
        }

        var formatted = pattern
            .Replace("{Prefix}", prefix)
            .Replace("{Number:D6}", number.ToString("D6"))
            .Replace("{Number:D5}", number.ToString("D5"))
            .Replace("{Number:D4}", number.ToString("D4"))
            .Replace("{Number}", number.ToString("D6"));

        return formatted;
    }
}