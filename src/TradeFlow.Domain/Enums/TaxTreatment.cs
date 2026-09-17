namespace TradeFlow.Domain.Enums;

/// <summary>
/// Phân loại đối tượng chịu thuế Giá trị gia tăng (VAT) của hàng hóa, dịch vụ.
/// </summary>
public enum TaxTreatment
{
    /// <summary>Không chịu thuế / Không thuộc diện kê khai nộp thuế GTGT (0% miễn thuế)</summary>
    NonTaxable = 0,

    /// <summary>Thuế suất 0% (Hàng xuất khẩu, vận tải quốc tế...)</summary>
    ZeroRate = 1,

    /// <summary>Thuế suất 5% (Nước sạch, thiết bị y tế, thuốc, đồ dùng nông nghiệp...)</summary>
    Rate5 = 2,

    /// <summary>
    /// Thuế suất chuẩn 10%.
    /// Có thể áp dụng giảm còn 8% nếu hàng hóa đủ điều kiện giảm thuế theo chính sách tạm thời (Nghị định 72/2024 đến 31/12/2026).
    /// </summary>
    Standard10 = 3,

    /// <summary>Thuế suất 8% (Cố định 8% nếu người dùng chỉ định cụ thể)</summary>
    Rate8 = 4
}
