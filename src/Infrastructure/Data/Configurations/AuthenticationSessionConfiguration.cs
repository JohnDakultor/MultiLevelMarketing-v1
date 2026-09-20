using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Infrastructure.Identity;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class AuthenticationSessionConfiguration
    : IEntityTypeConfiguration<AuthenticationSession>
{
    public void Configure(EntityTypeBuilder<AuthenticationSession> builder)
    {
        builder.ToTable("AuthenticationSessions");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.UserId).HasMaxLength(450).IsRequired();
        builder
            .Property(session => session.TokenFamilyHash)
            .HasMaxLength(AuthenticationSession.TokenFamilyHashLength)
            .IsRequired();
        builder
            .Property(session => session.RevocationReason)
            .HasMaxLength(AuthenticationSession.MaximumRevocationReasonLength);
        builder.Property(session => session.Version).IsRowVersion();
        builder.HasIndex(session => session.TokenFamilyHash).IsUnique();
        builder.HasIndex(session => new { session.UserId, session.ExpiresAt });
        builder.HasIndex(session => session.RevokedAt);
        builder
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
