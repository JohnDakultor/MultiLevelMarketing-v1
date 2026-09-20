using modular_mlm.Application.Common.Auditing;
using modular_mlm.Application.Common.Interfaces;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Application.Organizations.Commands.UpdateCommerceSettings;

public sealed class UpdateCommerceSettingsCommandHandler(
    IApplicationDbContext db,
    IAuditWriter auditWriter
) : IRequestHandler<UpdateCommerceSettingsCommand>
{
    public async Task Handle(
        UpdateCommerceSettingsCommand request,
        CancellationToken cancellationToken
    )
    {
        var organization = await db.Organizations.SingleOrDefaultAsync(
            candidate => candidate.Id == request.OrganizationId,
            cancellationToken
        );
        if (organization is null)
            throw new KeyNotFoundException("Organization was not found.");

        var beforeJson = Serialize(organization.Commerce);
        organization.UpdateCommerceSettings(
            CommerceSettings.Create(
                request.AllowGuestCheckout,
                request.RequireShippingAddress,
                request.RequireBillingAddress,
                request.InventoryReservationMinutes
            )
        );

        var audit = AuditCoverageMap.CommerceSettingsUpdated;
        auditWriter.Write(
            organization.Id,
            audit.Action,
            audit.EntityType,
            organization.Id,
            beforeJson,
            Serialize(organization.Commerce),
            reason: null
        );
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string Serialize(CommerceSettings settings) =>
        AuditJson.Serialize(
            new
            {
                settings.AllowGuestCheckout,
                settings.RequireShippingAddress,
                settings.RequireBillingAddress,
                settings.InventoryReservationMinutes,
            }
        );
}
