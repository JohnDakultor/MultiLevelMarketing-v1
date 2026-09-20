using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class AdministratorInvitationConfiguration
    : IEntityTypeConfiguration<AdministratorInvitation>
{
    public void Configure(EntityTypeBuilder<AdministratorInvitation> builder)
    {
        builder.ToTable("AdministratorInvitations");

        builder.HasKey(invitation => invitation.Id);

        builder
            .HasOne<Organization>()
            .WithMany()
            .HasForeignKey(invitation => invitation.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(invitation => invitation.TokenHash).IsUnique();

        builder
            .HasIndex(invitation => new { invitation.OrganizationId, invitation.NormalizedEmail })
            .IsUnique()
            .HasFilter($"\"Status\" = {(int)AdministratorInvitationStatus.Pending}");

        builder.HasIndex(invitation => new
        {
            invitation.OrganizationId,
            invitation.Status,
            invitation.ExpiresAt,
        });

        builder.Property(invitation => invitation.Email).HasMaxLength(256).IsRequired();
        builder
            .Property(invitation => invitation.NormalizedEmail)
            .HasColumnName("NormalizedEmail")
            .HasMaxLength(256)
            .IsRequired();
        builder.Property(invitation => invitation.TokenHash).HasMaxLength(256).IsRequired();
        builder.Property(invitation => invitation.Status).IsRequired();
        builder.Property(invitation => invitation.RevocationReason).HasMaxLength(500);
    }
}
