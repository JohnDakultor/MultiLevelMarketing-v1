using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Identity;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        builder.ToTable("CustomerProfiles");
        builder.Property(profile => profile.UserId).HasMaxLength(450).IsRequired();
        builder
            .Property(profile => profile.DisplayName)
            .HasMaxLength(CustomerProfile.MaximumDisplayNameLength)
            .IsRequired();
        builder.Property(profile => profile.Email).HasMaxLength(CustomerProfile.MaximumEmailLength);
        builder.Property(profile => profile.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasAlternateKey(profile => new { profile.Id, profile.OrganizationId });
        builder.HasIndex(profile => new { profile.OrganizationId, profile.UserId }).IsUnique();
        builder.HasIndex(profile => new { profile.OrganizationId, profile.Status });
        builder.HasIndex(profile => new { profile.OrganizationId, profile.Email });
    }
}
