using System.Text;

namespace NovaCryptG.Services;

public class EncryptionService
{
    // 15 is balanced (Speed and Security), 20 is increased security, 25 is max security
    private const int Rounds = 25; // Number of times to repeat all layers of the encryption process

    // File Operations
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public byte[] Data { get; set; } = Array.Empty<byte>();
    }

    public static async Task<OperationResult> EncryptFileAsync(byte[] fileData, string fileName, string password)
    {
        var result = new OperationResult();
        try
        {
            // Validating password
            if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsDigit) || !password.Any(c => !char.IsLetterOrDigit(c)))
            {
                result.Success = false;
                result.Message =
                    "Password must be at least 8 characters long and have at least 1 upper case character, digit, and special character";
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
            // Setting file format
            if (!fileName.EndsWith(".encrypted"))
            {
                result.Success = false;
                result.Message = "File is not encrypted";
                return result;
            }

            byte[] decrypted = await Task.Run(() => DecryptData(fileData, password));
            string originalName = fileName[..^10];
            result.Success = true;
            result.Data = decrypted;
            result.Message = $"Decrypted: {originalName}";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = ex.Message;
        }

        return result;
    }

    // Multi layer XOR Cipher and bit rotation with multiple rounds
    private static byte[] EncryptData(byte[] data, string password)
    {
        return Cryptography(data, password, true);
    }

    private static byte[] DecryptData(byte[] encryptedData, string password)
    {
        return Cryptography(encryptedData, password, false);
    }

    private static byte[] Cryptography(byte[] data, string password, bool encrypt)
    {
        byte[] result = new byte[data.Length];
        Array.Copy(data, result, data.Length);

        if (encrypt)
        {
            // Run all layers multiple times (Rounds)
            for (int round = 0; round < Rounds; round++)
            {
                // Layer 1: Standard XOR
                result = StandardXOR(result, password);

                // Layer 2: XOR in reverse order
                result = ReversedXOR(result, password);

                // Layer 3: Rolling XOR (each byte affects the next)
                result = RollingXOR(result, password, true);

                // Layer 4: XOR with reversed password
                result = ReversedPasswordXOR(result, password);

                // Layer 5: Bit Rotation
                result = BitRotation(result, password, true);
            }
        }
        else
        {
            // Decryption applies layers in reverse order for the same number of rounds
            for (int round = 0; round < Rounds; round++)
            {
                result = BitRotation(result, password, false);
                result = ReversedPasswordXOR(result, password);
                result = RollingXOR(result, password, false);
                result = ReversedXOR(result, password);
                result = StandardXOR(result, password);
            }
        }

        return result;
    }

    // Layer 1: Standard XOR
    private static byte[] StandardXOR(byte[] data, string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] result = new byte[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ passwordBytes[i % passwordBytes.Length]);
        }

        return result;
    }

    // Layer 2: XOR in reverse order
    private static byte[] ReversedXOR(byte[] data, string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] result = new byte[data.Length];
        Array.Copy(data, result, data.Length);

        int halfIndex = data.Length / 2;
        for (int i = 0; i < halfIndex; i++)
        {
            result[i] = (byte)(result[i] ^ passwordBytes[(result.Length - 1 - i) % passwordBytes.Length]);
            result[result.Length - 1 - i] =
                (byte)(result[result.Length - 1 - i] ^ passwordBytes[i % passwordBytes.Length]);
        }

        return result;
    }

    // Layer 3: Rolling XOR (each byte affects the next)
    private static byte[] RollingXOR(byte[] data, string password, bool encrypt)
    {
        byte[] result = new byte[data.Length];
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

        if (encrypt)
        {
            byte previous = 0;
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ previous ^ passwordBytes[i % passwordBytes.Length]);
                previous = result[i];
            }
        }
        else
        {
            // Decryption: reverse the rolling XOR
            byte previous = 0;
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ previous ^ passwordBytes[i % passwordBytes.Length]);
                previous = data[i];
            }
        }

        return result;
    }

    // Layer 4: XOR with reversed password
    private static byte[] ReversedPasswordXOR(byte[] data, string password)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] result = new byte[data.Length];

        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (byte)(data[i] ^ passwordBytes[(passwordBytes.Length - 1 - (i % passwordBytes.Length))]);
        }

        return result;
    }

    // Layer 5: Bit Rotation
    private static byte[] BitRotation(byte[] data, string password, bool encrypt)
    {
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[] result = new byte[data.Length];

        if (encrypt)
        {
            // Rotate left during encryption
            for (int i = 0; i < data.Length; i++)
            {
                int shift = passwordBytes[i % passwordBytes.Length] % 7 + 1; // Shift 1-7 bits
                result[i] = (byte)((data[i] << shift) | (data[i] >> (8 - shift)));
            }
        }
        else
        {
            // Rotate right during decryption
            for (int i = 0; i < data.Length; i++)
            {
                int shift = passwordBytes[i % passwordBytes.Length] % 7 + 1;
                result[i] = (byte)((data[i] >> shift) | (data[i] << (8 - shift)));
            }
        }

        return result;
    }
}