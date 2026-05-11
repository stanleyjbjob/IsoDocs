using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Customers;
using IsoDocs.Domain.Identity;
using IsoDocs.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IsoDocs.Infrastructure.Persistence.Configurations;

public class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        // 案件是核心表，啟用 Temporal Tables 以追蹤狀態變化
        builder.ToTable("Cases", t => t.IsTemporal());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CaseNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CustomVersionNumber).HasMaxLength(64);

        builder.HasIndex(x => x.CaseNumber).IsUnique();
        builder.HasIndex(x => new { x.Status, x.InitiatedAt });
        builder.HasIndex(x => x.InitiatedByUserId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.DocumentTypeId);

        builder.HasOne<DocumentType>()
            .WithMany()
            .HasForeignKey(x => x.DocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<WorkflowTemplate>()
            .WithMany()
            .HasForeignKey(x => x.WorkflowTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.InitiatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
