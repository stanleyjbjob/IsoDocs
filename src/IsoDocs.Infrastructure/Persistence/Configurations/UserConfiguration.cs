using IsoDocs.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IsoDocs.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // 啟用 Temporal Tables，追蹤使用者資料全生命週期變動
        builder.ToTable("Users", t => t.IsTemporal());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AzureAdObjectId).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Department).HasMaxLength(128);
        builder.Property(x => x.JobTitle).HasMaxLength(128);

        builder.HasIndex(x => x.AzureAdObjectId).IsUnique();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.IsActive);
    }
}
