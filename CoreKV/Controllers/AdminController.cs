using Microsoft.AspNetCore.Mvc;
using CoreKV.Services;
using CoreKV.Models;
using Microsoft.Extensions.Configuration;

namespace CoreKV.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        
        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("generate-key")]
        public IActionResult GenerateApiKey()
        {
            var newKey = ApiKeyGenerator.GenerateSecureApiKey();
            
            return Ok(new 
            { 
                Key = newKey,
                Role = "ReadWrite",
                Message = "Add this key to appsettings.json under ApiKeys:ReadWrite",
                Warning = "Store this key securely - it won't be shown again"
            });
        }
        
        [HttpGet("current-config")]
        public IActionResult GetCurrentConfig()
        {
            // Tylko do dev - nie pokazuje rzeczywistych keys w production!
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                return Ok(new 
                { 
                    Message = "Current API Keys configured",
                    ReadOnlyConfigured = !string.IsNullOrEmpty(_configuration["ApiKeys:ReadOnly"]),
                    ReadWriteConfigured = !string.IsNullOrEmpty(_configuration["ApiKeys:ReadWrite"])
                });
            }
            
            return Forbid("Not available in production");
        }
    }
}
