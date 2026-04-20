using System.IO.Compression;
using System.Text.Json;
using FilesXchange.API.Helpers.Contracts;
using FilesXchange.API.Helpers.Exceptions;
using FilesXchange.API.Helpers.Interfaces;
using FilesXchange.API.Models;
using FilesXchange.API.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

namespace FilesXchange.API.Controllers;

[ApiController]
[Route("api")]
public sealed class FileXchangeController : ControllerBase
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();
    private readonly IFileExchangeService _fileExchangeService;
    private readonly ICacheService _cacheService;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<FileXchangeController> _logger;
    private readonly AppOptions _options;

    public FileXchangeController(
        IFileExchangeService fileExchangeService,
        ICacheService cacheService,
        IFileStorageService fileStorageService,
        ILogger<FileXchangeController> logger,
        IOptions<AppOptions> options)
    {
        _fileExchangeService = fileExchangeService;
        _cacheService = cacheService;
        _fileStorageService = fileStorageService;
        _logger = logger;
        _options = options.Value;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType<UploadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorEnvelope>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ErrorEnvelope>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ErrorEnvelope>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadResponse>> UploadAsync(
        [FromForm(Name = "files")] List<IFormFile>? files,
        CancellationToken cancellationToken)
    {
        if (files is null || files.Count == 0)
        {
            return BadRequest(new ErrorEnvelope(new ApiError(
                "files_missing",
                "At least one file must be provided.")));
        }

        if (files.Any(static file => file.Length <= 0))
        {
            return BadRequest(new ErrorEnvelope(new ApiError(
                "empty_file",
                "Uploaded files must not be empty.")));
        }

        if (files.Count > _options.MaxFilesPerUpload)
        {
            return BadRequest(new ErrorEnvelope(new ApiError(
                "too_many_files",
                $"No more than {_options.MaxFilesPerUpload} files are allowed per upload.")));
        }

        long totalSize;
        try
        {
            totalSize = checked(files.Sum(static file => file.Length));
        }
        catch (OverflowException)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new ErrorEnvelope(new ApiError(
                "payload_too_large",
                "Total upload size exceeds the configured limit.")));
        }

        if (totalSize > _options.MaxFileSizeBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new ErrorEnvelope(new ApiError(
                "payload_too_large",
                "Total upload size exceeds the configured limit.")));
        }

        try
        {
            var result = await _fileExchangeService.CreateAsync(files, cancellationToken);
            _cacheService.SetFileExchange(result.Exchange);
            _logger.LogInformation(
                "[UPLOAD] Token={Token}, Files={FileCount}, Size={TotalSizeBytes}B, ExpiresAt={ExpiresAt}",
                result.Exchange.Token,
                files.Count,
                totalSize,
                result.Exchange.ExpiresAt);

            return Ok(new UploadResponse(
                result.Exchange.Token,
                files.Count,
                result.Exchange.ExpiresAt,
                $"/api/download/{result.Exchange.Token}"));
        }
        catch (FileStorageException)
        {
            _logger.LogError("[FS ERROR] Upload failed while saving files");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorEnvelope(new ApiError(
                "storage_error",
                "Failed to save uploaded files.")));
        }
        catch (FileExchangeCreationException)
        {
            _logger.LogError("[EXCHANGE ERROR] Failed to create exchange record after upload");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorEnvelope(new ApiError(
                "exchange_creation_failed",
                "Failed to create a file exchange record.")));
        }
    }

    [HttpGet("download/{token}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorEnvelope>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorEnvelope>(StatusCodes.Status410Gone)]
    [ProducesResponseType<ErrorEnvelope>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadAsync(string token, CancellationToken cancellationToken)
    {
        var resolvedExchange = await ResolveActiveExchangeAsync(token, cancellationToken);
        if (resolvedExchange.ErrorResult is not null)
        {
            return resolvedExchange.ErrorResult;
        }

        try
        {
            if (resolvedExchange.Files.Count == 1)
            {
                var file = resolvedExchange.Files[0];
                var handle = await _fileStorageService.OpenReadAsync(file.RelativePath, cancellationToken);
                var contentType = ResolveContentType(file.FileName);
                _logger.LogInformation("[DOWNLOAD] Token={Token}, File={FileName}",
                    resolvedExchange.Exchange!.Token,
                    file.FileName);
                return File(handle.Stream, contentType, file.FileName);
            }

            Response.ContentType = "application/zip";
            Response.Headers.ContentDisposition = "attachment; filename=\"files.zip\"";

            await using var archive = new ZipArchive(Response.Body, ZipArchiveMode.Create, leaveOpen: true);
            foreach (var file in resolvedExchange.Files)
            {
                var handle = await _fileStorageService.OpenReadAsync(file.RelativePath, cancellationToken);
                await using var fileStream = handle.Stream;
                var entry = archive.CreateEntry(file.FileName, CompressionLevel.Fastest);
                await using var entryStream = entry.Open();
                await fileStream.CopyToAsync(entryStream, cancellationToken);
            }

            _logger.LogInformation("[DOWNLOAD] Token={Token}, File=files.zip",
                resolvedExchange.Exchange!.Token);

            return new EmptyResult();
        }
        catch (FileStorageException)
        {
            _logger.LogError("[FS ERROR] Failed to read stored file(s) for Token={Token}", token);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorEnvelope(new ApiError(
                "file_access_error",
                "Failed to read one or more files from storage.")));
        }
    }

    [HttpGet("info/{token}")]
    [ProducesResponseType<FileInfoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ErrorEnvelope>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ErrorEnvelope>(StatusCodes.Status410Gone)]
    [ProducesResponseType<ErrorEnvelope>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FileInfoResponse>> GetInfoAsync(string token, CancellationToken cancellationToken)
    {
        var resolvedExchange = await ResolveActiveExchangeAsync(token, cancellationToken);
        if (resolvedExchange.ErrorResult is not null)
        {
            return resolvedExchange.ErrorResult switch
            {
                NotFoundObjectResult notFound => notFound,
                ObjectResult objectResult => objectResult,
                StatusCodeResult statusCodeResult => statusCodeResult,
                _ => StatusCode(StatusCodes.Status500InternalServerError, new ErrorEnvelope(new ApiError(
                    "unexpected_error",
                    "An unexpected error occurred.")))
            };
        }

        return Ok(new FileInfoResponse(
            resolvedExchange.Exchange!.Token,
            resolvedExchange.Files.Count,
            resolvedExchange.Exchange.ExpiresAt,
            resolvedExchange.Files.Select(static file => file.FileName).ToArray(),
            $"/api/download/{resolvedExchange.Exchange.Token}"));
    }

    private async Task<ResolvedExchangeResult> ResolveActiveExchangeAsync(string token, CancellationToken cancellationToken)
    {
        var exchange = await _cacheService.GetFileExchangeByTokenAsync(token, cancellationToken);
        if (exchange is null)
        {
            _logger.LogWarning("[NOT FOUND] Token={Token}", token);
            return new ResolvedExchangeResult(null, [], NotFound(new ErrorEnvelope(new ApiError(
                "token_not_found",
                "The requested token was not found."))));
        }

        if (exchange.ExpiresAt <= DateTime.UtcNow)
        {
            _logger.LogWarning("[EXPIRED] Token={Token}, ExpiredAt={ExpiredAt}", token, exchange.ExpiresAt);
            return new ResolvedExchangeResult(exchange, [], StatusCode(StatusCodes.Status410Gone, new ErrorEnvelope(new ApiError(
                "token_expired",
                "The requested token has expired."))));
        }

        try
        {
            var files = DeserializeFiles(exchange.PathsToFiles);
            if (files.Count == 0)
            {
                return new ResolvedExchangeResult(exchange, [], StatusCode(StatusCodes.Status500InternalServerError, new ErrorEnvelope(new ApiError(
                    "empty_exchange",
                    "No files are associated with the requested token."))));
            }

            return new ResolvedExchangeResult(exchange, files, null);
        }
        catch (JsonException)
        {
            _logger.LogError("[FS ERROR] Invalid stored metadata for Token={Token}", token);
            return new ResolvedExchangeResult(exchange, [], StatusCode(StatusCodes.Status500InternalServerError, new ErrorEnvelope(new ApiError(
                "invalid_exchange_record",
                "Stored file metadata is invalid."))));
        }
    }

    private static IReadOnlyList<StoredFileMetadata> DeserializeFiles(string pathsToFiles)
        => StoredFileMetadataSerializer.Deserialize(pathsToFiles);

    private static string ResolveContentType(string fileName)
        => ContentTypeProvider.TryGetContentType(fileName, out var contentType)
            ? contentType
            : "application/octet-stream";

    private sealed record ResolvedExchangeResult(
        FileForExchange? Exchange,
        IReadOnlyList<StoredFileMetadata> Files,
        IActionResult? ErrorResult);
}
