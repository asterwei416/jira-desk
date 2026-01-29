using Microsoft.EntityFrameworkCore;
using CallTrackingSystem.Infrastructure.Data;
using CallTrackingSystem.Core.Entities;

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlite("Data Source=../../src/CallTrackingSystem.Web/CallTrackingDB_Dev.db");

using var context = new ApplicationDbContext(optionsBuilder.Options);

Console.WriteLine("===== 清除舊的測試資料 =====");
// 清除舊的測試資料（如果存在）
var existingMappings = context.HandlerMappings.ToList();
context.HandlerMappings.RemoveRange(existingMappings);

var existingHandlers = context.Handlers.ToList();
context.Handlers.RemoveRange(existingHandlers);

var existingSystems = context.InquirySystems.ToList();
context.InquirySystems.RemoveRange(existingSystems);

await context.SaveChangesAsync();

Console.WriteLine("===== 建立詢問系統 =====");
var systemNames = new[] { "訂單查詢系統", "會員服務系統", "物流追蹤系統" };
var systems = new List<InquirySystem>();

foreach (var name in systemNames)
{
    var existing = await context.InquirySystems.FirstOrDefaultAsync(s => s.Name == name);
    if (existing == null)
    {
        var newSystem = InquirySystem.Create(name);
        context.InquirySystems.Add(newSystem);
        await context.SaveChangesAsync();
        systems.Add(newSystem);
        Console.WriteLine($"  建立新系統: {name}");
    }
    else
    {
        systems.Add(existing);
        Console.WriteLine($"  系統已存在: {name}");
    }
}

Console.WriteLine("\n===== 建立處理人員 =====");
var lineUserId = "U7d6dea4b033c775bc811eabe558b3607";
var handler = await context.Handlers.FirstOrDefaultAsync(h => h.LineUserId == lineUserId);
if (handler == null)
{
    handler = Handler.Create("測試人員", lineUserId);
    context.Handlers.Add(handler);
    await context.SaveChangesAsync();
    Console.WriteLine($"建立新處理人員: {handler.Name} (LINE User ID: {handler.LineUserId})");
}
else
{
    Console.WriteLine($"處理人員已存在: {handler.Name} (LINE User ID: {handler.LineUserId})");
}

Console.WriteLine("\n===== 建立對應關係 =====");
var existingMapping = await context.HandlerMappings
    .FirstOrDefaultAsync(m => m.HandlerId == handler.Id && m.InquirySystemId == systems[0].Id);
if (existingMapping == null)
{
    var mapping = HandlerMapping.Create(handler.Id, systems[0].Id);
    context.HandlerMappings.Add(mapping);
    await context.SaveChangesAsync();
    Console.WriteLine($"已建立對應: 訂單查詢系統 → 測試人員");
}
else
{
    Console.WriteLine($"對應關係已存在: 訂單查詢系統 → 測試人員");
}

Console.WriteLine("\n===== 驗證資料 =====");
var allSystems = await context.InquirySystems.ToListAsync();
Console.WriteLine($"\n詢問系統 ({allSystems.Count} 筆):");
foreach (var sys in allSystems)
{
    Console.WriteLine($"  [{sys.Id}] {sys.Name} (Active: {sys.IsActive})");
}

var allHandlers = await context.Handlers.ToListAsync();
Console.WriteLine($"\n處理人員 ({allHandlers.Count} 筆):");
foreach (var h in allHandlers)
{
    Console.WriteLine($"  [{h.Id}] {h.Name} - LINE ID: {h.LineUserId}");
}

var allMappings = await context.HandlerMappings
    .Include(m => m.InquirySystem)
    .Include(m => m.Handler)
    .ToListAsync();
Console.WriteLine($"\n對應關係 ({allMappings.Count} 筆):");
foreach (var m in allMappings)
{
    Console.WriteLine($"  [{m.Id}] {m.InquirySystem.Name} → {m.Handler.Name}");
}

Console.WriteLine("\n✅ 測試資料建立完成！");
