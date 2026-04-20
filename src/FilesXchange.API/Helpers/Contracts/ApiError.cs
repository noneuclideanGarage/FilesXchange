namespace FilesXchange.API.Helpers.Contracts;

public sealed record ApiError(
    string Code,
    string Message);