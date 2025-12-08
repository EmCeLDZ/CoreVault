using Microsoft.AspNetCore.Mvc;
using CoreVault.Application.Interfaces;

namespace CoreVault.Security.Controllers;

/// <summary>
/// Vault controller for secure secret storage with Zero-Knowledge encryption
/// Passphrase is extracted from X-Vault-Passphrase header - never stored
/// </summary>
[ApiController]
[Route("api/security/[controller]")]
public class VaultController : ControllerBase
{
    private readonly IVaultService _vaultService;

    public VaultController(IVaultService vaultService)
    {
        _vaultService = vaultService;
    }

    /// <summary>
    /// Store a secret securely
    /// </summary>
    /// <param name="request">Secret storage request</param>
    /// <returns></returns>
    [HttpPost]
    public async Task<IActionResult> SetSecret([FromBody] SetSecretRequest request)
    {
        var passphrase = GetPassphraseFromHeader();
        if (string.IsNullOrEmpty(passphrase))
        {
            return BadRequest("X-Vault-Passphrase header is required");
        }

        try
        {
            await _vaultService.SetAsync(request.Key, request.Value, passphrase);
            return Ok(new { Message = "Secret stored successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Failed to store secret", Details = ex.Message });
        }
    }

    /// <summary>
    /// Retrieve and decrypt a secret
    /// </summary>
    /// <param name="key">Secret identifier</param>
    /// <returns>Decrypted secret value</returns>
    [HttpGet("{key}")]
    public async Task<IActionResult> GetSecret(string key)
    {
        var passphrase = GetPassphraseFromHeader();
        if (string.IsNullOrEmpty(passphrase))
        {
            return BadRequest("X-Vault-Passphrase header is required");
        }

        try
        {
            var value = await _vaultService.GetAsync(key, passphrase);
            return Ok(new { Key = key, Value = value });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { Error = $"Secret '{key}' not found" });
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Return 401 instead of 500 for authentication failures
            return Unauthorized(new { Error = "Invalid passphrase or data integrity check failed" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Failed to retrieve secret", Details = ex.Message });
        }
    }

    /// <summary>
    /// Delete a secret
    /// </summary>
    /// <param name="key">Secret identifier</param>
    /// <returns></returns>
    [HttpDelete("{key}")]
    public async Task<IActionResult> DeleteSecret(string key)
    {
        try
        {
            await _vaultService.DeleteAsync(key);
            return Ok(new { Message = "Secret deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Failed to delete secret", Details = ex.Message });
        }
    }

    /// <summary>
    /// Check if secret exists
    /// </summary>
    /// <param name="key">Secret identifier</param>
    /// <returns></returns>
    [HttpGet("{key}/exists")]
    public async Task<IActionResult> SecretExists(string key)
    {
        try
        {
            var exists = await _vaultService.ExistsAsync(key);
            return Ok(new { Key = key, Exists = exists });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Failed to check secret existence", Details = ex.Message });
        }
    }

    /// <summary>
    /// Extract passphrase from HTTP header - never stored or logged
    /// </summary>
    private string? GetPassphraseFromHeader()
    {
        return HttpContext.Request.Headers["X-Vault-Passphrase"].FirstOrDefault();
    }
}

/// <summary>
/// Request DTO for storing secrets
/// </summary>
public class SetSecretRequest
{
    /// <summary>
    /// Secret identifier (must be unique)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Plain text value to be encrypted
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
