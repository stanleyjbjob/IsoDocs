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

        // CaseNodeId: NoAction（不是 SetNull）。
        // 原因：Cases → CaseNodes (Cascade) 與 Cases → CaseActions (Cascade) 並存，
        // 若這條 FK 設 SetNull，SQL Server 會以 1785（multiple cascade paths）拒絕建表。
        // 實務上 Case 採軟廢（Status = Voided），不會 hard-delete，NoAction 不影響語意。
        // 詳情見 docs/database.md §10.3。
        builder.HasOne<CaseNode>()
            .WithMany()
            .HasForeignKey(x => x.CaseNodeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CaseId, x.ActionAt });
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => x.ActionType);
    }
}
