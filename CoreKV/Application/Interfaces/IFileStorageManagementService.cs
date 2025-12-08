using CoreKV.Domain.Entities;
using CoreKV.Models;

namespace CoreKV.Application.Interfaces;

public interface IFileStorageManagementService
{
    Task<FileStorage> UploadFileAsync(IFormFile file, string? description = null, string @namespace = "default", ApiKey apiKey = null);
    Task<IEnumerable<FileStorage>> GetAllFilesAsync(string? @namespace = null, ApiKey apiKey = null);
    Task<FileStorage?> GetFileAsync(Guid id, ApiKey apiKey = null);
    Task<byte[]?> DownloadFileAsync(Guid id, ApiKey apiKey = null);
    Task<byte[]?> ViewFileAsync(Guid id, ApiKey apiKey = null);
    Task<bool> DeleteFileAsync(Guid id, ApiKey apiKey = null);
    Task<bool> HasNamespaceAccessAsync(string @namespace, ApiKey apiKey);
}
