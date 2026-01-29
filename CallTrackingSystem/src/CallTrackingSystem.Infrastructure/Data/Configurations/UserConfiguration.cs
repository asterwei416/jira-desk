using CallTrackingSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallTrackingSystem.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Username)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.PasswordHash)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.Role)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(x => x.LineUserId)
            .HasMaxLength(100);
        
        builder.Property(x => x.IsActive)
            .IsRequired();
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        builder.HasIndex(x => x.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username");
        
        builder.HasIndex(x => x.LineUserId)
            .IsUnique()
            .HasDatabaseName("IX_Users_LineUserId")
            .HasFilter("[LineUserId] IS NOT NULL");
    }
}
