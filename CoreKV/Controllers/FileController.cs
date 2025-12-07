using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CoreKV.Models;
using CoreKV.Data;
using CoreKV.Filters;
using System.Security.Cryptography;
using System.Text;

namespace CoreKV.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileController : ControllerBase
    {
        private readonly CoreKVContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly long _maxFileSize = 100 * 1024 * 1024; // 100MB
        private readonly string[] _allowedExtensions = { 
            ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp",
            ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm",
            ".mp3", ".wav", ".ogg", ".flac", ".aac",
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt",
            ".zip", ".rar", ".7z", ".tar", ".gz"
        };

        public FileController(CoreKVContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<FileStorage>>> GetAll(string? @namespace = null)
        {
            var query = _context.FileStorage.AsQueryable();
            
            // Filter by namespace if specified
            if (!string.IsNullOrEmpty(@namespace))
            {
                query = query.Where(x => x.Namespace == @namespace);
            }
            // For non-admin users, show only their namespaces
            else if (HttpContext.Items["UserRole"] is not ApiKeyRole.Admin)
            {
                var allowedNamespaces = HttpContext.Items["AllowedNamespaces"] as List<string> ?? new();
                query = query.Where(x => allowedNamespaces.Contains(x.Namespace) || 
                                       allowedNamespaces.Contains("*"));
            }

            var files = await query.ToListAsync();
            
            // Remove file data from response (only metadata)
            return files.Select(f => new FileStorage
            {
                Id = f.Id,
                OriginalFileName = f.OriginalFileName,
                ContentType = f.ContentType,
                FileSize = f.FileSize,
                FileHash = f.FileHash,
                Namespace = f.Namespace,
                Description = f.Description,
                OwnerId = f.OwnerId,
                IsEncrypted = f.IsEncrypted,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt
            }).ToList();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FileStorage>> GetById(Guid id)
        {
            var file = await _context.FileStorage.FindAsync(id);
            
            if (file == null)
            {
                return NotFound($"File with ID '{id}' not found");
            }

            // Check namespace access
            if (HttpContext.Items["UserRole"] is not ApiKeyRole.Admin)
            {
                var allowedNamespaces = HttpContext.Items["AllowedNamespaces"] as List<string> ?? new();
                if (!allowedNamespaces.Contains(file.Namespace) && !allowedNamespaces.Contains("*"))
                {
                    return Forbid("Access to this file is forbidden");
                }
            }

            return Ok(file);
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> Download(Guid id)
        {
            var file = await _context.FileStorage.FindAsync(id);
            
            if (file == null)
            {
                return NotFound($"File with ID '{id}' not found");
            }

            // Check namespace access
            if (HttpContext.Items["UserRole"] is not ApiKeyRole.Admin)
            {
                var allowedNamespaces = HttpContext.Items["AllowedNamespaces"] as List<string> ?? new();
                if (!allowedNamespaces.Contains(file.Namespace) && !allowedNamespaces.Contains("*"))
                {
                    return Forbid("Access to this file is forbidden");
                }
            }

            var filePath = Path.Combine(_environment.ContentRootPath, "uploads", file.StoredFileName);
            
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Physical file not found");
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            
            return File(fileBytes, file.ContentType, file.OriginalFileName);
        }

        [HttpPost("upload")]
        [RequireNamespace]
        [RequestSizeLimit(100 * 1024 * 1024)] // 100MB limit
        public async Task<ActionResult<FileStorage>> Upload(IFormFile file, string @namespace = "default", string? description = null)
        {
            // Check write permissions
            if (HttpContext.Items["UserRole"] is not (ApiKeyRole.Admin or ApiKeyRole.ReadWrite))
            {
                return Forbid("Write access required");
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided");
            }

            // Validate file size
            if (file.Length > _maxFileSize)
            {
                return BadRequest($"File size exceeds maximum allowed size of {_maxFileSize / (1024 * 1024)}MB");
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                return BadRequest("File type not allowed");
            }

            // Generate unique filename
            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads");
            
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            var filePath = Path.Combine(uploadsPath, storedFileName);

            // Calculate file hash
            string fileHash;
            using (var sha256 = SHA256.Create())
            {
                using (var stream = file.OpenReadStream())
                {
                    var hashBytes = await sha256.ComputeHashAsync(stream);
                    fileHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
                }
            }

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Save to database
            var fileStorage = new FileStorage
            {
                OriginalFileName = file.FileName,
                StoredFileName = storedFileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FileHash = fileHash,
                Namespace = @namespace,
                Description = description,
                OwnerId = HttpContext.Items["UserId"]?.ToString(),
                IsEncrypted = false
            };

            _context.FileStorage.Add(fileStorage);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = fileStorage.Id }, fileStorage);
        }

        [HttpDelete("{id}")]
        [RequireNamespace]
        public async Task<IActionResult> Delete(Guid id)
        {
            // Check write permissions
            if (HttpContext.Items["UserRole"] is not (ApiKeyRole.Admin or ApiKeyRole.ReadWrite))
            {
                return Forbid("Write access required");
            }

            var file = await _context.FileStorage.FindAsync(id);
            
            if (file == null)
            {
                return NotFound($"File with ID '{id}' not found");
            }

            // Check namespace access
            if (HttpContext.Items["UserRole"] is not ApiKeyRole.Admin)
            {
                var allowedNamespaces = HttpContext.Items["AllowedNamespaces"] as List<string> ?? new();
                if (!allowedNamespaces.Contains(file.Namespace) && !allowedNamespaces.Contains("*"))
                {
                    return Forbid("Access to this file is forbidden");
                }
            }

            // Delete physical file
            var filePath = Path.Combine(_environment.ContentRootPath, "uploads", file.StoredFileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            // Delete from database
            _context.FileStorage.Remove(file);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<FileStorage>>> Search(string? query = null, string? @namespace = null)
        {
            var filesQuery = _context.FileStorage.AsQueryable();

            // Filter by namespace
            if (!string.IsNullOrEmpty(@namespace))
            {
                filesQuery = filesQuery.Where(x => x.Namespace == @namespace);
            }
            else if (HttpContext.Items["UserRole"] is not ApiKeyRole.Admin)
            {
                var allowedNamespaces = HttpContext.Items["AllowedNamespaces"] as List<string> ?? new();
                filesQuery = filesQuery.Where(x => allowedNamespaces.Contains(x.Namespace) || 
                                                  allowedNamespaces.Contains("*"));
            }

            // Search by filename or description
            if (!string.IsNullOrEmpty(query))
            {
                filesQuery = filesQuery.Where(x => x.OriginalFileName.Contains(query) || 
                                                  (x.Description != null && x.Description.Contains(query)));
            }

            var files = await filesQuery.ToListAsync();
            
            return files.Select(f => new FileStorage
            {
                Id = f.Id,
                OriginalFileName = f.OriginalFileName,
                ContentType = f.ContentType,
                FileSize = f.FileSize,
                FileHash = f.FileHash,
                Namespace = f.Namespace,
                Description = f.Description,
                OwnerId = f.OwnerId,
                IsEncrypted = f.IsEncrypted,
                CreatedAt = f.CreatedAt,
                UpdatedAt = f.UpdatedAt
            }).ToList();
        }
    }
}
