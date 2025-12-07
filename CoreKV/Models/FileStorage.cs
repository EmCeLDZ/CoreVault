using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreKV.Models
{
    public class FileStorage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(255)]
        public string StoredFileName { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;
        
        [Required]
        public long FileSize { get; set; }
        
        [Required]
        [StringLength(50)]
        public string FileHash { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Namespace { get; set; } = "default";
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        public string? OwnerId { get; set; }
        
        public bool IsEncrypted { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum FileType
    {
        Image = 1,
        Video = 2,
        Audio = 3,
        Document = 4,
        Archive = 5,
        Other = 6
    }
}
