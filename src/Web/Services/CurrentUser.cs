using System.Security.Claims;
using modular_mlm.Application.Common.Interfaces;

namespace modular_mlm.Web.Services;

public class CurrentUser : IUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? Id => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public List<string>? Roles =>
        _httpContextAccessor
            .HttpContext?.User?.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .ToList();

    public Guid? OrganizationId =>
        Guid.TryParse(User?.FindFirstValue("organization_id"), out var orgId) ? orgId : null;
}
