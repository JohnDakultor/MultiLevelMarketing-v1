namespace modular_mlm.Application.Common.Interfaces;

public interface IUser
{
    string? Id { get; }
    List<string>? Roles { get; }

    Guid UserId => Guid.TryParse(Id, out var parsedGuid) ? parsedGuid : Guid.Empty;
    Guid? OrganizationId { get; }
    bool IsAdmin => Roles?.Contains("Administrator") ?? false;
    bool IsAgent => Roles?.Contains("Agent") ?? false;
}
