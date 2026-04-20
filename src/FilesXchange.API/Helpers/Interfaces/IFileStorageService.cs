using FilesXchange.API.Helpers.ServiceObjects.FileStorage;

namespace FilesXchange.API.Helpers.Interfaces;

public interface IFileStorageService
{
    Task<IReadOnlyList<StoredFileDescriptor>> SaveFilesAsync(
        IEnumerable<IFormFile> files,
        CancellationToken cancellationToken = default);

    Task<StoredFileReadHandle> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    Task DeleteFileAsync(
        string relativePath,
        CancellationToken cancellationToken = default);

    Task DeleteFilesDirectoryAsync(
        IEnumerable<string> relativePaths,
        CancellationToken cancellationToken = default);
}