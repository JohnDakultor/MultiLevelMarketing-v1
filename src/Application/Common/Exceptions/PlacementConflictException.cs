namespace modular_mlm.Application.Common.Exceptions;

public sealed class PlacementConflictException(
    string message =
        "The placement changed or the selected slot is no longer available. Refresh and retry."
) : Exception(message);
