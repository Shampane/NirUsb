using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;
using NirUsb.Domain.Interfaces;

namespace NirUsb.Application.Services;

public class CryptoService : ICryptoService {
    public byte[] CreateArgon2Key(string deviceId, string password, byte[] salt) {
        byte[] sumBytes = Encoding.UTF8.GetBytes(deviceId + password);
        using var argon2 = new Argon2id(sumBytes);

        argon2.Salt = salt;
        argon2.DegreeOfParallelism = 8;
        argon2.Iterations = 4;
        argon2.MemorySize = 128 * 1024;

        return argon2.GetBytes(32);
    }


    public (byte[] publicKey, byte[] privateKey) CreateRsaKeys() {
        using var rsa = RSA.Create(2048);
        byte[] privateKeyBytes = rsa.ExportPkcs8PrivateKey();
        byte[] publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        return (publicKeyBytes, privateKeyBytes);
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


    public byte[] GenerateRandomBytes(int size) {
        byte[] bytes = new byte[size];
        RandomNumberGenerator.Fill(bytes);
        return bytes;
    }
}