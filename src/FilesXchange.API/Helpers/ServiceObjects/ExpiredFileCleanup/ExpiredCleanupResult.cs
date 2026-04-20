namespace FilesXchange.API.Helpers.ServiceObjects.ExpiredFileCleanup;

public sealed record ExpiredCleanupResult(
    int ExpiredFound,
    int Removed,
    int Failed);