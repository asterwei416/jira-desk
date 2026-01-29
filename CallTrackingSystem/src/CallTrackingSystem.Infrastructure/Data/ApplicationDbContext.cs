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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 將在 Phase 2 套用所有 IEntityTypeConfiguration
        // modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
