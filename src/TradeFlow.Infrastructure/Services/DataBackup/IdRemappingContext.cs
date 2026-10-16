using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TradeFlow.Infrastructure.Services.DataBackup;

/// <summary>
/// Context lưu trữ bảng ánh xạ định danh (Identifier & Foreign Key Remapping)
/// bảo toàn toàn vẹn tham chiếu quan hệ giữa các thực thể khi nhập dữ liệu từ bản sao lưu.
/// </summary>
public class IdRemappingContext
{
    private readonly Dictionary<(string EntityType, int SourceId), int> _intMap = new();
    private readonly Dictionary<(string EntityType, string SourceId), string> _stringMap = new();

    public void Map(string entityType, int sourceId, int destinationId)
    {
        _intMap[(entityType, sourceId)] = destinationId;
    }

    public void Map(string entityType, string sourceId, string destinationId)
    {
        _stringMap[(entityType, sourceId)] = destinationId;
    }

    public int? Get(string entityType, int? sourceId)
    {
        if (!sourceId.HasValue) return null;
        if (_intMap.TryGetValue((entityType, sourceId.Value), out var destId))
        {
            return destId;
        }
        return sourceId.Value;
    }

    public int Resolve(string entityType, int sourceId)
    {
        if (_intMap.TryGetValue((entityType, sourceId), out var destId))
        {
            return destId;
        }
        return sourceId;
    }

    public string? Get(string entityType, string? sourceId)
    {
        if (string.IsNullOrEmpty(sourceId)) return sourceId;
        if (_stringMap.TryGetValue((entityType, sourceId), out var destId))
        {
            return destId;
        }
        return sourceId;
    }
}
