using System.Text;
using FilesXchange.API.Helpers.Exceptions;
using FilesXchange.API.Helpers.Interfaces;
using FilesXchange.API.Helpers.ServiceObjects.FileStorage;
using FilesXchange.API.Options;
using Microsoft.Extensions.Options;

namespace FilesXchange.API.Services;

public sealed class FileStorageService : IFileStorageService
{
    private static readonly HashSet<char> PortableInvalidFileNameCharacters =
    [
        '<', '>', ':', '"', '/', '\\', '|', '?', '*'
    ];

    private static readonly HashSet<string> ReservedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private readonly string _contentRootPath;
    private readonly string _rootDirectory;
    private readonly string _rootDirectoryPrefix;
    private readonly string _storedPathPrefix;
    private readonly int _maxFileNameLength;

    public FileStorageService(IOptions<AppOptions> options, IWebHostEnvironment environment)
    {
        var configuredDirectory = options.Value.UploadDirectory;
        _maxFileNameLength = Math.Max(8, options.Value.MaxFileNameLength);
        _contentRootPath = environment.ContentRootPath;

        if (Path.IsPathRooted(configuredDirectory))
        {
            _rootDirectory = Path.GetFullPath(configuredDirectory);
            _storedPathPrefix = NormalizeRelativeDirectory(Path.GetFileName(_rootDirectory)) switch
            {
                "" => "uploads",
                var prefix => prefix
            };
        }
        else
        {
            _storedPathPrefix = NormalizeRelativeDirectory(configuredDirectory);
            _rootDirectory = Path.GetFullPath(Path.Combine(_contentRootPath, _storedPathPrefix));
        }

        _rootDirectoryPrefix = _rootDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;

        Directory.CreateDirectory(_rootDirectory);
    }

    public async Task<IReadOnlyList<StoredFileDescriptor>> SaveFilesAsync(
        IEnumerable<IFormFile> files,
        CancellationToken cancellationToken = default)
    {
        var fileList = files.ToList();
        var internalId = Guid.NewGuid().ToString();
        var relativeDirectory = CombineRelativePath(_storedPathPrefix, internalId);
        var absoluteDirectory = ResolveDirectoryPath(relativeDirectory);

        Directory.CreateDirectory(absoluteDirectory);

        var storedFiles = new List<StoredFileDescriptor>(fileList.Count);
        var reservedStorageNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var reservedDownloadNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (var file in fileList)
            {
                var storedFileName = CreateUniqueStoredFileName(file.FileName, absoluteDirectory, reservedStorageNames, _maxFileNameLength);
                var downloadFileName = CreateUniqueDownloadFileName(file.FileName, reservedDownloadNames, _maxFileNameLength);
                var relativePath = CombineRelativePath(relativeDirectory, storedFileName);
                var absolutePath = ResolveFilePath(relativePath);

                await using var sourceStream = file.OpenReadStream();

                try
                {
                    await using var destinationStream = new FileStream(
                        absolutePath,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 81920,
                        useAsync: true);

                    await sourceStream.CopyToAsync(destinationStream, cancellationToken);
                }
                catch (Exception exception)
                {
                    SafeDeleteFile(absolutePath);
                    throw new FileStorageException($"Failed to write file '{storedFileName}' to storage.", exception);
                }

                storedFiles.Add(new StoredFileDescriptor(relativePath, downloadFileName, file.Length));
            }

            return storedFiles;
        }
        catch
        {
            SafeDeleteDirectory(absoluteDirectory);
            throw;
        }
    }

    public Task<StoredFileReadHandle> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var absolutePath = ResolveFilePath(relativePath);
        if (!File.Exists(absolutePath))
        {
            throw new FileStorageException($"Stored file was not found: '{relativePath}'.");
        }

        try
        {
            var stream = new FileStream(
                absolutePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);

            var fileInfo = new FileInfo(absolutePath);
            return Task.FromResult(new StoredFileReadHandle(stream, Path.GetFileName(absolutePath), fileInfo.Length));
        }
        catch (Exception exception)
        {
            throw new FileStorageException($"Failed to open stored file '{relativePath}'.", exception);
        }
    }

    public Task DeleteFileAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var absolutePath = ResolveFilePath(relativePath);
        SafeDeleteFile(absolutePath);

        return Task.CompletedTask;
    }

    public Task DeleteFilesDirectoryAsync(
        IEnumerable<string> relativePaths,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var directories = relativePaths
            .Select(Path.GetDirectoryName)
            .Where(static directory => !string.IsNullOrWhiteSpace(directory))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in directories)
        {
            var absoluteDirectory = ResolveDirectoryPath(directory!);
            SafeDeleteDirectory(absoluteDirectory);
        }

        return Task.CompletedTask;
    }

    private string ResolveFilePath(string relativePath)
    {
        var rootRelativePath = GetPathRelativeToStorageRoot(relativePath);
        var absolutePath = Path.GetFullPath(Path.Combine(_rootDirectory, rootRelativePath));

        EnsureWithinStorageRoot(absolutePath);
        return absolutePath;
    }

    private string ResolveDirectoryPath(string relativePath)
    {
        var rootRelativePath = GetPathRelativeToStorageRoot(relativePath);
        var absolutePath = Path.GetFullPath(Path.Combine(_rootDirectory, rootRelativePath));

        EnsureWithinStorageRoot(absolutePath);
        return absolutePath;
    }

    private string GetPathRelativeToStorageRoot(string path)
    {
        var normalizedPath = NormalizeRelativePath(path);
        var normalizedPrefix = NormalizeRelativeDirectory(_storedPathPrefix);

        if (string.IsNullOrWhiteSpace(normalizedPath) || normalizedPath.Equals(normalizedPrefix, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        if (normalizedPath.StartsWith($"{normalizedPrefix}/", StringComparison.Ordinal))
        {
            return normalizedPath[(normalizedPrefix.Length + 1)..];
        }

        return normalizedPath;
    }

    private void EnsureWithinStorageRoot(string absolutePath)
    {
        if (absolutePath.Equals(_rootDirectory, StringComparison.Ordinal))
        {
            return;
        }

        if (!absolutePath.StartsWith(_rootDirectoryPrefix, StringComparison.Ordinal))
        {
            throw new FileStorageException("Resolved path points outside the storage root.");
        }
    }

    private static string CreateUniqueStoredFileName(
        string originalFileName,
        string absoluteDirectory,
        ISet<string> reservedNames,
        int maxFileNameLength)
    {
        var sanitizedFileName = SanitizeFileName(originalFileName, maxFileNameLength);
        var extension = Path.GetExtension(sanitizedFileName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sanitizedFileName);
        var candidate = sanitizedFileName;
        var suffix = 1;

        while (reservedNames.Contains(candidate) || File.Exists(Path.Combine(absoluteDirectory, candidate)))
        {
            candidate = BuildFileName(fileNameWithoutExtension, extension, $" ({suffix})", maxFileNameLength);
            suffix++;
        }

        reservedNames.Add(candidate);
        return candidate;
    }

    private static string CreateUniqueDownloadFileName(
        string originalFileName,
        ISet<string> reservedNames,
        int maxFileNameLength)
    {
        var downloadFileName = SanitizeDownloadFileName(originalFileName, maxFileNameLength);
        var extension = Path.GetExtension(downloadFileName);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(downloadFileName);
        var candidate = downloadFileName;
        var suffix = 1;

        while (reservedNames.Contains(candidate))
        {
            candidate = BuildFileName(fileNameWithoutExtension, extension, $" ({suffix})", maxFileNameLength);
            suffix++;
        }

        reservedNames.Add(candidate);
        return candidate;
    }

    private static string SanitizeFileName(string fileName, int maxFileNameLength)
    {
        var leafName = ExtractLeafName(fileName);
        if (string.IsNullOrWhiteSpace(leafName))
        {
            return "file";
        }

        var normalizedLeafName = leafName.Normalize(NormalizationForm.FormKC);
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitizedCharacters = normalizedLeafName
            .Select(character => ShouldReplaceCharacter(character, invalidCharacters) ? '_' : character)
            .ToArray();

        var sanitized = CollapseWhitespace(new string(sanitizedCharacters)).Trim().Trim('.');
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "file";
        }

        var extension = Path.GetExtension(sanitized);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized).TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
        {
            fileNameWithoutExtension = "file";
        }

        if (ReservedFileNames.Contains(fileNameWithoutExtension))
        {
            fileNameWithoutExtension = $"{fileNameWithoutExtension}_";
        }

        extension = TrimExtension(extension);
        return BuildFileName(fileNameWithoutExtension, extension, string.Empty, maxFileNameLength);
    }

    private static string ExtractLeafName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return string.Empty;
        }

        var normalized = fileName.Replace('\\', '/');
        var lastSeparatorIndex = normalized.LastIndexOf('/');
        return lastSeparatorIndex >= 0 ? normalized[(lastSeparatorIndex + 1)..] : normalized;
    }

    private static string SanitizeDownloadFileName(string fileName, int maxFileNameLength)
    {
        var leafName = ExtractLeafName(fileName);
        if (string.IsNullOrWhiteSpace(leafName))
        {
            return "file";
        }

        var normalizedLeafName = leafName.Normalize(NormalizationForm.FormKC);
        var sanitizedCharacters = normalizedLeafName
            .Select(static character => character is '/' or '\\' || char.IsControl(character) ? '_' : character)
            .ToArray();

        var sanitized = CollapseWhitespace(new string(sanitizedCharacters)).Trim().Trim('.');
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "file";
        }

        var extension = TrimExtension(Path.GetExtension(sanitized));
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sanitized).TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
        {
            fileNameWithoutExtension = "file";
        }

        return BuildFileName(fileNameWithoutExtension, extension, string.Empty, maxFileNameLength);
    }

    private static bool ShouldReplaceCharacter(char character, char[] invalidCharacters)
        => character == '/'
           || character == '\\'
           || char.IsControl(character)
           || PortableInvalidFileNameCharacters.Contains(character)
           || invalidCharacters.Contains(character);

    private static string CollapseWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasWhitespace = false;

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        return builder.ToString();
    }

    private static string TrimExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        return extension.TrimEnd('.', ' ');
    }

    private static string BuildFileName(
        string fileNameWithoutExtension,
        string extension,
        string suffix,
        int maxFileNameLength)
    {
        var normalizedExtension = extension;
        if (!string.IsNullOrEmpty(normalizedExtension) && !normalizedExtension.StartsWith(".", StringComparison.Ordinal))
        {
            normalizedExtension = $".{normalizedExtension}";
        }

        var reservedLength = suffix.Length + normalizedExtension.Length;
        var maxBaseLength = Math.Max(1, maxFileNameLength - reservedLength);
        var truncatedBase = fileNameWithoutExtension.Length > maxBaseLength
            ? fileNameWithoutExtension[..maxBaseLength]
            : fileNameWithoutExtension;

        truncatedBase = truncatedBase.TrimEnd('.', ' ');
        if (string.IsNullOrWhiteSpace(truncatedBase))
        {
            truncatedBase = "file";
        }

        var candidate = $"{truncatedBase}{suffix}{normalizedExtension}";
        return candidate.Length > maxFileNameLength
            ? candidate[..maxFileNameLength].TrimEnd('.', ' ')
            : candidate;
    }

    private static string NormalizeRelativeDirectory(string path)
    {
        var normalized = NormalizeRelativePath(path);
        return normalized.TrimEnd('/');
    }

    private static string NormalizeRelativePath(string path)
    {
        var normalized = path.Replace('\\', '/').Trim();
        normalized = normalized.TrimStart('/');

        while (normalized.Contains("//", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
        }

        return normalized;
    }

    private static string CombineRelativePath(string left, string right)
        => $"{NormalizeRelativeDirectory(left)}/{NormalizeRelativePath(right)}";

    private static void SafeDeleteFile(string absolutePath)
    {
        try
        {
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
        }
        catch
        {
            // Cleanup failure should not hide the original storage error.
        }
    }

    private static void SafeDeleteDirectory(string absoluteDirectory)
    {
        try
        {
            if (Directory.Exists(absoluteDirectory))
            {
                Directory.Delete(absoluteDirectory, recursive: true);
            }
        }
        catch
        {
            // Cleanup failure should not hide the original storage error.
        }
    }
}