using System.ComponentModel.DataAnnotations;

namespace CoreVault.Infrastructure.Application.DTOs
{
    public class UpdateKeyValueRequest
    {
        [Required]
        public string Value { get; set; } = string.Empty;
    }
}
