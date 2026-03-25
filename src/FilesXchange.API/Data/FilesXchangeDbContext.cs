using FilesXchange.API.Models;
using Microsoft.EntityFrameworkCore;

namespace FilesXchange.API.Data;

public class FilesXchangeDbContext(DbContextOptions dbContextOptions) : DbContext(dbContextOptions)
{
    public DbSet<FileForExchange> Files { get; set; }
}
