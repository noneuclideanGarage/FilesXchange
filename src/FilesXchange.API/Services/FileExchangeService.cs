using FilesXchange.API.Data;
using FilesXchange.API.Helpers.Exceptions;
using FilesXchange.API.Helpers.Interfaces;
using FilesXchange.API.Helpers.ServiceObjects.FileExchange;
using FilesXchange.API.Models;
using FilesXchange.API.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FilesXchange.API.Services;

public sealed class FileExchangeService : IFileExchangeService
{
    private readonly FilesXchangeDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ITokenService _tokenService;
    private readonly AppOptions _options;

    public FileExchangeService(
        FilesXchangeDbContext context,
        IFileStorageService fileStorageService,
        ITokenService tokenService,
        IOptions<AppOptions> options)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _tokenService = tokenService;
        _options = options.Value;
    }

    public async Task<FileExchangeCreationResult> CreateAsync(
        IEnumerable<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        var fileList = files.ToList();
        var storedFiles = await _fileStorageService.SaveFilesAsync(fileList, cancellationToken);
        var createdAt = DateTime.UtcNow;
        var expiresAt = createdAt.AddDays(_options.TokenExpirationDays);

        try
        {
            var token = await GenerateUniqueTokenAsync(cancellationToken);
            var exchange = new FileForExchange
            {
                Token = token,
                PathsToFiles = StoredFileMetadataSerializer.Serialize(storedFiles),
                CreatedAt = createdAt,
                ExpiresAt = expiresAt
            };

            _context.Files.Add(exchange);
            await _context.SaveChangesAsync(cancellationToken);

            return new FileExchangeCreationResult(exchange, storedFiles);
        }
        catch (Exception exception)
        {
            await _fileStorageService.DeleteFilesDirectoryAsync(
                storedFiles.Select(static file => file.RelativePath),
                cancellationToken);

            throw new FileExchangeCreationException(
                "Failed to create file exchange record after storing files.",
                exception);
        }
    }

    private async Task<string> GenerateUniqueTokenAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var token = _tokenService.GenerateToken();
            var tokenExists = await _context.Files
                .AsNoTracking()
                .AnyAsync(exchange => exchange.Token == token, cancellationToken);

            if (!tokenExists)
            {
                return token;
            }
        }

        throw new FileExchangeCreationException("Failed to generate a unique token.");
    }
}