using Microsoft.EntityFrameworkCore;
using NirUsb.Domain.Interfaces;
using NirUsb.Domain.Models;
using NirUsb.Infrastructure.DataAccess;

namespace NirUsb.Infrastructure.Repositories;

public class UserRepository : IUserRepository {
    private readonly AppDbContext _dbContext;


    public UserRepository(AppDbContext dbContext) {
        _dbContext = dbContext;
    }


    public async Task<List<User>> GetUsers() {
        return await _dbContext.Users.ToListAsync();
    }


    public async Task<User?> GetUserByName(string name) {
        return await _dbContext.Users.FirstOrDefaultAsync(u => u.Name == name);
    }


    public async Task CreateUser(User user) {
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
    }


    public async Task<bool> IsUserExists(string name) {
        return await _dbContext.Users.AnyAsync(u => u.Name == name);
    }
}