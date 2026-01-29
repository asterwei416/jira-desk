using CallTrackingSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallTrackingSystem.Infrastructure.Data.Configurations;

public class InquirySystemConfiguration : IEntityTypeConfiguration<InquirySystem>
{
    public void Configure(EntityTypeBuilder<InquirySystem> builder)
    {
        builder.ToTable("InquirySystems");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(x => x.IsActive)
            .IsRequired();
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasDatabaseName("IX_InquirySystems_Name");
        
        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_InquirySystems_Active");
    }
}
