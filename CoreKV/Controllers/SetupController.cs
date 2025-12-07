using Microsoft.AspNetCore.Mvc;
using CoreKV.Services;
using CoreKV.Domain.Entities;
using CoreKV.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreKV.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SetupController : ControllerBase
    {
        private readonly CoreKVContext _context;

        public SetupController(CoreKVContext context)
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
