namespace FilesXchange.API.Contracts;

public sealed record ApiError(
    string Code,
    string Message);