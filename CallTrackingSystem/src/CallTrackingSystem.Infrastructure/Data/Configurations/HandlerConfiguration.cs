using CallTrackingSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallTrackingSystem.Infrastructure.Data.Configurations;

public class HandlerConfiguration : IEntityTypeConfiguration<Handler>
{
    public void Configure(EntityTypeBuilder<Handler> builder)
    {
        builder.ToTable("Handlers");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.LineUserId)
            .HasMaxLength(100);
        
        builder.Property(x => x.IsActive)
            .IsRequired();
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        builder.HasIndex(x => x.LineUserId)
            .IsUnique()
            .HasDatabaseName("IX_Handlers_LineUserId")
            .HasFilter("[LineUserId] IS NOT NULL");
        
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_Handlers_Active");
    }
}
