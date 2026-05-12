using Hangfire;
using IsoDocs.Api.Authorization;
using IsoDocs.Api.Hangfire;
using IsoDocs.Api.Middleware;
using IsoDocs.Application;
using IsoDocs.Infrastructure;
using IsoDocs.Infrastructure.Jobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// 註冊各層 DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// API 元件
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "IsoDocs API",
        Version = "v1",
        Description = "ISO 文件管理系統 API"
    });

    // Swagger UI 上提供輸入 Bearer Token 的入口（方便開發者手貼 token 試呼端點）
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Azure AD Access Token (Bearer)。在 SPA 端以 MSAL 取得後填入 Authorization: Bearer <token>。",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Reference = new Microsoft.OpenApi.Models.OpenApiReference
            {
                Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});

// issue #22 [6.3] — Hangfire 是否啟用（需要 DB 連線字串）
var isHangfireEnabled = !string.IsNullOrWhiteSpace(
    builder.Configuration.GetConnectionString(DependencyInjection.DefaultConnectionStringName));

// issue #2 [2.1.1] — Azure AD / Entra ID Bearer Token 認證
// 設計原則：AzureAd:ClientId 為空時跳過註冊，讓本機開發 (無 Azure AD 環境)、CI 整合測試
// (未連 Azure AD 也能起 service)、單元測試不受 [Authorize] 阻擋。整合測試會在
// CustomWebApplicationFactory 中補上假 ClientId，並以 TestAuthHandler 取代 JwtBearer。
var isAzureAdConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["AzureAd:ClientId"]);
if (isAzureAdConfigured)
{
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

    builder.Services.AddAuthorization(options =>
    {
        // 預設策略：安全預設-deny。未掛 [Authorize] 的 controller 仍需 [AllowAnonymous] 才能訪問。
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        // issue #6 [2.2.1] — 為每個已知權限碼註冊一條 Policy，名稱 = 權限碼本身
        options.AddIsoDocsPermissionPolicies();
    });

    // issue #6 [2.2.1] — PermissionAuthorizationHandler 需 Scoped（內部注入了與 DbContext 同生命週期的 IPermissionService）
    builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .WithOrigins(
                  builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                      ?? new[] { "http://localhost:5173" });
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddProblemDetails();

var app = builder.Build();

// 全域例外處理（必須在最前）
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

if (isAzureAdConfigured)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapControllers();

// issue #22 [6.3] — Hangfire Dashboard（僅管理者可存取）與每日逾期稽催排程
if (isHangfireEnabled)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new AdminDashboardAuthorizationFilter() },
        DashboardTitle = "IsoDocs Hangfire Dashboard"
    });

    RecurringJob.AddOrUpdate<OverdueCheckJob>(
        recurringJobId: "overdue-check-daily",
        methodCall: job => job.ExecuteAsync(CancellationToken.None),
        cronExpression: Cron.Daily(hour: 8),   // 每日 08:00 UTC 執行
        options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}

app.Run();

/// <summary>
/// 提供給整合測試使用的 Program 進入點。
/// </summary>
public partial class Program { }
