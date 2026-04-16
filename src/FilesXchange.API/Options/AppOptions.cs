using FilesXchange.API.Helpers.Constants;

namespace FilesXchange.API.Options;

public sealed class AppOptions
{
    public const string SectionName = "FilesXchange";

    public long MaxFileSizeBytes { get; set; } = 2 * FileSize.GB;

    public int MaxFilesPerUpload { get; set; } = 100;

    public int MaxFileNameLength { get; set; } = 128;

    public int TokenExpirationDays { get; set; } = 7;

    public int CleanupIntervalSeconds { get; set; } = 300;

    public string UploadDirectory { get; set; } = "uploads";

    public string LogDirectory { get; set; } = "logs";
}