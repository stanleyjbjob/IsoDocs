using IsoDocs.Domain.Cases;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IsoDocs.Infrastructure.Persistence.Configurations;

public class CaseRelationConfiguration : IEntityTypeConfiguration<CaseRelation>
{
    public void Configure(EntityTypeBuilder<CaseRelation> builder)
    {
        builder.ToTable("CaseRelations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RelationType).HasConversion<int>();

        builder.HasOne<Case>()
            .WithMany()
            .HasForeignKey(x => x.ParentCaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Case>()
            .WithMany()
            .HasForeignKey(x => x.ChildCaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ParentCaseId, x.ChildCaseId, x.RelationType }).IsUnique();
        builder.HasIndex(x => x.ChildCaseId);
    }
}
