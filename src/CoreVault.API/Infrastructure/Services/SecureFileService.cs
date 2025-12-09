using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using CoreVault.Infrastructure.Application.Interfaces;

namespace CoreVault.Infrastructure.Services;

public interface ISecureFileService
{
    Task<string> UploadEncryptedFileAsync(Stream inputStream, string fileName, string passphrase);
    Task<(Stream FileStream, string ContentType)> DownloadDecryptedFileAsync(string fileName, string passphrase);
}

public class SecureFileService : ISecureFileService
{
    private readonly string _storagePath;
    private const int KeySize = 32; // 256 bitów
    private const int IvSize = 16;  // 128 bitów
    private const int SaltSize = 16;

    public SecureFileService(IConfiguration configuration)
    {
        // Ustaw ścieżkę do bezpiecznego folderu (np. "SecureStorage")
        _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "SecureStorage");
        if (!Directory.Exists(_storagePath)) Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> UploadEncryptedFileAsync(Stream inputStream, string fileName, string passphrase)
    {
        var safeFileName = Path.GetFileName(fileName); // Zabezpieczenie przed "hackowaniem" ścieżki
        var filePath = Path.Combine(_storagePath, safeFileName);

        // 1. Generujemy losowy Salt dla tego pliku
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // 2. Wyprowadzamy klucz z hasła (KDF)
        using var kdf = new Rfc2898DeriveBytes(passphrase, salt, 100_000, HashAlgorithmName.SHA256);
        var key = kdf.GetBytes(KeySize);
        
        // 3. Tworzymy AES
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV(); // Generujemy losowy IV (Initialization Vector)

        // 4. Piszemy na dysk
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            // A. Zapisujemy SALT na początku pliku (jawnie, potrzebny do odszyfrowania)
            await fileStream.WriteAsync(salt);
            
            // B. Zapisujemy IV zaraz po Salcie (jawnie, potrzebny do AES)
            await fileStream.WriteAsync(aes.IV);

            // C. Tworzymy CryptoStream - to jest ta "maszynka szyfrująca"
            using (var encryptor = aes.CreateEncryptor())
            using (var cryptoStream = new CryptoStream(fileStream, encryptor, CryptoStreamMode.Write))
            {
                // D. Przepisujemy dane: Input -> CryptoStream -> FileStream
                await inputStream.CopyToAsync(cryptoStream);
            }
        }

        return safeFileName;
    }

    public async Task<(Stream FileStream, string ContentType)> DownloadDecryptedFileAsync(string fileName, string passphrase)
    {
        var filePath = Path.Combine(_storagePath, Path.GetFileName(fileName));
        if (!File.Exists(filePath)) throw new FileNotFoundException("File not found");

        // Otwieramy plik z dysku
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);

        try 
        {
            // 1. Odczytujemy SALT (pierwsze 16 bajtów)
            var salt = new byte[SaltSize];
            await fileStream.ReadExactlyAsync(salt);

            // 2. Odczytujemy IV (kolejne 16 bajtów)
            var iv = new byte[IvSize];
            await fileStream.ReadExactlyAsync(iv);

            // 3. Odtwarzamy klucz z hasła
            using var kdf = new Rfc2898DeriveBytes(passphrase, salt, 100_000, HashAlgorithmName.SHA256);
            var key = kdf.GetBytes(KeySize);

            // 4. Konfigurujemy AES
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            // 5. Zwracamy CryptoStream w trybie READ. 
            // UWAGA: Nie używamy 'using' tutaj, bo stream musi być otwarty dla kontrolera!
            var decryptor = aes.CreateDecryptor();
            var cryptoStream = new CryptoStream(fileStream, decryptor, CryptoStreamMode.Read);
            
            return (cryptoStream, "application/octet-stream");
        }
        catch
        {
            // Jeśli coś pójdzie nie tak, zamykamy stream
            await fileStream.DisposeAsync();
            throw;
        }
    }
}
