using System.ComponentModel.DataAnnotations;

namespace CoreKV.Application.DTOs
{
    public class UpdateKeyValueRequest
    {
        [Required]
        public string Value { get; set; } = string.Empty;
    }
}
