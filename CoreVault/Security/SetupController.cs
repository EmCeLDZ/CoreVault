using Microsoft.AspNetCore.Mvc;
using CoreVault.Services;
using CoreVault.Domain.Entities;
using CoreVault.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreVault.Security.Controllers
{
    [ApiController]
    [Route("api/security/[controller]")]
    public class SetupController : ControllerBase
    {
        private readonly CoreVaultContext _context;

        public SetupController(CoreVaultContext context)
        {
            _context = context;
        }

        [HttpPost("admin-key")]
        public async Task<IActionResult> CreateAdminKey([FromBody] string adminKey)
        {
            // Check if any admin key already exists
            var existingAdmin = await _context.ApiKeys
                .AnyAsync(k => k.Role == ApiKeyRole.Admin);
            
            if (existingAdmin)
            {
                return BadRequest("Admin key already exists");
            }

            // Create new admin key
            var apiKey = new ApiKey
            {
                Key = adminKey,
                Role = ApiKeyRole.Admin,
                AllowedNamespaces = "*",
                Description = "Administrator key",
                CreatedAt = DateTime.UtcNow
            };

            _context.ApiKeys.Add(apiKey);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Admin key created successfully" });
        }

        [HttpGet("check-admin")]
        public async Task<IActionResult> CheckAdminExists()
        {
            var adminExists = await _context.ApiKeys
                .AnyAsync(k => k.Role == ApiKeyRole.Admin);
            
            return Ok(new { AdminExists = adminExists });
        }
    }
}
