namespace modular_mlm.Application.Common.Exceptions;

public sealed class IdempotencyConflictException(string message) : Exception(message);
