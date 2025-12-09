# CoreVault Security Module - Documentation

## Overview

The CoreVault Security Module provides **Zero-Knowledge at Rest** encryption for storing secrets securely. Even database administrators cannot decrypt the stored data without the passphrase.

## Architecture

### Security Model
- **Zero-Knowledge Encryption**: Passphrase is never stored or logged
- **AES-GCM**: Provides confidentiality + integrity protection
- **PBKDF2**: Key derivation with 100,000 iterations to prevent brute force
- **Per-Secret Salting**: Unique salt for each secret prevents rainbow table attacks

### Components

#### 1. VaultController (`/api/security/vault`)
REST API endpoints for secret management:
- `POST /api/security/vault` - Store secret
- `GET /api/security/vault/{key}` - Retrieve secret  
- `DELETE /api/security/vault/{key}` - Delete secret
- `GET /api/security/vault/{key}/exists` - Check existence

#### 2. VaultService
Core encryption/decryption logic using military-grade cryptography

#### 3. VaultSecret Entity
Database entity storing encrypted data blobs only

## Usage Examples

### Store a Secret
```bash
curl -X POST http://localhost:5000/api/security/vault \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -H "X-Vault-Passphrase: your-secure-passphrase" \
  -d '{
    "key": "database-password",
    "value": "super-secret-password-123"
  }'
```

### Retrieve a Secret
```bash
curl -X GET http://localhost:5000/api/security/vault/database-password \
  -H "X-API-Key: your-api-key" \
  -H "X-Vault-Passphrase: your-secure-passphrase"
```

### Delete a Secret
```bash
curl -X DELETE http://localhost:5000/api/security/vault/database-password \
  -H "X-API-Key: your-api-key"
```

### Check if Secret Exists
```bash
curl -X GET http://localhost:5000/api/security/vault/database-password/exists \
  -H "X-API-Key: your-api-key"
```

## Testing

### Run Security Tests
```bash
dotnet test --filter "Category=Security"
```

### Run All Tests
```bash
dotnet test
```

## Security Considerations

### Passphrase Security
- **Never store passphrases** in logs, configuration, or database
- Passphrase is only transmitted in HTTP headers
- Use strong, unique passphrases for each secret type
- Consider using passphrase management tools in production

### API Security
- All vault endpoints require valid API key authentication
- Rate limiting middleware prevents brute force attacks
- Request logging excludes sensitive headers

### Encryption Details
- **Algorithm**: AES-GCM (Galois/Counter Mode)
- **Key Size**: 256-bit
- **Nonce Size**: 96-bit (recommended for GCM)
- **Salt Size**: 256-bit
- **PBKDF2 Iterations**: 100,000
- **Hash Algorithm**: HMAC-SHA256

## Error Handling

### Common HTTP Status Codes
- `400 Bad Request`: Missing X-Vault-Passphrase header
- `401 Unauthorized`: Invalid passphrase or data integrity failure
- `404 Not Found`: Secret doesn't exist
- `500 Internal Server Error`: System errors

### Security Response Details
- Wrong passphrase returns `401` (not `500`) to prevent information leakage
- Error messages don't reveal whether a key exists when passphrase is wrong
- All cryptographic failures are treated as authentication failures

## Implementation Notes

### Memory Safety
- Sensitive data is cleared from memory immediately after use
- `Array.Clear()` called on encryption keys and plaintext data
- Uses `using` statements for cryptographic objects

### Database Storage
- Only encrypted blobs are stored in database
- Each secret has unique salt, nonce, and authentication tag
- Even with full database access, secrets remain encrypted

### Performance
- PBKDF2 with 100,000 iterations provides security vs performance balance
- AES-GCM hardware acceleration available on modern CPUs
- Database indexes on secret keys for fast lookups

## Best Practices

1. **Use strong passphrases**: Minimum 12 characters with mixed case, numbers, symbols
2. **Rotate passphrases**: Regularly update passphrases for critical secrets
3. **Monitor access**: Use audit logs to track secret access patterns
4. **Backup strategy**: Implement secure backup procedures for encrypted data
5. **Environment separation**: Use different passphrases per environment (dev/staging/prod)

## Troubleshooting

### Common Issues

1. **"Invalid passphrase" errors**
   - Verify exact passphrase including case and special characters
   - Check for whitespace characters
   - Ensure passphrase wasn't changed after secret was stored

2. **"Secret not found" errors**
   - Verify exact key including case sensitivity
   - Check if secret was successfully stored
   - Use exists endpoint to confirm

3. **Performance issues**
   - Large number of secrets may impact performance
   - Consider connection pooling for database
   - Monitor memory usage during high load

### Debug Mode
Enable debug logging in appsettings.json:
```json
{
  "Logging": {
    "CoreVault": "Debug"
  }
}
```
