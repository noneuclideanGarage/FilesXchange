using FilesXchange.API.Helpers.ServiceObjects.FileStorage;

namespace FilesXchange.API.Tests.Helpers;

public sealed class StoredFileMetadataSerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_PreservesStoredFileMetadata()
    {
        var files = new[]
        {
            new StoredFileDescriptor("uploads/internal/file-a.txt", "file-a.txt", 12),
            new StoredFileDescriptor("uploads/internal/file-b.txt", "file-b.txt", 34),
        };

        var payload = StoredFileMetadataSerializer.Serialize(files);
        var result = StoredFileMetadataSerializer.Deserialize(payload);

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal("uploads/internal/file-a.txt", first.RelativePath);
                Assert.Equal("file-a.txt", first.FileName);
            },
            second =>
            {
                Assert.Equal("uploads/internal/file-b.txt", second.RelativePath);
                Assert.Equal("file-b.txt", second.FileName);
            });
    }

    [Fact]
    public void Deserialize_SupportsLegacyStringArrayPayload()
    {
        const string payload = "[\"uploads/internal/legacy-a.txt\",\"uploads/internal/legacy-b.txt\"]";

        var result = StoredFileMetadataSerializer.Deserialize(payload);

        Assert.Collection(
            result,
            first =>
            {
                Assert.Equal("uploads/internal/legacy-a.txt", first.RelativePath);
                Assert.Equal("legacy-a.txt", first.FileName);
            },
            second =>
            {
                Assert.Equal("uploads/internal/legacy-b.txt", second.RelativePath);
                Assert.Equal("legacy-b.txt", second.FileName);
            });
    }
}
