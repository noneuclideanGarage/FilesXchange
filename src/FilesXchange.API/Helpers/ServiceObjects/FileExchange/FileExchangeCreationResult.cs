using FilesXchange.API.Helpers.ServiceObjects.FileStorage;
using FilesXchange.API.Models;

namespace FilesXchange.API.Helpers.ServiceObjects.FileExchange;

public sealed record FileExchangeCreationResult(
    FileForExchange Exchange,
    IReadOnlyList<StoredFileDescriptor> StoredFiles);