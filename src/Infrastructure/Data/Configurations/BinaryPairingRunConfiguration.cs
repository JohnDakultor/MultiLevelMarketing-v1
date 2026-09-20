using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Domain.Compensation;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class BinaryPairingRunConfiguration : IEntityTypeConfiguration<BinaryPairingRun>
{
    public void Configure(EntityTypeBuilder<BinaryPairingRun> builder)
    {
        builder.ToTable("BinaryPairingRuns");
        builder.HasKey(run => run.Id);
        builder.HasIndex(run => new { run.OrganizationId, run.IdempotencyKey }).IsUnique();
        builder.HasIndex(run => new
        {
            run.OrganizationId,
            run.AgentId,
            run.PeriodEnd,
        });
        builder.Property(run => run.IdempotencyKey).HasMaxLength(256);
        builder.Property(run => run.QualificationFailureReason).HasMaxLength(500);
        builder.Property(run => run.LeftBefore).HasPrecision(18, 4);
        builder.Property(run => run.RightBefore).HasPrecision(18, 4);
        builder.Property(run => run.MatchedVolume).HasPrecision(18, 4);
        builder.Property(run => run.LeftConsumed).HasPrecision(18, 4);
        builder.Property(run => run.RightConsumed).HasPrecision(18, 4);
        builder.Property(run => run.LeftAfter).HasPrecision(18, 4);
        builder.Property(run => run.RightAfter).HasPrecision(18, 4);
        builder.Property(run => run.GrossCommission).HasPrecision(18, 2);
        builder.Property(run => run.CappedAmount).HasPrecision(18, 2);
        builder.Property(run => run.NetCommission).HasPrecision(18, 2);
    }
}
