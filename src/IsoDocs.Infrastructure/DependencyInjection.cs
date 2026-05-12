using Hangfire;
using Hangfire.SqlServer;
using IsoDocs.Application.Auth;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Identity.Roles;
using IsoDocs.Application.Identity.UserRoles;
using IsoDocs.Application.Notifications;
using IsoDocs.Infrastructure.Auth;
using IsoDocs.Infrastructure.Authorization;
using IsoDocs.Infrastructure.Jobs;
using IsoDocs.Infrastructure.Notifications;
using IsoDocs.Infrastructure.Persistence;
using IsoDocs.Infrastructure.Persistence.Interceptors;
using IsoDocs.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IsoDocs.Infrastructure;

/// <summary>
/// Infrastructure 層的 DI 註冊入口。集中註冊 EF Core DbContext 與外部服務（後續：Microsoft Graph、Azure Blob）。
/// </summary>
public static class DependencyInjection
{
    public const string DefaultConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // SaveChanges 攔截器：自動維護 UpdatedAt
        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContext<IsoDocsDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString(DefaultConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // 容許在沒有連線字串的情況下啟動（例如純單元測試或 Swagger 預覽）。
                // 整合測試會用 InMemory 或 Testcontainers 提供連線。
                return;
            }

            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(IsoDocsDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
            });

            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        // issue #2 [2.1.1] — Azure AD 使用者同步服務
        services.AddScoped<IUserSyncService, UserSyncService>();

        // issue #6 [2.2.1] — 自訂角色與 RBAC 權限管理
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IPermissionService, PermissionService>();

        // issue #22 [6.3] — Hangfire 排程與每日逾期稽催通知
        // 僅在有 DB 連線字串時啟用 Hangfire；本機開發或 CI 無 DB 時跳過
        var hangfireConnectionString = configuration.GetConnectionString(DefaultConnectionStringName);
        var hangfireEnabled = !string.IsNullOrWhiteSpace(hangfireConnectionString);
        if (hangfireEnabled)
        {
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(hangfireConnectionString!, new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));

            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 2;
                options.Queues = new[] { "default" };
            });
        }

        // 通知相關服務
        services.AddScoped<INotificationRepository, NotificationRepository>();
        // issue [6.1] 完成後以 MicrosoftGraphNotificationSender 取代此暫用實作
        services.AddScoped<INotificationSender, LoggingNotificationSender>();
        services.AddScoped<OverdueCheckJob>();

        // TODO: issue #23 — 註冊 Microsoft Graph（提供離職同步所需的 GraphServiceClient）
        // TODO: issue #26 — 註冊 Azure Blob Storage

        return services;
    }
}
