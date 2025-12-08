using System.ComponentModel.DataAnnotations;

namespace CoreVault.Domain.Entities
{
    public class ApiKey
    {
        [Key]
        public string Key { get; set; } = string.Empty;
        public ApiKeyRole Role { get; set; }
        public string AllowedNamespaces { get; set; } = ""; // e.g. "public,user123,private"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Description { get; set; } = string.Empty;
    }

    public enum ApiKeyRole
    {
        ReadOnly = 1,
        ReadWrite = 2,
        Admin = 3
    }
}
