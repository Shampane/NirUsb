namespace NirUsb.Domain.Interfaces;

public interface ICryptoService {
    public byte[] GenerateKeyFromDeviceId(string deviceId, string password, byte[] salt);
    public (byte[] publicKey, byte[] privateKey) CreateRsaKeys();
    public byte[] CreateAesKey(byte[] key, byte[] bytesToWrite, byte[] iv);
    public byte[] DecryptAes(byte[] key, byte[] encryptedPackage);
    public byte[] GenerateRandomBytes(int size);
}