using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.Contracts;

/// <summary>
/// Cấu hình thông tin pháp nhân Bên A mặc định dùng trong việc tạo lập hợp đồng.
/// Lưu trữ bền vững trong CSDL, không phải nhập lại mỗi lần tạo hợp đồng.
/// </summary>
public class ContractPartyASettings : AuditableEntity<int>
{
    public ContractPartyASettings()
    {
        Id = 1;
    }

    /// <summary>Tên doanh nghiệp / Đơn vị bán hàng</summary>
    public string CompanyName { get; set; } = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM";

    /// <summary>Tên giao dịch viết tắt</summary>
    public string? TradingName { get; set; } = "LACASA";

    /// <summary>Mã số thuế / Mã số doanh nghiệp</summary>
    public string TaxCode { get; set; } = "0316899999";

    /// <summary>Số giấy chứng nhận ĐKKD (nếu khác MST)</summary>
    public string? BusinessRegistrationNumber { get; set; }

    /// <summary>Địa chỉ trụ sở chính (theo ĐKKD)</summary>
    public string RegisteredAddress { get; set; } = "Số 123 Đường Nguyễn Văn Cừ, Phường An Khánh, Quận Ninh Kiều, TP. Cần Thơ";

    /// <summary>Địa chỉ văn phòng giao dịch / Showroom</summary>
    public string? ContactAddress { get; set; } = "Số 123 Đường Nguyễn Văn Cừ, Phường An Khánh, Quận Ninh Kiều, TP. Cần Thơ";

    /// <summary>Họ và tên người đại diện theo pháp luật</summary>
    public string RepresentativeName { get; set; } = "Nguyễn Văn An";

    /// <summary>Chức vụ người đại diện (Giám đốc, Tổng giám đốc...)</summary>
    public string RepresentativePosition { get; set; } = "Giám đốc";

    /// <summary>Căn cứ thẩm quyền (theo Điều lệ công ty / Giấy ủy quyền)</summary>
    public string? RepresentativeAuthority { get; set; } = "Đại diện theo pháp luật";

    /// <summary>Số điện thoại liên hệ</summary>
    public string Phone { get; set; } = "0909 123 456";

    /// <summary>Hộp thư điện tử</summary>
    public string Email { get; set; } = "hopdong@lacasavietnam.vn";

    /// <summary>Tên ngân hàng mở tài khoản</summary>
    public string? BankName { get; set; } = "Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank)";

    /// <summary>Số tài khoản ngân hàng</summary>
    public string? BankAccount { get; set; } = "0071000999888";

    /// <summary>Tên chủ tài khoản</summary>
    public string? BankAccountHolder { get; set; } = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM";

    /// <summary>Chi nhánh ngân hàng</summary>
    public string? BankBranch { get; set; } = "Chi nhánh Cần Thơ";

    /// <summary>Hướng dẫn thanh toán mặc định</summary>
    public string? DefaultPaymentInstructions { get; set; } = "Chuyển khoản theo số tài khoản công ty hoặc tiền mặt tại quầy giao dịch";

    /// <summary>Điều kiện giao nhận hàng mặc định</summary>
    public string? DefaultDeliveryTerms { get; set; } = "Giao hàng tại chân công trình hoặc kho bên mua trong bán kính 15km";

    /// <summary>Chính sách bảo hành mặc định</summary>
    public string? DefaultWarrantyTerms { get; set; } = "Bảo hành chính hãng 10 năm đối với phần sứ, 03 năm đối với linh kiện và phụ kiện sen vòi";
}
