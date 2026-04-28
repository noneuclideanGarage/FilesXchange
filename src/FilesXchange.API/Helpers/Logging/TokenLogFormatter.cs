namespace FilesXchange.API.Helpers.Logging;

public static class TokenLogFormatter
{
    public static string Redact(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return "<empty>";
        }

        if (token.Length <= 8)
        {
            return "<redacted>";
        }

        return $"{token[..8]}...{token[^4..]}";
    }
}
