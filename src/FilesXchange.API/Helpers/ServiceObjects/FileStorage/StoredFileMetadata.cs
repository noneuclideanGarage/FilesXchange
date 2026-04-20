using System.Text.Json;
using FilesXchange.API.Helpers.ServiceObjects.FileStorage;

public sealed record StoredFileMetadata(
    string RelativePath,
    string FileName);

public static class StoredFileMetadataSerializer
{
    public static string Serialize(IEnumerable<StoredFileDescriptor> files)
        => JsonSerializer.Serialize(files.Select(static file => new StoredFileMetadata(file.RelativePath, file.FileName)));

    public static IReadOnlyList<StoredFileMetadata> Deserialize(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return [];
        }

        using var document = JsonDocument.Parse(payload);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var items = new List<StoredFileMetadata>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    {
                        var relativePath = element.GetString();
                        if (string.IsNullOrWhiteSpace(relativePath))
                        {
                            continue;
                        }

                        items.Add(new StoredFileMetadata(
                            relativePath,
                            Path.GetFileName(relativePath) ?? string.Empty));
                        break;
                    }
                case JsonValueKind.Object:
                    {
                        var relativePath = TryReadProperty(element, "relativePath") ?? TryReadProperty(element, "RelativePath");
                        if (string.IsNullOrWhiteSpace(relativePath))
                        {
                            continue;
                        }

                        var fileName = TryReadProperty(element, "fileName")
                                       ?? TryReadProperty(element, "FileName")
                                       ?? Path.GetFileName(relativePath)
                                       ?? string.Empty;

                        items.Add(new StoredFileMetadata(relativePath, fileName));
                        break;
                    }
            }
        }

        return items;
    }

    private static string? TryReadProperty(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
}