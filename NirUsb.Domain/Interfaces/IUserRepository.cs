using NirUsb.Domain.Models;

namespace NirUsb.Domain.Interfaces;

public interface IUserRepository {
    public Task<List<User>> GetUsers();
    public Task<User?> GetUserByName(string name);
    public Task CreateUser(User user);
    public Task<bool> IsUserExists(string name);
}