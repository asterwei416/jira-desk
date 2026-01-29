using CallTrackingSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallTrackingSystem.Infrastructure.Data.Configurations;

public class CallRecordConfiguration : IEntityTypeConfiguration<CallRecord>
{
    public void Configure(EntityTypeBuilder<CallRecord> builder)
    {
        builder.ToTable("CallRecords");
        builder.HasKey(x => x.Id);
        
        // 欄位設定
        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.Content)
            .IsRequired()
            .HasMaxLength(150);
        
        builder.Property(x => x.ContactName)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.ContactPhone)
            .IsRequired()
            .HasMaxLength(20);
        
        builder.Property(x => x.FaqReference)
            .HasMaxLength(500);
        
        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(x => x.UrgencyLevel)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(x => x.CreatedByUserId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.LockedByUserId)
            .HasMaxLength(50);
        
        builder.Property(x => x.CreatedAt)
            .IsRequired();
        
        builder.Property(x => x.UpdatedAt)
            .IsRequired();
        
        // 並發控制
        builder.Property(x => x.RowVersion)
            .IsRowVersion();
        
        // 關聯設定
        builder.HasOne(x => x.InquirySystem)
            .WithMany(x => x.CallRecords)
            .HasForeignKey(x => x.InquirySystemId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasMany(x => x.Handlers)
            .WithMany(x => x.CallRecords)
            .UsingEntity(join => join.ToTable("CallRecordHandlers"));
        
        // 索引策略
        builder.HasIndex(x => new { x.CreatedAt, x.Status, x.UrgencyLevel })
            .HasDatabaseName("IX_CallRecords_Search")
            .IsDescending(true, false, false);
        
        builder.HasIndex(x => x.InquirySystemId)
            .HasDatabaseName("IX_CallRecords_InquirySystem");
        
        builder.HasIndex(x => x.LockedByUserId)
            .HasDatabaseName("IX_CallRecords_Lock")
            .HasFilter("[LockedByUserId] IS NOT NULL");
    }
}
