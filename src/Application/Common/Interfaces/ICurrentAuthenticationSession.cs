namespace modular_mlm.Application.Common.Interfaces;

public interface ICurrentAuthenticationSession
{
    Guid? SessionId { get; }
    void Set(Guid sessionId, TimeSpan lifetime);
    void Clear();
}
