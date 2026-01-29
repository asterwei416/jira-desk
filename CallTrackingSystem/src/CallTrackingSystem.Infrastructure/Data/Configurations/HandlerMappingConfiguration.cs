using CallTrackingSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallTrackingSystem.Infrastructure.Data.Configurations;

public class HandlerMappingConfiguration : IEntityTypeConfiguration<HandlerMapping>
{
    public void Configure(EntityTypeBuilder<HandlerMapping> builder)
    {
        builder.ToTable("HandlerMappings");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        // 唯一約束：同一個 Handler 不能重複對應同一個 InquirySystem
        builder.HasIndex(x => new { x.HandlerId, x.InquirySystemId })
            .IsUnique()
            .HasDatabaseName("IX_HandlerMappings_Unique");
        
        builder.HasOne(x => x.Handler)
            .WithMany(x => x.HandlerMappings)
            .HasForeignKey(x => x.HandlerId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasOne(x => x.InquirySystem)
            .WithMany(x => x.HandlerMappings)
            .HasForeignKey(x => x.InquirySystemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
