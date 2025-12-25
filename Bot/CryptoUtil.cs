using System.Security.Cryptography;
using System.Text;

namespace Bot;

public static class CryptoUtil
{
    private const int IvLengthByte = 12;
    private const int TagLengthBit = 128;
    private const int AesKeySizeBit = 256;
    private const int Pbkdf2Iterations = 50000;

    public static string Sha256(string source)
    {
        Span<byte> buffer = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(Encoding.UTF8.GetBytes(source), buffer);
        return Convert.ToHexString(buffer);
    }

    private static byte[] PrepCipherKey(string passPhrase, byte[] salt, int iterationCount = Pbkdf2Iterations, int keyStrength = AesKeySizeBit)
    {
        var keyBytes = Rfc2898DeriveBytes.Pbkdf2(
            password: passPhrase,
            salt: salt,
            iterations: iterationCount,
            hashAlgorithm: HashAlgorithmName.SHA1,
            outputLength: keyStrength / 8);

        return keyBytes.Length != keyStrength / 8
            ? throw new InvalidOperationException($"Unexpected key length: {keyBytes.Length * 8} bits instead of {keyStrength} bits")
            : keyBytes;
    }

    public static string? Encrypt(string passPhrase, byte[] salt, string rawData)
    {
        try
        {
            var iv = new byte[IvLengthByte];
            RandomNumberGenerator.Create().GetBytes(iv);

            using var aes = new AesGcm(PrepCipherKey(passPhrase, salt), TagLengthBit / 8);
            var plaintextBytes = Encoding.UTF8.GetBytes(rawData);
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[TagLengthBit / 8];

            aes.Encrypt(iv, plaintextBytes, ciphertext, tag);

            var result = new byte[1 + iv.Length + ciphertext.Length + tag.Length];
            result[0] = (byte)iv.Length;
            Buffer.BlockCopy(iv, 0, result, 1, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, result, 1 + iv.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, result, 1 + iv.Length + ciphertext.Length, tag.Length);

            return Convert.ToBase64String(result);
        }
        catch
        {
            return null;
        }
    }

    public static string? Decrypt(string passPhrase, byte[] salt, string encryptedData)
    {
        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedData);
            if (encryptedBytes.Length < 1 + IvLengthByte + TagLengthBit / 8)
            {
                throw new ArgumentException("Invalid encrypted data format");
            }

            int ivLength = encryptedBytes[0];
            if (ivLength != IvLengthByte)
            {
                throw new ArgumentException($"Invalid IV length: expected {IvLengthByte}, got {ivLength}");
            }

            var iv = new byte[ivLength];
            Buffer.BlockCopy(encryptedBytes, 1, iv, 0, ivLength);

            var tagPosition = 1 + ivLength + (encryptedBytes.Length - 1 - ivLength - TagLengthBit / 8);
            if (tagPosition + TagLengthBit / 8 > encryptedBytes.Length)
            {
                throw new ArgumentException("Invalid encrypted data length");
            }

            var ciphertext = new byte[encryptedBytes.Length - 1 - ivLength - TagLengthBit / 8];
            Buffer.BlockCopy(encryptedBytes, 1 + ivLength, ciphertext, 0, ciphertext.Length);

            var tag = new byte[TagLengthBit / 8];
            Buffer.BlockCopy(encryptedBytes, tagPosition, tag, 0, tag.Length);

            using var aes = new AesGcm(PrepCipherKey(passPhrase, salt), TagLengthBit / 8);
            var plaintextBytes = new byte[ciphertext.Length];
            aes.Decrypt(iv, ciphertext, tag, plaintextBytes);

            return Encoding.UTF8.GetString(plaintextBytes);
        }
        catch
        {
            return null;
        }
    }
}