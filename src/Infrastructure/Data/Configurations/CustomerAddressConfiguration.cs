using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Identity;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("CustomerAddresses");

        builder
            .Property(address => address.Label)
            .HasMaxLength(CustomerAddress.MaximumLabelLength)
            .IsRequired();
        builder
            .Property(address => address.RecipientName)
            .HasMaxLength(CustomerAddress.MaximumRecipientNameLength)
            .IsRequired();
        builder
            .Property(address => address.PhoneNumber)
            .HasMaxLength(CustomerAddress.MaximumPhoneNumberLength)
            .IsRequired();
        builder
            .Property(address => address.AddressLine1)
            .HasMaxLength(CustomerAddress.MaximumAddressLineLength)
            .IsRequired();
        builder
            .Property(address => address.AddressLine2)
            .HasMaxLength(CustomerAddress.MaximumAddressLineLength);
        builder
            .Property(address => address.Barangay)
            .HasMaxLength(CustomerAddress.MaximumBarangayLength);
        builder
            .Property(address => address.CityOrMunicipality)
            .HasMaxLength(CustomerAddress.MaximumLocalityLength)
            .IsRequired();
        builder
            .Property(address => address.Province)
            .HasMaxLength(CustomerAddress.MaximumLocalityLength)
            .IsRequired();
        builder
            .Property(address => address.PostalCode)
            .HasMaxLength(CustomerAddress.MaximumPostalCodeLength)
            .IsRequired();
        builder.Property(address => address.CountryCode).HasMaxLength(3).IsRequired();
        builder.Property(address => address.IsActive).HasDefaultValue(true).IsRequired();

        builder
            .HasOne<CustomerProfile>()
            .WithMany(profile => profile.Addresses)
            .HasForeignKey(address => new { address.CustomerProfileId, address.OrganizationId })
            .HasPrincipalKey(profile => new { profile.Id, profile.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasIndex(address => new
            {
                address.OrganizationId,
                address.CustomerProfileId,
                address.IsActive,
            })
            .HasDatabaseName("IX_CustomerAddresses_Org_Profile_Active");
    }
}
