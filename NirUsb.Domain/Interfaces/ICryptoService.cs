namespace NirUsb.Domain.Interfaces;

public interface ICryptoService {
    public byte[] CreateArgon2Key(string deviceId, string password, byte[] salt);
    public (byte[] publicKey, byte[] privateKey) CreateRsaKeys();
    public byte[] CreateAesKey(byte[] key, byte[] bytesToWrite, byte[] iv);
    public byte[] GenerateRandomBytes(int size);
}