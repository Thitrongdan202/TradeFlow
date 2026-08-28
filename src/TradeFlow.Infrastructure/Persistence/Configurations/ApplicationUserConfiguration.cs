using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FullName)
            .HasMaxLength(200);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(Domain.Enums.UserStatus.Active);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256);

        builder.Property(x => x.EncryptedPassword)
            .HasMaxLength(1000);

        builder.HasMany(x => x.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .IsRequired();

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
    }
}
