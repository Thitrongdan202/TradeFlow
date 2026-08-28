using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TradeFlow.Infrastructure.Persistence.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.RoleId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(x => x.Resource)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Action)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.IsGranted)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.GrantedBy)
            .HasMaxLength(256);

        // Unique constraint: one row per role+resource+action
        builder.HasIndex(x => new { x.RoleId, x.Resource, x.Action })
            .IsUnique();

        builder.HasOne(x => x.Role)
            .WithMany(r => r.Permissions)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
