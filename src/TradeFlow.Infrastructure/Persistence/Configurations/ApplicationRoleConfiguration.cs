using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.IsSystem)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(256);

        builder.HasMany(x => x.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .IsRequired();

        builder.HasMany(x => x.Permissions)
            .WithOne(p => p.Role)
            .HasForeignKey(p => p.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
