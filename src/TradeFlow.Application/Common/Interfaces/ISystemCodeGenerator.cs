namespace TradeFlow.Application.Common.Interfaces;

/// <summary>
/// Dịch vụ sinh mã định danh hệ thống (SP000001, KH000001...) tuần tự, an toàn đa luồng.
/// </summary>
public interface ISystemCodeGenerator
{
    /// <summary>Sinh mã tiếp theo cho đối tượng và tăng sequence trong CSDL</summary>
    Task<string> GenerateCodeAsync(string sequenceKey, CancellationToken cancellationToken = default);

    /// <summary>Xem trước mã tiếp theo mà không làm tăng sequence</summary>
    Task<string> PeekNextCodeAsync(string sequenceKey, CancellationToken cancellationToken = default);
}