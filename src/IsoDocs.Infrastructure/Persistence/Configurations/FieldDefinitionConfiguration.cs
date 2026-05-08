using IsoDocs.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IsoDocs.Infrastructure.Persistence.Configurations;

public class FieldDefinitionConfiguration : IEntityTypeConfiguration<FieldDefinition>
{
    public void Configure(EntityTypeBuilder<FieldDefinition> builder)
    {
        builder.ToTable("FieldDefinitions", t => t.IsTemporal());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.ValidationJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.OptionsJson).HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.Code, x.Version }).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}
