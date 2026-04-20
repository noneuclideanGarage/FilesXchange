namespace FilesXchange.API.Helpers.Exceptions;


public sealed class FileStorageException
    (string message, Exception? innerException = null)
    : Exception(message, innerException);
