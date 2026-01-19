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


    public byte[] DeriveKeyFromCredentials(string deviceId, string password, byte[] salt) {
        string credentials = string.Concat(deviceId, password);
        byte[] credentialsBytes = Encoding.UTF8.GetBytes(credentials);

        using var argon2 = new Argon2id(credentialsBytes);

        argon2.Salt = salt;
        argon2.DegreeOfParallelism = Argon2DegreeOfParallelism;
        argon2.Iterations = Argon2Iterations;
        argon2.MemorySize = Argon2MemorySizeKb;

        return argon2.GetBytes(Argon2HashLengthBytes);
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

        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);

        ms.Write(iv, 0, iv.Length);
        cs.Write(text, 0, text.Length);
        cs.FlushFinalBlock();

        return ms.ToArray();
    }


    public byte[] DecryptWithAes(ReadOnlySpan<byte> key, ReadOnlySpan<byte> encryptedData) {
        byte[] iv = encryptedData[..16].ToArray();
        byte[] ciphertext = encryptedData[16..].ToArray();

        using var aes = Aes.Create();
        aes.Key = key.ToArray();
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream(ciphertext);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);

        using var result = new MemoryStream();
        cs.CopyTo(result);
        return result.ToArray();
    }


    public bool VerifyUserIdentity(
        ReadOnlySpan<byte> derivedKey, ReadOnlySpan<byte> encryptedPrivateKey,
        ReadOnlySpan<byte> userPublicKey
    ) {
        byte[] decryptedPrivateKey = DecryptWithAes(derivedKey, encryptedPrivateKey);

        using var rsa = RSA.Create();
        rsa.ImportPkcs8PrivateKey(decryptedPrivateKey, out _);
        byte[] derivedPublicKey = rsa.ExportSubjectPublicKeyInfo();

        return derivedPublicKey.AsSpan().SequenceEqual(userPublicKey);
    }


    public byte[] GenerateRandomBytes(int size) {
        return RandomNumberGenerator.GetBytes(size);
    }
}