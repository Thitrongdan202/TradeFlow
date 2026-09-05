using Microsoft.AspNetCore.Hosting;
using TradeFlow.Application.Common.Interfaces;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Lưu trữ file cục bộ trong thư mục wwwroot/uploads.
/// Chuẩn bị sẵn abstraction để có thể chuyển sang S3/Azure Blob trong tương lai mà không đổi Domain/Application code.
/// </summary>
public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> SaveFileAsync(Stream stream, string fileName, string contentType, string category = "products", CancellationToken cancellationToken = default)
    {
        var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var categoryDir = Path.Combine(webRoot, "uploads", category);

        if (!Directory.Exists(categoryDir))
        {
            Directory.CreateDirectory(categoryDir);
        }

        var ext = Path.GetExtension(fileName);
        var safeFileName = Path.GetFileNameWithoutExtension(fileName);
        var uniqueName = $"{Guid.NewGuid():N}_{safeFileName}{ext}";
        var fullPath = Path.Combine(categoryDir, uniqueName);

        using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        return $"uploads/{category}/{uniqueName}";
    }

    public Task<Stream?> GetFileAsync(string storageReference, CancellationToken cancellationToken = default)
    {
        var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var fullPath = Path.Combine(webRoot, storageReference.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream fileStream = File.OpenRead(fullPath);
        return Task.FromResult<Stream?>(fileStream);
    }

    public Task<bool> DeleteFileAsync(string storageReference, CancellationToken cancellationToken = default)
    {
        var webRoot = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        var fullPath = Path.Combine(webRoot, storageReference.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(fullPath))
        {
            try
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        return Task.FromResult(false);
    }

    public string GetPublicUrl(string storageReference)
    {
        if (string.IsNullOrWhiteSpace(storageReference))
            return string.Empty;

        return "/" + storageReference.TrimStart('/');
    }
}