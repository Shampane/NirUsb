using Microsoft.EntityFrameworkCore;
using NirUsb.Domain.Models;

namespace NirUsb.Infrastructure.DataAccess;

public class AppDbContext : DbContext {
    public DbSet<User> Users { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>().HasKey(u => u.Id);
    }


    protected override void OnConfiguring(DbContextOptionsBuilder builder) {
        if (!builder.IsConfigured) {
            builder.UseSqlite("Data Source=mydatabase.db");
        }
    }
}