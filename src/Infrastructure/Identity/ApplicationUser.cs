using Microsoft.AspNetCore.Identity;

namespace modular_mlm.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public Guid? OrganizationId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
