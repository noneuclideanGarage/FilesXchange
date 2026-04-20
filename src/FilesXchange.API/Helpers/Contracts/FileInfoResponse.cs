namespace FilesXchange.API.Helpers.Contracts;

public sealed record FileInfoResponse(
    string Token,
    int FileCount,
    DateTime ExpiresAt,
    IReadOnlyList<string> FileNames,
    string DownloadUrl);