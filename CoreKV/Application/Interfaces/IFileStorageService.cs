namespace CoreKV.Application.Interfaces;

public interface IFileStorageService
{
    Task<string> StoreFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream?> GetFileStreamAsync(string storedFileName, CancellationToken cancellationToken = default);
    Task<byte[]?> GetFileBytesAsync(string storedFileName, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string storedFileName, CancellationToken cancellationToken = default);
    Task<bool> FileExistsAsync(string storedFileName, CancellationToken cancellationToken = default);
    Task<string> ComputeFileHashAsync(Stream fileStream, CancellationToken cancellationToken = default);
}
