using Microsoft.AspNetCore.Mvc;
using CoreKV.Models;
using CoreKV.Domain.Entities;
using CoreKV.Data;
using System.Security.Cryptography;
using System.Text;
using CoreKV.Filters;
using ApiKey = CoreKV.Domain.Entities.ApiKey;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class FileController : ControllerBase
{
    private readonly CoreKVContext _context;
    private readonly string _storagePath;

    public FileController(CoreKVContext context, IConfiguration configuration)
    {
        _context = context;
        _storagePath = configuration["FileStorage:UploadPath"] ?? "uploads";
        
        // Upewnij się że katalog istnieje
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    private ApiKey GetCurrentApiKey()
    {
        var apiKey = HttpContext.Items["ApiKey"] as ApiKey;
        if (apiKey == null)
            throw new UnauthorizedAccessException("API key required");
        
        return apiKey;
    }

    [HttpGet("test")]
    public IActionResult Test()
    {
        try
        {
            var apiKey = GetCurrentApiKey();
            return Ok(new { message = "File controller works!", timestamp = DateTime.UtcNow, apiKey = apiKey.Key });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("public")]
    public IActionResult PublicTest()
    {
        return Ok(new { message = "Public endpoint works!", timestamp = DateTime.UtcNow });
    }

    [HttpPost("upload")]
    public async Task<ActionResult<FileStorage>> UploadFile(IFormFile file, [FromForm] string? description = null, [FromForm] string @namespace = "default")
    {
        try
        {
            var apiKey = GetCurrentApiKey();
            
            if (file == null || file.Length == 0)
                return BadRequest("Plik jest wymagany");

            // Sprawdzenie rozmiaru pliku (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
                return BadRequest("Plik jest za duży (maksymalnie 10MB)");

            // Generuj unikalną nazwę pliku
            var fileHash = await ComputeFileHashAsync(file);
            var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(_storagePath, storedFileName);

            // Zapisz plik na dysku
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

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
                OwnerId = apiKey.Key,
                IsEncrypted = false
            };

            _context.FileStorage.Add(fileStorage);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetFile), new { id = fileStorage.Id }, fileStorage);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Wystąpił błąd: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FileStorage>>> GetAllFiles(string? @namespace = null)
    {
        try
        {
            var apiKey = GetCurrentApiKey();
            
            var query = _context.FileStorage.AsQueryable();
            
            if (!string.IsNullOrEmpty(@namespace))
            {
                query = query.Where(f => f.Namespace == @namespace);
            }
            
            // Filtruj według uprawnień API key
            if (apiKey.AllowedNamespaces != "*")
            {
                var allowedNamespaces = apiKey.AllowedNamespaces.Split(',');
                query = query.Where(f => allowedNamespaces.Contains(f.Namespace));
            }
            
            var files = await query.OrderByDescending(f => f.CreatedAt).ToListAsync();
            
            return Ok(files);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FileStorage>> GetFile(Guid id)
    {
        try
        {
            var apiKey = GetCurrentApiKey();
            var file = await _context.FileStorage.FindAsync(id);
            
            if (file == null)
                return NotFound();
            
            // Sprawdź uprawnienia
            if (apiKey.AllowedNamespaces != "*" && !apiKey.AllowedNamespaces.Split(',').Contains(file.Namespace))
                return Unauthorized("Brak dostępu do tego pliku");
            
            return Ok(file);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(Guid id)
    {
        try
        {
            var apiKey = GetCurrentApiKey();
            var file = await _context.FileStorage.FindAsync(id);
            
            if (file == null)
                return NotFound();
            
            // Sprawdź uprawnienia
            if (apiKey.AllowedNamespaces != "*" && !apiKey.AllowedNamespaces.Split(',').Contains(file.Namespace))
                return Unauthorized("Brak dostępu do tego pliku");
            
            var filePath = Path.Combine(_storagePath, file.StoredFileName);
            
            if (!System.IO.File.Exists(filePath))
                return NotFound("Plik nie istnieje na dysku");
            
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            
            return File(fileBytes, file.ContentType, file.OriginalFileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpGet("{id}/view")]
    public async Task<IActionResult> ViewFile(Guid id)
    {
        try
        {
            var apiKey = GetCurrentApiKey();
            var file = await _context.FileStorage.FindAsync(id);
            
            if (file == null)
                return NotFound();
            
            // Sprawdź uprawnienia
            if (apiKey.AllowedNamespaces != "*" && !apiKey.AllowedNamespaces.Split(',').Contains(file.Namespace))
                return Unauthorized("Brak dostępu do tego pliku");
            
            var filePath = Path.Combine(_storagePath, file.StoredFileName);
            
            if (!System.IO.File.Exists(filePath))
                return NotFound("Plik nie istnieje na dysku");
            
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            
            return File(fileBytes, file.ContentType);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [RequireNamespace]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        try
        {
            var apiKey = GetCurrentApiKey();
            var file = await _context.FileStorage.FindAsync(id);
            
            if (file == null)
                return NotFound();
            
            // Sprawdź uprawnienia
            if (apiKey.AllowedNamespaces != "*" && !apiKey.AllowedNamespaces.Split(',').Contains(file.Namespace))
                return Unauthorized("Brak dostępu do tego pliku");
            
            // Usuń plik z dysku
            var filePath = Path.Combine(_storagePath, file.StoredFileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            
            // Usuń rekord z bazy danych
            _context.FileStorage.Remove(file);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }

    private async Task<string> ComputeFileHashAsync(IFormFile file)
    {
        using var sha256 = SHA256.Create();
        using var stream = file.OpenReadStream();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
