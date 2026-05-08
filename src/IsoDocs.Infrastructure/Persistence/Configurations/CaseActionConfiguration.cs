using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IsoDocs.Infrastructure.Persistence.Configurations;

public class CaseActionConfiguration : IEntityTypeConfiguration<CaseAction>
{
    public void Configure(EntityTypeBuilder<CaseAction> builder)
    {
        builder.ToTable("CaseActions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActionType).HasConversion<int>();
        builder.Property(x => x.Comment).HasColumnType("nvarchar(max)");
        builder.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");

        builder.HasOne<Case>()
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<CaseNode>()
            .WithMany()
            .HasForeignKey(x => x.CaseNodeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CaseId, x.ActionAt });
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => x.ActionType);
    }
}
