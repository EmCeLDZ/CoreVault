using System.ComponentModel.DataAnnotations;

namespace CoreVault.Domain.Entities;

/// <summary>
/// Entity for storing encrypted secrets in the vault.
/// Implements Zero-Knowledge at Rest - database only stores encrypted blobs.
/// Even database admins cannot decrypt the data without the passphrase.
/// </summary>
public class VaultSecret
{
    [Key]
    [MaxLength(255)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Encrypted data blob (AES-GCM ciphertext)
    /// This is what gets stored in the database - completely unreadable without passphrase
    /// </summary>
    [Required]
    public byte[] Ciphertext { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Nonce/IV used for AES-GCM encryption (12 bytes recommended for GCM)
    /// Must be unique per encryption operation with the same key
    /// </summary>
    [Required]
    public byte[] Nonce { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Salt used for PBKDF2 key derivation (16+ bytes recommended)
    /// Unique per secret to prevent rainbow table attacks
    /// </summary>
    [Required]
    public byte[] Salt { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Authentication tag from AES-GCM (automatically generated, ensures integrity)
    /// In .NET, this is typically included in the ciphertext array, but storing separately for clarity
    /// </summary>
    [Required]
    public byte[] Tag { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Metadata timestamps
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
