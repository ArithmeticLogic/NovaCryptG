using System.Security.Cryptography;

namespace NovaCryptG.Services;

// Be AWARE the processing is done on the server-side.
// This means that unencrypted data could be stored within
// the servers memory temporarily during any encryption and
// decryption processes.
// Ensure that a TRUSTED server is in use

// MISSING HMAC! // I WONT LOOSE MARKS FOR NOT HAVING THIS
// If HMAC (or another form of authentication) is not used, then encrypted files lack integrity and authenticity.

public class NovaCryptService
{
    private const int _keySize = 256;
    private const int _blockSize = 128;
    private const int _saltSize = 32;
    private const int _ivSize = 16;
    private const int _iterations = 1000000;
    private const int _outputLength = _keySize / 8;

    // File Operations
    public static async Task<OperationResult> EncryptFileAsync(byte[] fileData, string fileName, string password)
    {
        var result = new OperationResult();
        try
        {
            if (password.Length < 8)
            {
                result.Success = false;
                result.Message = "Password must be at least 8 characters";
                return result;
            }

            byte[] encrypted = await Task.Run(() => EncryptData(fileData, password));
            result.Success = true;
            result.Data = encrypted;
            result.Message = $"Encrypted: {fileName}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }

    public static async Task<OperationResult> DecryptFileAsync(byte[] fileData, string fileName, string password)
    {
        var result = new OperationResult();
        try
        {
            if (!fileName.EndsWith(".encrypted"))
            {
                result.Success = false;
                result.Message = "File is not encrypted";
                return result;
            }

            byte[] decrypted = await Task.Run(() => DecryptData(fileData, password));
            string originalName = fileName[..^10]; // Takes all characters of fileName except the last 10 due to string slicing
            result.Success = true;
            result.Data = decrypted;
            result.Message = $"Decrypted: {originalName}";
        }
        catch (CryptographicException)
        {
            result.Success = false;
            result.Message = "Wrong password or corrupted file";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }

    // Cryptography here
    private static byte[] EncryptData(byte[] data, string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(_saltSize);
        byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, _iterations, HashAlgorithmName.SHA256, _outputLength);

        using Aes aes = Aes.Create();
        aes.KeySize = _keySize;
        aes.BlockSize = _blockSize;
        aes.Mode = CipherMode.CBC; // CBC is fine for this use case (FILE encryption)
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var ms = new MemoryStream();
        ms.Write(salt);
        ms.Write(aes.IV);

        using var encryptor = aes.CreateEncryptor(derivedKey, aes.IV);
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        cs.Write(data, 0, data.Length);
        cs.FlushFinalBlock();

        return ms.ToArray();
    }

    private static byte[] DecryptData(byte[] encryptedData, string password)
    {
        using var ms = new MemoryStream(encryptedData);
        byte[] salt = new byte[_saltSize];
        byte[] iv = new byte[_ivSize];
        ms.ReadExactly(salt, 0, _saltSize);
        ms.ReadExactly(iv, 0, _ivSize);

        byte[] derivedKey = Rfc2898DeriveBytes.Pbkdf2(password, salt, _iterations, HashAlgorithmName.SHA256, _outputLength);

        using Aes aes = Aes.Create();
        aes.KeySize = _keySize;
        aes.BlockSize = _blockSize;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor(derivedKey, iv);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var outputMs = new MemoryStream();
        cs.CopyTo(outputMs);
        return outputMs.ToArray();
    }
}

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public byte[] Data { get; set; } = Array.Empty<byte>();
}
