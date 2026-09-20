namespace modular_mlm.Application.Common.Exceptions;

public sealed class ObjectStorageException(string message, Exception innerException)
    : Exception(message, innerException);
