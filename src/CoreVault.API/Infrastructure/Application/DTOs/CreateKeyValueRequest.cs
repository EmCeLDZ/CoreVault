using System.ComponentModel.DataAnnotations;

namespace CoreVault.Infrastructure.Application.DTOs
{
    public class CreateKeyValueRequest
    {
        [Required]
        public string Namespace { get; set; } = string.Empty;
        
        [Required]
        public string Key { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;
    }
}
