using System;
using System.Threading.Tasks;

namespace TradeFlow.Application.Common.Interfaces;

/// <summary>
/// Dịch vụ quản lý trạng thái màn hình tải toàn cục (Global Loading Screen) cho các tác vụ trọng yếu.
/// </summary>
public interface ILoadingService
{
    /// <summary>Trạng thái màn hình tải đang hiển thị hay không</summary>
    bool IsLoading { get; }

    /// <summary>Văn bản thông báo tiến trình hiển thị bên dưới (mặc định "Đang tải...")</summary>
    string LoadingText { get; }

    /// <summary>Sự kiện phát ra khi trạng thái tải thay đổi để UI lắng nghe</summary>
    event Action? OnChange;

    /// <summary>Kích hoạt hiển thị màn hình tải toàn cục</summary>
    void Show(string? text = null);

    /// <summary>Tắt màn hình tải toàn cục</summary>
    void Hide();

    /// <summary>Kích hoạt hiển thị màn hình tải toàn cục (bất đồng bộ)</summary>
    Task ShowAsync(string? text = null);

    /// <summary>Tắt màn hình tải toàn cục (bất đồng bộ)</summary>
    Task HideAsync();
}
