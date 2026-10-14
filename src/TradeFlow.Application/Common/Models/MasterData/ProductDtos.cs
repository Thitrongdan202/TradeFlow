using System.Collections.Generic;

namespace TradeFlow.Application.Common.Models.MasterData;

public class DeleteProductResult
{
    public bool Succeeded { get; set; }
    public bool IsReferenced { get; set; }
    public int ReferenceCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class BulkDeleteProductResult
{
    public int RequestedCount { get; set; }
    public int DeletedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Messages { get; set; } = new();
}
