using IsoDocs.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IsoDocs.Infrastructure.Persistence.Configurations;

public class DelegationConfiguration : IEntityTypeConfiguration<Delegation>
{
    public void Configure(EntityTypeBuilder<Delegation> builder)
    {
        builder.ToTable("Delegations");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Note).HasMaxLength(512);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.DelegatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.DelegateUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DelegatorUserId, x.StartAt, x.EndAt });
        builder.HasIndex(x => x.IsRevoked);
    }
}
