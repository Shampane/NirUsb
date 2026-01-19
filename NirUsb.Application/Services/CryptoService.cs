using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using NirUsb.Domain.Interfaces;

namespace NirUsb.Application.Services;

public class CryptoService : ICryptoService {
    private const int Argon2DegreeOfParallelism = 8;
    private const int Argon2Iterations = 4;
    private const int Argon2MemorySize = 128 * 1024;
    private const int Argon2HashLength = 32;


    public byte[] GenerateKeyFromDeviceId(string deviceId, string password, byte[] salt) {
        int maxBytesCount = Encoding.UTF8.GetMaxByteCount(Argon2MemorySize);
        byte[] buffer = new byte[maxBytesCount];

        int written = Encoding.UTF8.GetBytes(deviceId + password, buffer);
        byte[] inputSpan = buffer[..written];

        using var argon2 = new Argon2id(inputSpan);

        argon2.Salt = salt;
        argon2.DegreeOfParallelism = Argon2DegreeOfParallelism;
        argon2.Iterations = Argon2Iterations;
        argon2.MemorySize = Argon2MemorySize;

        return argon2.GetBytes(Argon2HashLength);
    }


    public (byte[] publicKey, byte[] privateKey) CreateRsaKeys() {
        using var rsa = RSA.Create(2048);
        return (rsa.ExportSubjectPublicKeyInfo(), rsa.ExportPkcs8PrivateKey());
    }


    public byte[] CreateAesKey(byte[] key, byte[] bytesToWrite, byte[] iv) {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.IV = iv;

        using var ms = new MemoryStream();
        ms.Write(iv, 0, iv.Length);

        using ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        cs.Write(bytesToWrite, 0, bytesToWrite.Length);
        cs.FlushFinalBlock();

        return ms.ToArray();
    }


    public byte[] DecryptAes(byte[] key, byte[] encryptedPackage) {
        byte[] iv = new byte[16];
        byte[] ciphertext = new byte[encryptedPackage.Length - 16];

        Buffer.BlockCopy(encryptedPackage, 0, iv, 0, 16);
        Buffer.BlockCopy(encryptedPackage, 16, ciphertext, 0, ciphertext.Length);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var ms = new MemoryStream(ciphertext);
        using ICryptoTransform decryptor = aes.CreateDecryptor();
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var output = new MemoryStream();

        cs.CopyTo(output);
        return output.ToArray();
    }


    public byte[] GenerateRandomBytes(int size) {
        return RandomNumberGenerator.GetBytes(size);
    }
}