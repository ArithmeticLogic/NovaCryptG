using Microsoft.EntityFrameworkCore;
using NovaCryptG.Models;

namespace NovaCryptG.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<LoginCredential> LoginCredentials { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<LoginCredential>().ToTable("LoginCredentials");
        modelBuilder.Entity<LoginCredential>().HasKey(c => c.UserName);
    }
}