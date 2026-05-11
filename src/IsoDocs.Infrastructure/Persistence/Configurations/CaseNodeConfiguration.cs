using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Identity;
using IsoDocs.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IsoDocs.Infrastructure.Persistence.Configurations;

public class CaseNodeConfiguration : IEntityTypeConfiguration<CaseNode>
{
    public void Configure(EntityTypeBuilder<CaseNode> builder)
    {
        builder.ToTable("CaseNodes", t => t.IsTemporal());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NodeName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasOne<Case>()
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<WorkflowNode>()
            .WithMany()
            .HasForeignKey(x => x.WorkflowNodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AssigneeUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CaseId, x.NodeOrder });
        builder.HasIndex(x => new { x.AssigneeUserId, x.Status });
        builder.HasIndex(x => x.Status);
    }
}
