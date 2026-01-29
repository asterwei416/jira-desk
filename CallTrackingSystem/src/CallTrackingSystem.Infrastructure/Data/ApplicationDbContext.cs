using CallTrackingSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace CallTrackingSystem.Infrastructure.Data;

/// <summary>
/// 應用程式資料庫上下文
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSet 屬性
    public DbSet<CallRecord> CallRecords => Set<CallRecord>();
    public DbSet<InquirySystem> InquirySystems => Set<InquirySystem>();
    public DbSet<Handler> Handlers => Set<Handler>();
    public DbSet<HandlerMapping> HandlerMappings => Set<HandlerMapping>();
    public DbSet<ChangeHistory> ChangeHistories => Set<ChangeHistory>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 自動套用所有 IEntityTypeConfiguration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        // SQLite RowVersion 處理（避免 NOT NULL 插入失敗）
        if (Database.IsSqlite())
        {
            modelBuilder.Entity<CallTrackingSystem.Core.Entities.CallRecord>()
                .Property(x => x.RowVersion)
                .HasDefaultValueSql("randomblob(8)")
                .IsConcurrencyToken()
                .ValueGeneratedOnAddOrUpdate();
        }
    }
}
