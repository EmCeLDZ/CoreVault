using System.ComponentModel.DataAnnotations;

namespace CoreVault.Domain.Entities
{
    public class KeyValueItem
    {
        [Key]
        public int Id { get; set; }
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
