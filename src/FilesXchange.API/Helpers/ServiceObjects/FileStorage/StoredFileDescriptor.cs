namespace FilesXchange.API.Helpers.ServiceObjects.FileStorage;

public sealed record StoredFileDescriptor(
    string RelativePath,
    string FileName,
    long Length);