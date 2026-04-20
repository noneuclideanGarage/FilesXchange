namespace FilesXchange.API.Helpers.ServiceObjects.FileStorage;

public sealed record StoredFileReadHandle(
    Stream Stream,
    string FileName,
    long Length);