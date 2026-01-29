using CallTrackingSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CallTrackingSystem.Infrastructure.Data.Configurations;

public class ChangeHistoryConfiguration : IEntityTypeConfiguration<ChangeHistory>
{
    public void Configure(EntityTypeBuilder<ChangeHistory> builder)
    {
        builder.ToTable("ChangeHistories");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.FieldName)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.OldValue)
            .HasMaxLength(500);
        
        builder.Property(x => x.NewValue)
            .HasMaxLength(500);
        
        builder.Property(x => x.ChangedByUserId)
            .IsRequired()
            .HasMaxLength(50);
        
        builder.Property(x => x.ChangedAt)
            .IsRequired();
        
        builder.HasOne(x => x.CallRecord)
            .WithMany(x => x.ChangeHistories)
            .HasForeignKey(x => x.CallRecordId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // 索引：依來電紀錄和時間查詢變更歷史
        builder.HasIndex(x => new { x.CallRecordId, x.ChangedAt })
            .HasDatabaseName("IX_ChangeHistories_Record");
    }
}
