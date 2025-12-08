using Microsoft.AspNetCore.Mvc;
using CoreKV.Models;
using CoreKV.Domain.Entities;
using CoreKV.Application.Services;
using CoreKV.Application.Interfaces;
using CoreKV.Filters;
using ApiKey = CoreKV.Domain.Entities.ApiKey;

[ApiController]
[Route("api/[controller]")]
[TypeFilter(typeof(ValidationFilter))]
public class FileController : ControllerBase
{
    private readonly IFileStorageManagementService _fileStorageService;

    public FileController(IFileStorageManagementService fileStorageService)
    {
        _fileStorageService = fileStorageService;
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
            return Ok(new { message = "File controller works!", timestamp = DateTime.UtcNow, role = apiKey.Role });
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
            var fileStorage = await _fileStorageService.UploadFileAsync(file, description, @namespace, apiKey);
            return CreatedAtAction(nameof(GetFile), new { id = fileStorage.Id }, fileStorage);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
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
            var files = await _fileStorageService.GetAllFilesAsync(@namespace, apiKey);
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
            var file = await _fileStorageService.GetFileAsync(id, apiKey);
            
            if (file == null)
                return NotFound();
            
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
            var file = await _fileStorageService.GetFileAsync(id, apiKey);
            
            if (file == null)
                return NotFound();
            
            var fileBytes = await _fileStorageService.DownloadFileAsync(id, apiKey);
            
            if (fileBytes == null)
                return NotFound("Plik nie istnieje na dysku");
            
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
            var file = await _fileStorageService.GetFileAsync(id, apiKey);
            
            if (file == null)
                return NotFound();
            
            var fileBytes = await _fileStorageService.ViewFileAsync(id, apiKey);
            
            if (fileBytes == null)
                return NotFound("Plik nie istnieje na dysku");
            
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
            var success = await _fileStorageService.DeleteFileAsync(id, apiKey);
            
            if (!success)
                return NotFound();
            
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ex.Message);
        }
    }
}
