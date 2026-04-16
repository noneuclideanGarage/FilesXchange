using FilesXchange.API.Data;
using FilesXchange.API.Helpers.Interfaces;
using FilesXchange.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FilesXchange.API.Services;

public class CacheService : ICacheService
{
    private readonly FilesXchangeDbContext _context;
    private readonly IMemoryCache _memoryCache;

    public CacheService(FilesXchangeDbContext context, IMemoryCache memoryCache)
    {
        _context = context;
        _memoryCache = memoryCache;
    }
    public async Task<FileForExchange?> GetFileExchangeByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        if (_memoryCache.TryGetValue<FileForExchange>(token, out var cachedExchange))
        {
            if (cachedExchange!.ExpiresAt <= DateTime.UtcNow)
            {
                _memoryCache.Remove(token);
                return cachedExchange;
            }

            return cachedExchange;
        }

        var fileExchange = await _context.Files
            .AsNoTracking()
            .FirstOrDefaultAsync(exchange => exchange.Token == token, cancellationToken);

        if (fileExchange is null)
        {
            return null;
        }

        SetFileExchange(fileExchange);
        return fileExchange;
    }

    public void RemoveFileExchange(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        _memoryCache.Remove(token);
    }

    public void SetFileExchange(FileForExchange fileExchange)
    {
        var ttl = fileExchange.ExpiresAt - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            _memoryCache.Remove(fileExchange.Token);
            return;
        }

        var cacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        };

        _memoryCache.Set(fileExchange.Token, fileExchange, cacheEntryOptions);
    }
}