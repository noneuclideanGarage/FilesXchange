using FilesXchange.API.Services;

namespace FilesXchange.API.Tests.Services;

public sealed class TokenServiceTests
{
    [Fact]
    public void GenerateToken_ReturnsValidGuid()
    {
        var service = new TokenService();

        var token = service.GenerateToken();

        Assert.True(Guid.TryParse(token, out _));
    }

    [Fact]
    public void GenerateToken_ReturnsDifferentTokens()
    {
        var service = new TokenService();

        var firstToken = service.GenerateToken();
        var secondToken = service.GenerateToken();

        Assert.NotEqual(firstToken, secondToken);
    }
}
