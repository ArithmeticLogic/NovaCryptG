using Microsoft.EntityFrameworkCore;
using NovaCryptG.Models;

namespace NovaCryptG.Data;

// This class serves as the database session/context.
// It is the main interface for interacting with the database using EF Core.
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // DbSet represents the "LoginCredentials" table.
    public DbSet<LoginCredential> LoginCredentials { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Explicitly maps the model class to a specific table name.
        modelBuilder.Entity<LoginCredential>().ToTable("LoginCredentials");

        // Sets "UserName" as the unique primary key for this table.
        modelBuilder.Entity<LoginCredential>().HasKey(c => c.UserName);
    }
}