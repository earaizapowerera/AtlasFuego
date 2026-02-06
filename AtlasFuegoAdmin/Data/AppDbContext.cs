using Microsoft.EntityFrameworkCore;
using AtlasFuegoAdmin.Models;

namespace AtlasFuegoAdmin.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<PreRegistro> PreRegistros { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PreRegistro>(entity =>
        {
            entity.ToTable("PreRegistros");
        });
    }
}
