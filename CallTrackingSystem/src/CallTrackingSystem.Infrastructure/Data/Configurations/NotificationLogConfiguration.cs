using CallTrackingSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallTrackingSystem.Infrastructure.Data.Configurations;

public class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.LineUserId)
            .IsRequired()
            .HasMaxLength(100);
        
        builder.Property(x => x.MessageType)
            .HasConversion<int>()
            .IsRequired();
        
        builder.Property(x => x.Success)
            .IsRequired();
        
        builder.Property(x => x.ErrorMessage)
            .HasMaxLength(1000);
        
        builder.Property(x => x.SentAt)
            .IsRequired();
        
        builder.HasOne(x => x.CallRecord)
            .WithMany(x => x.NotificationLogs)
            .HasForeignKey(x => x.CallRecordId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // 索引：依來電紀錄查詢通知記錄
        builder.HasIndex(x => x.CallRecordId)
            .HasDatabaseName("IX_NotificationLogs_CallRecord");
        
        // 索引：查詢失敗的通知（供管理者追蹤）
        builder.HasIndex(x => new { x.Success, x.SentAt })
            .HasDatabaseName("IX_NotificationLogs_Failure")
            .HasFilter("[Success] = 0");
    }
}
