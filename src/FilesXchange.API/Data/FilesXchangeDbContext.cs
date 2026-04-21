using FilesXchange.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FilesXchange.API.Data;

public class FilesXchangeDbContext(DbContextOptions dbContextOptions) : DbContext(dbContextOptions)
{
    public DbSet<FileForExchange> Files { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var fileExchange = modelBuilder.Entity<FileForExchange>();

        fileExchange.ToTable("FilesForExchanges");

        fileExchange.HasKey(x => x.Id);

        fileExchange.Property(x => x.Token)
            .IsRequired()
            .HasMaxLength(128);

        fileExchange.HasIndex(x => x.Token)
            .IsUnique();

        fileExchange.Property(x => x.PathsToFiles)
            .IsRequired();

        fileExchange.Property(x => x.CreatedAt)
            .IsRequired();

        fileExchange.Property(x => x.ExpiresAt)
            .IsRequired();
    }
}
