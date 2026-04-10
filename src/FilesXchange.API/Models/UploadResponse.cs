namespace FilesXchange.API.Models;

public record UploadResponse(IList<string> Filenames, string Token);
