using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using NirUsb.Domain.Interfaces;

namespace NirUsb.Application.Services;

public class CryptoService : ICryptoService {
    private const int Argon2DegreeOfParallelism = 8;
    private const int Argon2Iterations = 4;
    private const int Argon2MemorySizeKb = 128 * 1024;
    private const int Argon2HashLengthBytes = 32;

    private const int RsaKeySizeBits = 2048;

    private const int AesKeySizeBits = 256;
    private const int AesIvSize = 16;


    public byte[] DeriveKeyFromCredentials(string deviceId, string password, byte[] salt) {
        string credentials = string.Concat(deviceId, password);
        byte[] credentialsBytes = Encoding.UTF8.GetBytes(credentials);

        try {
            using var argon2 = new Argon2id(credentialsBytes);
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = Argon2DegreeOfParallelism;
            argon2.Iterations = Argon2Iterations;
            argon2.MemorySize = Argon2MemorySizeKb;

            return argon2.GetBytes(Argon2HashLengthBytes);
        } finally {
            CryptographicOperations.ZeroMemory(credentialsBytes);
        }
    }


    public (byte[] publicKey, byte[] privateKey) GenerateRsaKeys() {
        using var rsa = RSA.Create(RsaKeySizeBits);
        return (rsa.ExportSubjectPublicKeyInfo(), rsa.ExportPkcs8PrivateKey());
    }


    public byte[] EncryptWithAes(byte[] key, byte[] text, byte[] iv) {
        using var aes = Aes.Create();
        aes.KeySize = AesKeySizeBits;
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.IV = iv;

        int ciphertextLength = aes.GetCiphertextLengthCbc(text.Length);
        byte[] result = new byte[AesIvSize + ciphertextLength];

        iv.CopyTo(result.AsSpan(0, AesIvSize));
        int written = aes.EncryptCbc(text, iv, result.AsSpan(AesIvSize));

        return result.Length == AesIvSize + written
            ? result
            : result[..(AesIvSize + written)];
    }


    public byte[] DecryptWithAes(ReadOnlySpan<byte> key, ReadOnlySpan<byte> encryptedData) {
        if (encryptedData.Length <= AesIvSize) {
            throw new ArgumentException("Invalid data");
        }

        ReadOnlySpan<byte> iv = encryptedData[..AesIvSize];
        ReadOnlySpan<byte> ciphertext = encryptedData[AesIvSize..];

        using var aes = Aes.Create();
        aes.Key = key.ToArray();
        aes.IV = iv.ToArray();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        byte[] result = new byte[ciphertext.Length];
        int resultLength = aes.DecryptCbc(ciphertext, iv, result);

        return result[..resultLength];
    }


    public bool VerifyUserIdentity(
        ReadOnlySpan<byte> derivedKey, ReadOnlySpan<byte> encryptedPrivateKey,
        ReadOnlySpan<byte> userPublicKey
    ) {
        try {
            byte[] decryptedPrivateKey = DecryptWithAes(derivedKey, encryptedPrivateKey);

            using var rsa = RSA.Create();
            rsa.ImportPkcs8PrivateKey(decryptedPrivateKey, out _);
            byte[] derivedPublicKey = rsa.ExportSubjectPublicKeyInfo();

            return CryptographicOperations.FixedTimeEquals(derivedPublicKey, userPublicKey);
        } catch {
            return false;
        }
    }


    public byte[] GenerateRandomBytes(int size) {
        return RandomNumberGenerator.GetBytes(size);
    }
}