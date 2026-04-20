namespace FilesXchange.API.Helpers.Exceptions;

public sealed class FileExchangeCreationException
    (string message, Exception? innerException = null)
    : Exception(message, innerException);