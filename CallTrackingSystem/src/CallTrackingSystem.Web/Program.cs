using System.Text;
using System.Text.Json;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using CallTrackingSystem.Infrastructure.Data;
using CallTrackingSystem.Infrastructure.Repositories;
using CallTrackingSystem.Infrastructure.Services;
using CallTrackingSystem.Web.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        // 設定 JSON 序列化選項
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    });

builder.Services.AddHttpClient();
builder.Services.AddHttpClient<ILineMessagingApiClient, LineMessagingApiClient>();
builder.Services.AddResponseCaching();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // 啟用 XML 註解
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// JWT & Cookie 認證
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "JWT_OR_COOKIE";
        options.DefaultChallengeScheme = "JWT_OR_COOKIE";
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "CallTrackingSystem.Auth";
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException("JWT SecretKey 未設定");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    })
    .AddPolicyScheme("JWT_OR_COOKIE", "JWT_OR_COOKIE", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            // 如果 Authorization Header 存在且以此開頭，使用 JWT
            string authorization = context.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return JwtBearerDefaults.AuthenticationScheme;
            }

            // 否則預設使用 Cookie (Web 瀏覽器)
            return CookieAuthenticationDefaults.AuthenticationScheme;
        };
    });

// 授權原則
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy =>
        policy.RequireRole(UserRole.Admin.ToString()));
    options.AddPolicy("Staff", policy =>
        policy.RequireRole(UserRole.Staff.ToString()));
});

// 註冊資料庫上下文 (SQLite)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 註冊 Repositories
builder.Services.AddScoped<ICallRecordRepository, CallRecordRepository>();
builder.Services.AddScoped<IInquirySystemRepository, InquirySystemRepository>();
builder.Services.AddScoped<IHandlerRepository, HandlerRepository>();
builder.Services.AddScoped<IChangeHistoryRepository, ChangeHistoryRepository>();
builder.Services.AddScoped<IHandlerMappingRepository, HandlerMappingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
builder.Services.AddScoped<CallTrackingSystem.Infrastructure.Services.ILineMessagingClient, CallTrackingSystem.Infrastructure.Services.LineMessagingClientWrapper>();

// 註冊 Services
builder.Services.AddScoped<CallRecordService>();
builder.Services.AddScoped<IEditLockManager, EditLockManager>();
builder.Services.AddScoped<IChangeHistoryService, ChangeHistoryService>();
builder.Services.AddScoped<INotificationLogService, NotificationLogService>();
builder.Services.AddScoped<IInquirySystemService, InquirySystemService>();
builder.Services.AddScoped<IHandlerService, HandlerService>();
builder.Services.AddScoped<IHandlerMappingService, HandlerMappingService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILineLoginService, LineLoginService>();
builder.Services.AddScoped<ILineNotificationService, CallTrackingSystem.Infrastructure.Services.LineNotificationService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IReportService, ReportService>();

// 註冊背景服務
builder.Services.AddHostedService<EditLockCleanupService>();

// 註冊健康檢查
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>(
        name: "資料庫連線",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql" });

var app = builder.Build();

// 初始化資料庫與種子資料（Development）
if (app.Environment.IsDevelopment())
{
    try
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        DbInitializer.Initialize(dbContext);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "資料庫初始化/種子資料建立失敗");
        throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseMiddleware<GlobalExceptionHandler>();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=()";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; style-src 'self' https://cdn.jsdelivr.net 'unsafe-inline'; img-src 'self' data:;";
    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseResponseCaching();

// 啟用 Controllers
app.MapControllers();

// MVC 頁面路由
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=CallRecord}/{action=Index}/{id?}");

// 健康檢查端點
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        }, new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        
        await context.Response.WriteAsync(result);
    }
});

app.Run();
