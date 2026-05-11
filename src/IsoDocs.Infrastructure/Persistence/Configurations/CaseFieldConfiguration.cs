using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IsoDocs.Infrastructure.Persistence.Configurations;

public class CaseFieldConfiguration : IEntityTypeConfiguration<CaseField>
{
    public void Configure(EntityTypeBuilder<CaseField> builder)
    {
        builder.ToTable("CaseFields", t => t.IsTemporal());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FieldCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ValueJson).HasColumnType("nvarchar(max)").IsRequired();

        builder.HasOne<Case>()
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<FieldDefinition>()
            .WithMany()
            .HasForeignKey(x => x.FieldDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.CaseId, x.FieldCode }).IsUnique();
    }
}
