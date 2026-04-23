using FilesXchange.API.Data;
using FilesXchange.API.Helpers.Interfaces;
using FilesXchange.API.Helpers.ServiceObjects.FileStorage;
using FilesXchange.API.Options;
using FilesXchange.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FilesXchange.API.Tests.Services;

public sealed class FileExchangeServiceTests
{
    [Fact]
    public async Task CreateAsync_SetsExpiresAtUsingConfiguredTokenExpirationDays()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var dbOptions = new DbContextOptionsBuilder<FilesXchangeDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new FilesXchangeDbContext(dbOptions);
        await context.Database.EnsureCreatedAsync();

        var appOptions = Microsoft.Extensions.Options.Options.Create(new AppOptions
        {
            TokenExpirationDays = 5
        });

        var service = new FileExchangeService(
            context,
            new StubFileStorageService(),
            new StubTokenService(),
            appOptions);

        var files = new List<IFormFile>
        {
            new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "file", "test.txt")
        };

        var beforeCreate = DateTime.UtcNow;
        var result = await service.CreateAsync(files);
        var afterCreate = DateTime.UtcNow;

        Assert.Equal(TimeSpan.FromDays(appOptions.Value.TokenExpirationDays), result.Exchange.ExpiresAt - result.Exchange.CreatedAt);
        Assert.InRange(result.Exchange.CreatedAt, beforeCreate, afterCreate);
        Assert.Equal("stub-token", result.Exchange.Token);
    }

    private sealed class StubTokenService : ITokenService
    {
        public string GenerateToken() => "stub-token";
    }

    private sealed class StubFileStorageService : IFileStorageService
    {
        public Task<IReadOnlyList<StoredFileDescriptor>> SaveFilesAsync(
            IEnumerable<IFormFile> files,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<StoredFileDescriptor> storedFiles =
            [
                new StoredFileDescriptor("uploads/internal/test.txt", "test.txt", 3)
            ];

            return Task.FromResult(storedFiles);
        }

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
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
