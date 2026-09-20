namespace modular_mlm.Domain.Exceptions;

public sealed class DomainInvariantException(string message) : Exception(message);
