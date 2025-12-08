using CoreVault.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;

namespace CoreVault.Controllers;

[ApiController]
[Route("api/secure-files")]
public class SecureFileController : ControllerBase
{
    private readonly ISecureFileService _secureFileService;

    public SecureFileController(ISecureFileService secureFileService)
    {
        _secureFileService = secureFileService;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(100 * 1024 * 1024)] // Limit 100 MB na przykład
    public async Task<IActionResult> Upload(IFormFile file, [FromHeader(Name = "X-Vault-Passphrase")] string passphrase)
    {
        if (string.IsNullOrEmpty(passphrase)) return BadRequest("Passphrase header missing");
        if (file == null || file.Length == 0) return BadRequest("No file uploaded");

        // Otwieramy stream z uploadowanego pliku
        using var stream = file.OpenReadStream();
        
        var savedName = await _secureFileService.UploadEncryptedFileAsync(stream, file.FileName, passphrase);

        return Ok(new { Message = "File encrypted and saved", FileName = savedName });
    }

    [HttpGet("download/{fileName}")]
    public async Task<IActionResult> Download(string fileName, [FromHeader(Name = "X-Vault-Passphrase")] string passphrase)
    {
        if (string.IsNullOrEmpty(passphrase)) return BadRequest("Passphrase header missing");

        try
        {
            // Dostajemy strumień, który "w locie" odszyfrowuje dane
            var result = await _secureFileService.DownloadDecryptedFileAsync(fileName, passphrase);
            
            // FileStreamResult automatycznie obsłuży strumieniowanie do klienta
            return File(result.FileStream, result.ContentType, fileName);
        }
        catch (CryptographicException)
        {
            return Unauthorized("Invalid passphrase or corrupted file");
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("preview/{fileName}")]
    public async Task<IActionResult> Preview(string fileName, [FromHeader(Name = "X-Vault-Passphrase")] string passphrase)
    {
        if (string.IsNullOrEmpty(passphrase)) return BadRequest("Passphrase header missing");

        try
        {
            var result = await _secureFileService.DownloadDecryptedFileAsync(fileName, passphrase);
            
            // Ustawiamy content type na podstawie rozszerzenia pliku
            var contentType = GetContentType(fileName);
            
            return File(result.FileStream, contentType);
        }
        catch (CryptographicException)
        {
            return Unauthorized("Invalid passphrase or corrupted file");
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".mp4" => "video/mp4",
            ".mp3" => "audio/mpeg",
            _ => "application/octet-stream"
        };
    }
}
