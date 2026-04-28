using FilesXchange.API.Controllers;
using FilesXchange.API.Contracts;
using FilesXchange.API.Helpers.Interfaces;
using FilesXchange.API.Helpers.ServiceObjects.FileExchange;
using FilesXchange.API.Helpers.ServiceObjects.FileStorage;
using FilesXchange.API.Models;
using FilesXchange.API.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.IO.Compression;

namespace FilesXchange.API.Tests.Controllers;

public sealed class FileXchangeControllerTests
{
    [Fact]
    public async Task UploadAsync_WithoutFiles_ReturnsBadRequest()
    {
        var controller = CreateController();

        var result = await controller.UploadAsync(null, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, badRequest.StatusCode);

        var payload = Assert.IsType<ErrorEnvelope>(badRequest.Value);
        Assert.Equal("files_missing", payload.Error.Code);
    }

    [Fact]
    public async Task DownloadAsync_WithUnknownToken_ReturnsNotFound()
    {
        var controller = CreateController();

        var result = await controller.DownloadAsync("missing-token", CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);

        var payload = Assert.IsType<ErrorEnvelope>(notFound.Value);
        Assert.Equal("token_not_found", payload.Error.Code);
    }

    [Fact]
    public async Task UploadAsync_WhenTotalSizeExceedsLimit_ReturnsPayloadTooLarge()
    {
        var controller = CreateController(appOptions: new AppOptions
        {
            MaxFileSizeBytes = 3
        });

        var files = new List<IFormFile>
        {
            new FormFile(new MemoryStream(new byte[] { 1, 2 }), 0, 2, "files", "first.txt"),
            new FormFile(new MemoryStream(new byte[] { 3, 4 }), 0, 2, "files", "second.txt")
        };

        var result = await controller.UploadAsync(files, CancellationToken.None);

        var payloadTooLarge = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, payloadTooLarge.StatusCode);

        var payload = Assert.IsType<ErrorEnvelope>(payloadTooLarge.Value);
        Assert.Equal("payload_too_large", payload.Error.Code);
    }

    [Fact]
    public async Task DownloadAsync_WithExpiredToken_ReturnsGone()
    {
        var exchange = new FileForExchange
        {
            Token = "expired-token",
            PathsToFiles = "[]",
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1)
        };

        var controller = CreateController(cacheService: new StubCacheService(exchange));

        var result = await controller.DownloadAsync(exchange.Token, CancellationToken.None);

        var gone = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status410Gone, gone.StatusCode);

        var payload = Assert.IsType<ErrorEnvelope>(gone.Value);
        Assert.Equal("token_expired", payload.Error.Code);
    }

    [Fact]
    public async Task UploadAsync_WithSingleFile_ReturnsOk()
    {
        var exchange = CreateExchange("single-file-token");
        var controller = CreateController(
            fileExchangeService: new StubFileExchangeService(
                new FileExchangeCreationResult(
                    exchange,
                    [new StoredFileDescriptor("uploads/internal/one.txt", "one.txt", 3)])));

        var files = new List<IFormFile>
        {
            new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "files", "one.txt")
        };

        var result = await controller.UploadAsync(files, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var payload = Assert.IsType<UploadResponse>(ok.Value);
        Assert.Equal("single-file-token", payload.Token);
        Assert.Equal(1, payload.FileCount);
        Assert.Equal("/api/download/single-file-token", payload.DownloadUrl);
    }

    [Fact]
    public async Task UploadAsync_WithMultipleFiles_ReturnsOkWithFileCount()
    {
        var exchange = CreateExchange("multi-file-token");
        var controller = CreateController(
            fileExchangeService: new StubFileExchangeService(
                new FileExchangeCreationResult(
                    exchange,
                    [
                        new StoredFileDescriptor("uploads/internal/one.txt", "one.txt", 3),
                        new StoredFileDescriptor("uploads/internal/two.txt", "two.txt", 4)
                    ])));

        var files = new List<IFormFile>
        {
            new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "files", "one.txt"),
            new FormFile(new MemoryStream(new byte[] { 4, 5, 6, 7 }), 0, 4, "files", "two.txt")
        };

        var result = await controller.UploadAsync(files, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);

        var payload = Assert.IsType<UploadResponse>(ok.Value);
        Assert.Equal("multi-file-token", payload.Token);
        Assert.Equal(2, payload.FileCount);
        Assert.Equal("/api/download/multi-file-token", payload.DownloadUrl);
    }

    [Fact]
    public async Task DownloadAsync_WithSingleFile_ReturnsFileResult()
    {
        var exchange = CreateExchange(
            "single-download-token",
            [new StoredFileDescriptor("uploads/internal/report.txt", "report.txt", 4)]);
        var controller = CreateController(
            cacheService: new StubCacheService(exchange),
            fileStorageService: new StubFileStorageService(new Dictionary<string, byte[]>
            {
                ["uploads/internal/report.txt"] = [1, 2, 3, 4]
            }));

        var result = await controller.DownloadAsync(exchange.Token, CancellationToken.None);

        var fileResult = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("text/plain", fileResult.ContentType);
        Assert.Equal("report.txt", fileResult.FileDownloadName);
    }

    [Fact]
    public async Task DownloadAsync_WithMultipleFiles_ReturnsZipArchive()
    {
        var exchange = CreateExchange(
            "zip-download-token",
            [
                new StoredFileDescriptor("uploads/internal/one.txt", "one.txt", 3),
                new StoredFileDescriptor("uploads/internal/two.txt", "two.txt", 4)
            ]);
        var controller = CreateController(
            cacheService: new StubCacheService(exchange),
            fileStorageService: new StubFileStorageService(new Dictionary<string, byte[]>
            {
                ["uploads/internal/one.txt"] = [1, 2, 3],
                ["uploads/internal/two.txt"] = [4, 5, 6, 7]
            }));

        await using var responseBody = new MemoryStream();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        controller.ControllerContext.HttpContext.Response.Body = responseBody;

        var result = await controller.DownloadAsync(exchange.Token, CancellationToken.None);

        Assert.IsType<EmptyResult>(result);
        Assert.Equal("application/zip", controller.Response.ContentType);
        Assert.Equal("attachment; filename=\"files.zip\"", controller.Response.Headers.ContentDisposition.ToString());

        responseBody.Position = 0;
        using var archive = new ZipArchive(responseBody, ZipArchiveMode.Read, leaveOpen: true);
        Assert.Collection(
            archive.Entries.OrderBy(static entry => entry.FullName),
            first => Assert.Equal("one.txt", first.FullName),
            second => Assert.Equal("two.txt", second.FullName));
    }

    private static FileXchangeController CreateController(
        IFileExchangeService? fileExchangeService = null,
        ICacheService? cacheService = null,
        IFileStorageService? fileStorageService = null,
        AppOptions? appOptions = null)
    {
        return new FileXchangeController(
            fileExchangeService ?? new StubFileExchangeService(),
            cacheService ?? new StubCacheService(),
            fileStorageService ?? new StubFileStorageService(),
            NullLogger<FileXchangeController>.Instance,
            Microsoft.Extensions.Options.Options.Create(appOptions ?? new AppOptions()));
    }

    private static FileForExchange CreateExchange(
        string token,
        IReadOnlyList<StoredFileDescriptor>? storedFiles = null)
    {
        var createdAt = DateTime.UtcNow;

        return new FileForExchange
        {
            Token = token,
            PathsToFiles = StoredFileMetadataSerializer.Serialize(storedFiles ?? []),
            CreatedAt = createdAt,
            ExpiresAt = createdAt.AddDays(7)
        };
    }

    private sealed class StubFileExchangeService : IFileExchangeService
    {
        private readonly FileExchangeCreationResult? _result;

        public StubFileExchangeService(FileExchangeCreationResult? result = null)
        {
            _result = result;
        }

        public Task<FileExchangeCreationResult> CreateAsync(
            IEnumerable<IFormFile> files,
            CancellationToken cancellationToken = default)
        {
            if (_result is null)
            {
                throw new NotSupportedException();
            }

            return Task.FromResult(_result);
        }
    }

    private sealed class StubCacheService : ICacheService
    {
        private readonly FileForExchange? _exchange;

        public StubCacheService(FileForExchange? exchange = null)
        {
            _exchange = exchange;
        }

        public void SetFileExchange(FileForExchange fileForExchange)
        {
        }

        public void RemoveFileExchange(string token)
        {
        }

        public Task<FileForExchange?> GetFileExchangeByTokenAsync(
            string token,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_exchange);
    }

    private sealed class StubFileStorageService : IFileStorageService
    {
        private readonly IReadOnlyDictionary<string, byte[]> _files;

        public StubFileStorageService(IReadOnlyDictionary<string, byte[]>? files = null)
        {
            _files = files ?? new Dictionary<string, byte[]>();
        }

        public Task<IReadOnlyList<StoredFileDescriptor>> SaveFilesAsync(
            IEnumerable<IFormFile> files,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<StoredFileReadHandle> OpenReadAsync(
            string relativePath,
            CancellationToken cancellationToken = default)
        {
            if (!_files.TryGetValue(relativePath, out var content))
            {
                throw new NotSupportedException();
            }

            var fileName = Path.GetFileName(relativePath);
            return Task.FromResult(new StoredFileReadHandle(new MemoryStream(content), fileName, content.LongLength));
        }

        public Task DeleteFileAsync(
            string relativePath,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteFilesDirectoryAsync(
            IEnumerable<string> relativePaths,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
