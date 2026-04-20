using FilesXchange.API.Helpers.Interfaces;

namespace FilesXchange.API.Services;

public class TokenService : ITokenService
{
    public string GenerateToken()
    {
        return Guid.NewGuid().ToString();
    }
}