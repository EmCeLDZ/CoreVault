using CoreVault.API.Modules.Security.Entities;

namespace CoreVault.Infrastructure.Application.Interfaces;

/// <summary>
/// Service interface for vault operations with Zero-Knowledge security
/// </summary>
public interface IVaultService
{
    /// <summary>
    /// Store a secret securely with Zero-Knowledge encryption
    /// </summary>
    /// <param name="key">Secret identifier</param>
    /// <param name="value">Plain text value to encrypt</param>
    /// <param name="passphrase">Client-provided passphrase (never stored)</param>
    /// <returns></returns>
    Task SetAsync(string key, string value, string passphrase);

    /// <summary>
    /// Retrieve and decrypt a secret
    /// </summary>
    /// <param name="key">Secret identifier</param>
    /// <param name="passphrase">Client-provided passphrase (must match original)</param>
    /// <returns>Decrypted plain text value</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown when passphrase is incorrect or data is tampered</exception>
    Task<string> GetAsync(string key, string passphrase);

    /// <summary>
    /// Delete a secret
    /// </summary>
    /// <param name="key">Secret identifier</param>
    Task DeleteAsync(string key);

    /// <summary>
    /// Check if secret exists
    /// </summary>
    /// <param name="key">Secret identifier</param>
    Task<bool> ExistsAsync(string key);
}
