using CoreVault.Application.Interfaces;
using CoreVault.Infrastructure.Logging;

namespace CoreVault.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storagePath;

    public LocalFileStorageService(IConfiguration configuration)
    {
        _storagePath = configuration["FileStorage:UploadPath"] ?? "uploads";
        
        // Upewnij się że katalog istnieje
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> StoreFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";
        var filePath = Path.Combine(_storagePath, storedFileName);

        using var destinationStream = new FileStream(filePath, FileMode.Create);
        await fileStream.CopyToAsync(destinationStream, cancellationToken);

        return storedFileName;
    }

    public Task<Stream?> GetFileStreamAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_storagePath, storedFileName);
        
        if (!File.Exists(filePath))
            return Task.FromResult<Stream?>(null);

        return Task.FromResult<Stream?>(new FileStream(filePath, FileMode.Open, FileAccess.Read));
    }

    public async Task<byte[]?> GetFileBytesAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_storagePath, storedFileName);
        
        if (!File.Exists(filePath))
            return null;

        return await File.ReadAllBytesAsync(filePath, cancellationToken);
    }

    public Task<bool> DeleteFileAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_storagePath, storedFileName);
        
        if (!File.Exists(filePath))
            return Task.FromResult(false);

        File.Delete(filePath);
        return Task.FromResult(true);
    }

    public Task<bool> FileExistsAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_storagePath, storedFileName);
        return Task.FromResult(File.Exists(filePath));
    }

    public async Task<string> ComputeFileHashAsync(Stream fileStream, CancellationToken cancellationToken = default)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(fileStream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
