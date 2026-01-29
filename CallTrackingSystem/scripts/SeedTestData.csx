using Microsoft.EntityFrameworkCore;
using CallTrackingSystem.Infrastructure.Data;
using CallTrackingSystem.Core.Entities;

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlite("Data Source=../CallTrackingSystem.Web/CallTrackingDB_Dev.db");

using var context = new ApplicationDbContext(optionsBuilder.Options);

Console.WriteLine("===== 清除舊的測試資料 =====");
// 清除舊的測試資料（如果存在）
var existingMappings = context.HandlerMappings.Where(m => m.Id <= 3).ToList();
context.HandlerMappings.RemoveRange(existingMappings);

var existingHandlers = context.Handlers.Where(h => h.Id <= 3).ToList();
context.Handlers.RemoveRange(existingHandlers);

var existingSystems = context.InquirySystems.Where(s => s.Id <= 3).ToList();
context.InquirySystems.RemoveRange(existingSystems);

await context.SaveChangesAsync();

Console.WriteLine("===== 建立詢問系統 =====");
var systems = new[]
{
    new InquirySystem { Id = 1, SystemName = "訂單查詢系統", IsActive = true },
    new InquirySystem { Id = 2, SystemName = "會員服務系統", IsActive = true },
    new InquirySystem { Id = 3, SystemName = "物流追蹤系統", IsActive = true }
};
context.InquirySystems.AddRange(systems);
await context.SaveChangesAsync();
Console.WriteLine($"已建立 {systems.Length} 個詢問系統");

Console.WriteLine("\n===== 建立處理人員 =====");
var handler = new Handler 
{ 
    Id = 1, 
    HandlerName = "測試人員", 
    LineUserId = "U7d6dea4b033c775bc811eabe558b3607", 
    IsActive = true 
};
context.Handlers.Add(handler);
await context.SaveChangesAsync();
Console.WriteLine($"已建立處理人員: {handler.HandlerName} (LINE User ID: {handler.LineUserId})");

Console.WriteLine("\n===== 建立對應關係 =====");
var mapping = new HandlerMapping 
{ 
    Id = 1, 
    InquirySystemId = 1, 
    HandlerId = 1 
};
context.HandlerMappings.Add(mapping);
await context.SaveChangesAsync();
Console.WriteLine($"已建立對應: 訂單查詢系統 → 測試人員");

Console.WriteLine("\n===== 驗證資料 =====");
var allSystems = await context.InquirySystems.ToListAsync();
Console.WriteLine($"\n詢問系統 ({allSystems.Count} 筆):");
foreach (var sys in allSystems)
{
    Console.WriteLine($"  [{sys.Id}] {sys.SystemName} (Active: {sys.IsActive})");
}

var allHandlers = await context.Handlers.ToListAsync();
Console.WriteLine($"\n處理人員 ({allHandlers.Count} 筆):");
foreach (var h in allHandlers)
{
    Console.WriteLine($"  [{h.Id}] {h.HandlerName} - LINE ID: {h.LineUserId}");
}

var allMappings = await context.HandlerMappings
    .Include(m => m.InquirySystem)
    .Include(m => m.Handler)
    .ToListAsync();
Console.WriteLine($"\n對應關係 ({allMappings.Count} 筆):");
foreach (var m in allMappings)
{
    Console.WriteLine($"  [{m.Id}] {m.InquirySystem.SystemName} → {m.Handler.HandlerName}");
}

Console.WriteLine("\n✅ 測試資料建立完成！");
