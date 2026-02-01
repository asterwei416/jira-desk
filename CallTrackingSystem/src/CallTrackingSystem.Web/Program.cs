using System.Text;
using System.Text.Json;
using CallTrackingSystem.Core.Enums;
using CallTrackingSystem.Core.Interfaces;
using CallTrackingSystem.Core.Services;
using CallTrackingSystem.Infrastructure.Data;
using CallTrackingSystem.Infrastructure.Repositories;
using CallTrackingSystem.Infrastructure.Services;
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

// JWT 認證
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<INotificationLogRepository, NotificationLogRepository>();
builder.Services.AddScoped<CallTrackingSystem.Infrastructure.Services.ILineMessagingClient, CallTrackingSystem.Infrastructure.Services.LineMessagingClientWrapper>();

// 註冊 Services
builder.Services.AddScoped<CallRecordService>();
builder.Services.AddScoped<IEditLockManager, EditLockManager>();
builder.Services.AddScoped<IChangeHistoryService, ChangeHistoryService>();
builder.Services.AddScoped<INotificationLogService, NotificationLogService>();
builder.Services.AddScoped<IUserService, UserService>();
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

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
