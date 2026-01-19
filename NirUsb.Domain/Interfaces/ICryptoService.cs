namespace NirUsb.Domain.Interfaces;

public interface ICryptoService {
    public byte[] DeriveKeyFromCredentials(string deviceId, string password, byte[] salt);
    public (byte[] publicKey, byte[] privateKey) GenerateRsaKeys();
    public byte[] EncryptWithAes(byte[] key, byte[] bytesToWrite, byte[] iv);
    public byte[] DecryptWithAes(ReadOnlySpan<byte> key, ReadOnlySpan<byte> encryptedPackage);
    public byte[] GenerateRandomBytes(int size);
}