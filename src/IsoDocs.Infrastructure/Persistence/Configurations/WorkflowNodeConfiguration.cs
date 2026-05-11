using IsoDocs.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IsoDocs.Infrastructure.Persistence.Configurations;

public class WorkflowNodeConfiguration : IEntityTypeConfiguration<WorkflowNode>
{
    public void Configure(EntityTypeBuilder<WorkflowNode> builder)
    {
        builder.ToTable("WorkflowNodes");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.NodeType).HasConversion<int>();
        builder.Property(x => x.ConfigJson).HasColumnType("nvarchar(max)");

        builder.HasOne<WorkflowTemplate>()
            .WithMany()
            .HasForeignKey(x => x.WorkflowTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.WorkflowTemplateId, x.TemplateVersion, x.NodeOrder }).IsUnique();
    }
}
