using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.MasterData;

/// <summary>
/// Siêu dữ liệu hình ảnh sản phẩm (lưu trữ metadata trong PostgreSQL, file thực tế lưu trong storage abstraction).
/// </summary>
public class ProductImage : AuditableEntity<int>
{
    public ProductImage() { }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Tên file gốc</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Đường dẫn hoặc định danh lưu trữ (storage reference)</summary>
    public string StorageReference { get; set; } = string.Empty;

    /// <summary>Loại MIME (vd: image/jpeg, image/png)</summary>
    public string? ContentType { get; set; }

    /// <summary>Kích thước file (bytes)</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>Là ảnh đại diện chính?</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Thứ tự sắp xếp hiển thị</summary>
    public int SortOrder { get; set; }

    /// <summary>Đang hoạt động</summary>
    public bool IsActive { get; set; } = true;
}