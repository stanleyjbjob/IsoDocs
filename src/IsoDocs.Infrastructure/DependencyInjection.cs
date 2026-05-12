using Azure.Identity;
using IsoDocs.Application.Auth;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Identity.Roles;
using IsoDocs.Application.Identity.UserRoles;
using IsoDocs.Application.Notifications;
using IsoDocs.Infrastructure.Auth;
using IsoDocs.Infrastructure.Authorization;
using IsoDocs.Infrastructure.Notifications;
using IsoDocs.Infrastructure.Persistence;
using IsoDocs.Infrastructure.Persistence.Interceptors;
using IsoDocs.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;

namespace IsoDocs.Infrastructure;

/// <summary>
/// Infrastructure 層的 DI 註冊入口。集中註冊 EF Core DbContext 與外部服務（後續：Azure Blob、Hangfire）。
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

        // issue #23 [6.1] — Microsoft Graph SDK（Teams 推播 + Email 通知）
        services.Configure<GraphNotificationSettings>(
            configuration.GetSection(GraphNotificationSettings.SectionName));

        services.AddSingleton<GraphServiceClient>(sp =>
        {
            var cfg = configuration.GetSection(GraphNotificationSettings.SectionName);
            var tenantId = cfg[nameof(GraphNotificationSettings.TenantId)] ?? string.Empty;
            var clientId = cfg[nameof(GraphNotificationSettings.ClientId)] ?? string.Empty;
            var clientSecret = cfg[nameof(GraphNotificationSettings.ClientSecret)] ?? string.Empty;
            var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
            return new GraphServiceClient(credential);
        });

        services.AddScoped<ITeamsNotificationService, GraphTeamsNotificationService>();
        services.AddScoped<IEmailNotificationService, GraphEmailNotificationService>();
        services.AddScoped<INotificationSender, NotificationSender>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // TODO: issue #22 — 註冊 Hangfire
        // TODO: issue #26 — 註冊 Azure Blob Storage

        return services;
    }
}
