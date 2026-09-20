namespace modular_mlm.Application.Common.Interfaces;

public interface ICartSessionAccessor
{
    string GetOrCreateSessionId();
    string? GetSessionId();
    void ClearSession();
}
