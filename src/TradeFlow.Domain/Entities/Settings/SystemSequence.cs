using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.Settings;

/// <summary>
/// Cấu hình và bộ đếm mã số tự động cho các đối tượng trong hệ thống.
/// </summary>
public class SystemSequence : AuditableEntity<int>
{
    public SystemSequence() { }

    public SystemSequence(string sequenceKey, string prefix, string formatPattern = "{Prefix}{Number:D6}", string? description = null)
    {
        SequenceKey = sequenceKey;
        Prefix = prefix;
        FormatPattern = formatPattern;
        Description = description;
        CurrentNumber = 0;
        Step = 1;
    }

    /// <summary>Khóa định danh đối tượng (vd: Product, Customer, Supplier, Warehouse, ProductCategory, UnitOfMeasure)</summary>
    public string SequenceKey { get; set; } = string.Empty;

    /// <summary>Tiền tố mã (vd: SP, KH, NCC, KHO, DM, DV)</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>Giá trị số hiện tại của sequence</summary>
    public long CurrentNumber { get; set; }

    /// <summary>Bước nhảy (mặc định 1)</summary>
    public int Step { get; set; } = 1;

    /// <summary>Mẫu định dạng mã (vd: {Prefix}{Number:D6} hoặc {Prefix}-{Number:D6})</summary>
    public string FormatPattern { get; set; } = "{Prefix}{Number:D6}";

    /// <summary>Mô tả quy tắc đánh mã</summary>
    public string? Description { get; set; }
}