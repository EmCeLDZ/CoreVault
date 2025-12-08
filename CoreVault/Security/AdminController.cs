using Microsoft.AspNetCore.Mvc;
using CoreVault.Services;
using CoreVault.Models;
using Microsoft.Extensions.Configuration;

namespace CoreVault.Security.Controllers
{
    [ApiController]
    [Route("api/security/[controller]")]
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
                Message = "Add this key to database via SetupController",
                Warning = "Store this key securely - it won't be shown again"
            });
        }
    }
}
