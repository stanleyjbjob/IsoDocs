using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Communications;
using IsoDocs.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IsoDocs.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.Channel).HasConversion<int>();
        builder.Property(x => x.Subject).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.LastError).HasColumnType("nvarchar(max)");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Case>()
            .WithMany()
            .HasForeignKey(x => x.CaseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.RecipientUserId, x.IsRead });
        builder.HasIndex(x => x.SentAt);
    }
}
