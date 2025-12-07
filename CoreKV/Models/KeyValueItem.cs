using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreKV.Models
{
    public class KeyValueItem
    {
        [Key]
        [Column(Order = 1)]
        public string Namespace { get; set; } = "default";
        
        [Key]
        [Column(Order = 2)]
        public string Key { get; set; } = string.Empty;
        
        public string Value { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; }
        public string? OwnerId { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
