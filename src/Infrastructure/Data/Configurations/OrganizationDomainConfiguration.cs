using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Organizations;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class OrganizationDomainConfiguration : IEntityTypeConfiguration<OrganizationDomain>
{
    public void Configure(EntityTypeBuilder<OrganizationDomain> builder)
    {
        builder.ToTable("OrganizationDomains");
        builder.HasKey(domain => domain.Id);
        builder.Property(domain => domain.HostName).HasMaxLength(253).IsRequired();
        builder.Property(domain => domain.CreatedAt).IsRequired();
        builder.Property(domain => domain.VerificationToken).HasMaxLength(64).IsRequired();
        builder.Ignore(domain => domain.VerificationRecordName);
        builder.Ignore(domain => domain.IsVerified);

        builder
            .HasIndex(domain => domain.HostName)
            .IsUnique()
            .HasDatabaseName(
                modular_mlm.Application.Common.Persistence.DatabaseConstraintNames.OrganizationDomainHostName
            );
        builder
            .HasIndex(domain => domain.OrganizationId)
            .IsUnique()
            .HasFilter("\"IsPrimary\" = TRUE");

        builder
            .HasOne<Organization>()
            .WithMany(organization => organization.Domains)
            .HasForeignKey(domain => domain.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
