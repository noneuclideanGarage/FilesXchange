using FilesXchange.API.Data;
using FilesXchange.API.Helpers.Interfaces;
using FilesXchange.API.Helpers.ServiceObjects.FileStorage;
using FilesXchange.API.Models;
using FilesXchange.API.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace FilesXchange.API.Tests.Services;

public sealed class ExpiredFileCleanupServiceTests
{
    [Fact]
    public async Task ExecuteOnceAsync_RemovesExpiredExchangeFromDatabaseAndCache()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var dbOptions = new DbContextOptionsBuilder<FilesXchangeDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new FilesXchangeDbContext(dbOptions);
        await context.Database.EnsureCreatedAsync();

        var exchange = CreateExpiredExchange("expired-token");
        context.Files.Add(exchange);
        await context.SaveChangesAsync();

        var cacheService = new StubCacheService();
        var storageService = new StubFileStorageService();
        var service = new ExpiredFileCleanupService(
            context,
            storageService,
            cacheService,
            NullLogger<ExpiredFileCleanupService>.Instance);

        var result = await service.ExecuteOnceAsync();

        Assert.Equal(1, result.ExpiredFound);
        Assert.Equal(1, result.Removed);
        Assert.Equal(0, result.Failed);
        Assert.Empty(await context.Files.ToListAsync());
        Assert.Equal("expired-token", cacheService.RemovedToken);
    }

    [Fact]
    public async Task ExecuteOnceAsync_DeletesStoredFilesDirectoryForExpiredExchange()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var dbOptions = new DbContextOptionsBuilder<FilesXchangeDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new FilesXchangeDbContext(dbOptions);
        await context.Database.EnsureCreatedAsync();

        var exchange = CreateExpiredExchange(
            "expired-token",
            [
                new StoredFileDescriptor("uploads/internal/one.txt", "one.txt", 3),
                new StoredFileDescriptor("uploads/internal/two.txt", "two.txt", 4)
            ]);
        context.Files.Add(exchange);
        await context.SaveChangesAsync();

        var storageService = new StubFileStorageService();
        var service = new ExpiredFileCleanupService(
            context,
            storageService,
            new StubCacheService(),
            NullLogger<ExpiredFileCleanupService>.Instance);

        var result = await service.ExecuteOnceAsync();

        Assert.Equal(1, result.ExpiredFound);
        Assert.Equal(1, result.Removed);
        Assert.Equal(0, result.Failed);
        Assert.Equal(
            ["uploads/internal/one.txt", "uploads/internal/two.txt"],
            storageService.DeletedRelativePaths);
    }

    private static FileForExchange CreateExpiredExchange(
        string token,
        IReadOnlyList<StoredFileDescriptor>? storedFiles = null)
    {
        var createdAt = DateTime.UtcNow.AddDays(-8);

        return new FileForExchange
        {
            Token = token,
            PathsToFiles = StoredFileMetadataSerializer.Serialize(storedFiles ?? []),
            CreatedAt = createdAt,
            ExpiresAt = createdAt.AddDays(1)
        };
    }

    private sealed class StubCacheService : ICacheService
    {
        public string? RemovedToken { get; private set; }

        public Task<FileForExchange?> GetFileExchangeByTokenAsync(
            string token,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<FileForExchange?>(null);

        public void SetFileExchange(FileForExchange fileExchange)
        {
        }

        public void RemoveFileExchange(string token)
        {
            RemovedToken = token;
        }
    }

    private sealed class StubFileStorageService : IFileStorageService
    {
        public IReadOnlyList<string> DeletedRelativePaths { get; private set; } = [];

        public Task<IReadOnlyList<StoredFileDescriptor>> SaveFilesAsync(
            IEnumerable<Microsoft.AspNetCore.Http.IFormFile> files,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<StoredFileReadHandle> OpenReadAsync(
            string relativePath,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteFileAsync(
            string relativePath,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteFilesDirectoryAsync(
            IEnumerable<string> relativePaths,
            CancellationToken cancellationToken = default)
        {
            DeletedRelativePaths = relativePaths.ToArray();
            return Task.CompletedTask;
        }
    }
}
