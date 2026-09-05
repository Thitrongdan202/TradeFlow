namespace TradeFlow.Application.Common.Interfaces;

/// <summary>
/// Trừu tượng hóa lưu trữ file (hình ảnh, tài liệu) tách biệt khỏi cơ sở dữ liệu.
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream stream, string fileName, string contentType, string category = "products", CancellationToken cancellationToken = default);
    Task<Stream?> GetFileAsync(string storageReference, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string storageReference, CancellationToken cancellationToken = default);
    string GetPublicUrl(string storageReference);
}