using FilesXchange.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FilesXchange.API.Data;

public class FilesXchangeDbContext : DbContext
{
    public const string DbName = "files.db";
    public DbSet<FileForExchange> Files { get; set; }

    public required string DbPath { get; init; }

    public FilesXchangeDbContext()
    {
        var pathForDatabase = Environment
            .GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        DbPath = Path.Join(pathForDatabase, DbName);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");
}
