using System.Security.Cryptography;
using System.Text;
using CoreVault.Application.Interfaces;
using CoreVault.Domain.Entities;
using CoreVault.Domain.Interfaces;

namespace CoreVault.Application.Services;

/// <summary>
/// Military-grade vault service implementing Zero-Knowledge at Rest security
/// Uses AES-GCM for confidentiality + integrity, PBKDF2 for key derivation
/// </summary>
public class VaultService : IVaultService
{
    private readonly IVaultRepository _repository;
    private const int KeyDerivationIterations = 100_000; // High iteration count for PBKDF2
    private const int SaltSize = 32; // 256-bit salt for PBKDF2
    private const int NonceSize = 12; // 96-bit nonce recommended for AES-GCM
    private const int TagSize = 16; // 128-bit authentication tag for AES-GCM
    private const int DerivedKeySize = 32; // 256-bit AES key

    public VaultService(IVaultRepository repository)
    {
        _repository = repository;
    }

    public async Task SetAsync(string key, string value, string passphrase)
    {
        // Generate unique salt for this secret
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        
        // Derive encryption key from passphrase using PBKDF2
        var encryptionKey = DeriveKey(passphrase, salt);
        
        // Generate unique nonce for this encryption
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        
        // Encrypt using AES-GCM (provides confidentiality + integrity)
        var plaintextBytes = Encoding.UTF8.GetBytes(value);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];
        
        using var aes = new AesGcm(encryptionKey, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        
        // Create and store the encrypted secret
        var secret = new VaultSecret
        {
            Key = key,
            Ciphertext = ciphertext,
            Nonce = nonce,
            Salt = salt,
            Tag = tag,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        var existingSecret = await _repository.GetAsync(key);
        if (existingSecret != null)
        {
            // Update existing secret
            existingSecret.Ciphertext = ciphertext;
            existingSecret.Nonce = nonce;
            existingSecret.Salt = salt;
            existingSecret.Tag = tag;
            existingSecret.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(existingSecret);
        }
        else
        {
            // Add new secret
            await _repository.AddAsync(secret);
        }
        
        // Clear sensitive data from memory immediately
        Array.Clear(encryptionKey, 0, encryptionKey.Length);
    }

    public async Task<string> GetAsync(string key, string passphrase)
    {
        var secret = await _repository.GetAsync(key);
        if (secret == null)
        {
            throw new KeyNotFoundException($"Secret with key '{key}' not found.");
        }
        
        // Derive the same encryption key using the stored salt and provided passphrase
        var encryptionKey = DeriveKey(passphrase, secret.Salt);
        
        try
        {
            // Decrypt using AES-GCM (will throw CryptographicException if tag doesn't match)
            var plaintext = new byte[secret.Ciphertext.Length];
            
            using var aes = new AesGcm(encryptionKey, TagSize);
            aes.Decrypt(secret.Nonce, secret.Ciphertext, secret.Tag, plaintext);
            
            // Clear sensitive data from memory immediately
            Array.Clear(encryptionKey, 0, encryptionKey.Length);
            
            return Encoding.UTF8.GetString(plaintext);
        }
        catch (CryptographicException)
        {
            // Clear sensitive data from memory even on failure
            Array.Clear(encryptionKey, 0, encryptionKey.Length);
            
            // Re-throw to indicate authentication failure (wrong passphrase or tampered data)
            throw new CryptographicException("Failed to decrypt secret. Invalid passphrase or data integrity check failed.");
        }
    }

    public async Task DeleteAsync(string key)
    {
        await _repository.DeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _repository.ExistsAsync(key);
    }

    /// <summary>
    /// Derives a cryptographic key from passphrase using PBKDF2 with HMAC-SHA256
    /// This prevents using raw passphrase as encryption key and provides protection against rainbow tables
    /// </summary>
    private static byte[] DeriveKey(string passphrase, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password: passphrase,
            salt: salt,
            iterations: KeyDerivationIterations,
            hashAlgorithm: HashAlgorithmName.SHA256
        );
        
        return pbkdf2.GetBytes(DerivedKeySize);
    }
}
