using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using modular_mlm.Infrastructure.BackgroundJobs;

namespace modular_mlm.Infrastructure.Data.Configurations;

public sealed class DurableBackgroundJobConfiguration
    : IEntityTypeConfiguration<DurableBackgroundJob>
{
    public void Configure(EntityTypeBuilder<DurableBackgroundJob> builder)
    {
        builder.ToTable(
            "DurableBackgroundJobs",
            table =>
                table.HasCheckConstraint(
                    "CK_DurableBackgroundJobs_FinalState",
                    "NOT (\"CompletedAt\" IS NOT NULL AND \"DeadLetteredAt\" IS NOT NULL)"
                )
        );
        builder.HasKey(job => job.Id);
        builder.Property(job => job.JobName).HasMaxLength(100).IsRequired();
        builder.Property(job => job.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(job => job.IdempotencyKey).HasMaxLength(200).IsRequired();
        builder.Property(job => job.ClaimedBy).HasMaxLength(200);
        builder.Property(job => job.LastError).HasMaxLength(2_000);
        builder
            .HasIndex(job => new
            {
                job.OrganizationId,
                job.JobName,
                job.IdempotencyKey,
            })
            .IsUnique()
            .HasFilter("\"OrganizationId\" IS NOT NULL")
            .HasDatabaseName("UX_DurableBackgroundJobs_Tenant_Job_Key");
        builder
            .HasIndex(job => new { job.JobName, job.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"OrganizationId\" IS NULL")
            .HasDatabaseName("UX_DurableBackgroundJobs_Global_Job_Key");
        builder.HasIndex(job => new
        {
            job.CompletedAt,
            job.DeadLetteredAt,
            job.NextAttemptAt,
        });
    }
}
