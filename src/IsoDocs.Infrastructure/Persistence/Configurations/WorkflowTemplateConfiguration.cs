using IsoDocs.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IsoDocs.Infrastructure.Persistence.Configurations;

public class WorkflowTemplateConfiguration : IEntityTypeConfiguration<WorkflowTemplate>
{
    public void Configure(EntityTypeBuilder<WorkflowTemplate> builder)
    {
        // 範本異動全生命週期需追蹤，啟用 Temporal Tables
        builder.ToTable("WorkflowTemplates", t => t.IsTemporal());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.DefinitionJson).HasColumnType("nvarchar(max)").IsRequired();

        builder.HasIndex(x => new { x.Code, x.Version }).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}
