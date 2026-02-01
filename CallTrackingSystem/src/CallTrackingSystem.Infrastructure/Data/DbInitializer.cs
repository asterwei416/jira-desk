using CallTrackingSystem.Core.Entities;
using CallTrackingSystem.Core.Enums;

namespace CallTrackingSystem.Infrastructure.Data;

/// <summary>
/// 資料庫初始化與種子資料
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// 初始化資料庫並植入種子資料
    /// </summary>
    public static void Initialize(ApplicationDbContext context)
    {
        // 確保資料庫已建立
        context.Database.EnsureCreated();

        // 逐項補齊種子資料（避免部分資料存在時整體被跳過）
        SeedInquirySystems(context);
        SeedUsers(context);
        SeedHandlers(context);
        SeedHandlerMappings(context);
    }
    
    private static void SeedInquirySystems(ApplicationDbContext context)
    {
        if (context.InquirySystems.Any())
        {
            return;
        }

        var systems = new[]
        {
            InquirySystem.Create("Google 表單系統"),
            InquirySystem.Create("Email 系統"),
            InquirySystem.Create("客戶管理系統"),
            InquirySystem.Create("財務系統"),
            InquirySystem.Create("其他")
        };
        
        context.InquirySystems.AddRange(systems);
        context.SaveChanges();
    }
    
    private static void SeedUsers(ApplicationDbContext context)
    {
        // 預設管理者帳號
        // 使用者名稱: admin
        // 密碼: Admin@123
        var admin = context.Users.FirstOrDefault(u => u.Username.ToLower() == "admin");
        if (admin == null)
        {
            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            admin = User.Create("admin", adminPasswordHash, "系統管理員", UserRole.Admin);
            context.Users.Add(admin);
        }
        else
        {
            // Development 初始化：確保可直接登入測試
            admin.ChangePassword(BCrypt.Net.BCrypt.HashPassword("Admin@123"));
        }

        // 預設員工帳號
        // 使用者名稱: staff
        // 密碼: Staff@123
        var staff = context.Users.FirstOrDefault(u => u.Username.ToLower() == "staff");
        if (staff == null)
        {
            var staffPasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123");
            staff = User.Create("staff", staffPasswordHash, "客服人員", UserRole.Staff);
            context.Users.Add(staff);
        }
        else
        {
            staff.ChangePassword(BCrypt.Net.BCrypt.HashPassword("Staff@123"));
        }

        context.SaveChanges();
    }
    
    private static void SeedHandlers(ApplicationDbContext context)
    {
        if (context.Handlers.Any())
        {
            return;
        }

        var handlers = new[]
        {
            Handler.Create("張小明"),
            Handler.Create("李小華"),
            Handler.Create("王大同"),
            Handler.Create("陳美麗"),
            Handler.Create("林志明")
        };
        
        context.Handlers.AddRange(handlers);
        context.SaveChanges();
    }
    
    private static void SeedHandlerMappings(ApplicationDbContext context)
    {
        if (context.HandlerMappings.Any())
        {
            return;
        }

        // 取得已建立的系統和處理人員
        var systems = context.InquirySystems.ToList();
        var handlers = context.Handlers.ToList();
        
        // 為每個系統分配處理人員（示範資料）
        var mappings = new List<HandlerMapping>();
        
        // Google 表單系統 -> 張小明、李小華
        if (systems.Count > 0 && handlers.Count > 0)
        {
            mappings.Add(HandlerMapping.Create(handlers[0].Id, systems[0].Id));
            if (handlers.Count > 1)
                mappings.Add(HandlerMapping.Create(handlers[1].Id, systems[0].Id));
        }
        
        // Email 系統 -> 李小華、王大同
        if (systems.Count > 1 && handlers.Count > 1)
        {
            mappings.Add(HandlerMapping.Create(handlers[1].Id, systems[1].Id));
            if (handlers.Count > 2)
                mappings.Add(HandlerMapping.Create(handlers[2].Id, systems[1].Id));
        }
        
        // 客戶管理系統 -> 王大同、陳美麗
        if (systems.Count > 2 && handlers.Count > 2)
        {
            mappings.Add(HandlerMapping.Create(handlers[2].Id, systems[2].Id));
            if (handlers.Count > 3)
                mappings.Add(HandlerMapping.Create(handlers[3].Id, systems[2].Id));
        }
        
        // 財務系統 -> 陳美麗、林志明
        if (systems.Count > 3 && handlers.Count > 3)
        {
            mappings.Add(HandlerMapping.Create(handlers[3].Id, systems[3].Id));
            if (handlers.Count > 4)
                mappings.Add(HandlerMapping.Create(handlers[4].Id, systems[3].Id));
        }
        
        // 其他 -> 所有人員
        if (systems.Count > 4 && handlers.Count > 0)
        {
            foreach (var handler in handlers)
            {
                mappings.Add(HandlerMapping.Create(handler.Id, systems[4].Id));
            }
        }
        
        context.HandlerMappings.AddRange(mappings);
        context.SaveChanges();
    }
}
