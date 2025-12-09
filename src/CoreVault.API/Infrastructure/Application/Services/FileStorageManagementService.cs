using CoreVault.Infrastructure.Application.Interfaces;
using CoreVault.Infrastructure;
using CoreVault.Models;
using Microsoft.EntityFrameworkCore;
using CoreVault.Infrastructure.Services;

namespace CoreVault.Infrastructure.Application.Services;

public class FileStorageManagementService : IFileStorageManagementService
{
    private readonly CoreVaultContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FileStorageManagementService> _logger;

    public FileStorageManagementService(
        CoreVaultContext context,
        IFileStorageService fileStorageService,
        ILogger<FileStorageManagementService> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<FileStorage> UploadFileAsync(IFormFile file, string? description = null, string @namespace = "default", ApiKey? apiKey = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Plik jest wymagany");

        // Sprawdzenie rozmiaru pliku (max 10MB)
        if (file.Length > 10 * 1024 * 1024)
            throw new ArgumentException("Plik jest za duży (maksymalnie 10MB)");

        // Sprawdź uprawnień do namespace
        if (!await HasNamespaceAccessAsync(@namespace, apiKey))
            throw new UnauthorizedAccessException($"Brak dostępu do namespace '{@namespace}'");

        // Generuj hash i zapisz plik
        var fileHash = await _fileStorageService.ComputeFileHashAsync(file.OpenReadStream());
        var storedFileName = await _fileStorageService.StoreFileAsync(file.OpenReadStream(), file.FileName, file.ContentType);

        // Zapisz informacje o pliku w bazie danych
        var fileStorage = new FileStorage
        {
            OriginalFileName = file.FileName,
            StoredFileName = storedFileName,
            ContentType = file.ContentType,
            FileSize = file.Length,
            FileHash = fileHash,
            Namespace = @namespace,
            Description = description,
            OwnerId = apiKey?.Key,
            IsEncrypted = false
        };

        _context.FileStorage.Add(fileStorage);
        await _context.SaveChangesAsync();

        _logger.LogInformation("File uploaded successfully: {FileName} by {ApiKey}", file.FileName, apiKey?.Key);
        return fileStorage;
    }

    public async Task<IEnumerable<FileStorage>> GetAllFilesAsync(string? @namespace = null, ApiKey? apiKey = null)
    {
        var query = _context.FileStorage.AsQueryable();
        
        if (!string.IsNullOrEmpty(@namespace))
        {
            query = query.Where(f => f.Namespace == @namespace);
        }
        
        if (apiKey?.AllowedNamespaces != "*")
        {
            var allowedNamespaces = apiKey!.AllowedNamespaces.Split(',');
            query = query.Where(f => allowedNamespaces.Contains(f.Namespace!));
        }
        
        var files = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
        return files;
    }

    public async Task<FileStorage?> GetFileAsync(Guid id, ApiKey? apiKey = null)
    {
        var file = await _context.FileStorage.FindAsync(id);
        
        if (file == null)
            return null!;
        
        // Sprawdź uprawnienia
        if (!await HasNamespaceAccessAsync(file.Namespace, apiKey))
            throw new UnauthorizedAccessException("Brak dostępu do tego pliku");
        
        return file;
    }

    public async Task<byte[]?> DownloadFileAsync(Guid id, ApiKey? apiKey = null)
    {
        var file = await GetFileAsync(id, apiKey);
        
        if (file == null)
            return null!;
        
        return await _fileStorageService.GetFileBytesAsync(file.StoredFileName);
    }

    public async Task<byte[]?> ViewFileAsync(Guid id, ApiKey? apiKey = null)
    {
        var file = await GetFileAsync(id, apiKey);
        
        if (file == null)
            return null!;
        
        return await _fileStorageService.GetFileBytesAsync(file.StoredFileName);
    }

    public async Task<bool> DeleteFileAsync(Guid id, ApiKey? apiKey = null)
    {
        var file = await GetFileAsync(id, apiKey);
        
        if (file == null)
            return false;
        
        // Usuń plik z dysku
        await _fileStorageService.DeleteFileAsync(file.StoredFileName);
        
        // Usuń rekord z bazy danych
        _context.FileStorage.Remove(file);
        await _context.SaveChangesAsync();

        _logger.LogInformation("File deleted successfully: {FileId}", id);
        return true;
    }

    public Task<bool> HasNamespaceAccessAsync(string @namespace, ApiKey? apiKey)
    {
        if (apiKey == null)
            return Task.FromResult(false);
        
        if (apiKey.AllowedNamespaces == "*")
            return Task.FromResult(true);
        
        var allowedNamespaces = apiKey.AllowedNamespaces.Split(',');
        return Task.FromResult(allowedNamespaces.Contains(@namespace));
    }
}
