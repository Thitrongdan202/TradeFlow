using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.MasterData;

public class UnitOfMeasure : AuditableEntity<int>
{
    public UnitOfMeasure() { }

    public string Code { get; set; } = string.Empty;    // Mã
    public string Name { get; set; } = string.Empty;    // Tên
    public string? Symbol { get; set; }                  // Ký hiệu
    public bool IsActive { get; set; } = true;
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
