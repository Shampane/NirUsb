using Microsoft.EntityFrameworkCore;
using NirUsb.Domain.Interfaces;
using NirUsb.Domain.Models;
using NirUsb.Infrastructure.DataAccess;

namespace NirUsb.Infrastructure.Repositories;

public class UserRepository : IUserRepository {
    private readonly AppDbContext _dbContext;
    private readonly IQueryable<User> _usersNoTracking;


    public UserRepository(AppDbContext dbContext) {
        _dbContext = dbContext;
        _usersNoTracking = _dbContext.Users.AsNoTracking();
    }


    public async Task<List<User>> GetUsers() {
        return await _usersNoTracking.ToListAsync();
    }


    public async Task<User?> GetUserByName(string name) {
        return await _usersNoTracking.FirstOrDefaultAsync(u => u.Name == name);
    }


    public async Task CreateUser(User user) {
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();
    }


    public async Task RemoveAllUser() {
        await _dbContext.Users.ExecuteDeleteAsync();
    }


    public async Task<bool> IsUserExists(string name) {
        return await _usersNoTracking.AnyAsync(u => u.Name == name);
    }
}