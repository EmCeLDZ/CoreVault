using System.ComponentModel.DataAnnotations;

namespace CoreVault.API.Modules.KeyValue.Entities
{
    public class KeyValueItem
    {
        [Required]
        public string Namespace { get; set; } = string.Empty;
        [Required]
        public string Key { get; set; } = string.Empty;
        [Required]
        public string Value { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
