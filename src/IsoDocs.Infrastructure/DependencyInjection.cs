using IsoDocs.Application.Auth;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Communications;
using IsoDocs.Application.Identity.Roles;
using IsoDocs.Application.Identity.UserRoles;
using IsoDocs.Infrastructure.Auth;
using IsoDocs.Infrastructure.Authorization;
using IsoDocs.Infrastructure.Persistence;
using IsoDocs.Infrastructure.Persistence.Interceptors;
using IsoDocs.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IsoDocs.Infrastructure;

/// <summary>
/// Infrastructure 層的 DI 註冊入口。
/// </summary>
public static class DependencyInjection
{
    public const string DefaultConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContext<IsoDocsDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString(DefaultConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
                return;

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

        // issue #24 [6.2] — 通知事件觸發與通知中心
        services.AddScoped<INotificationRepository, NotificationRepository>();

        // TODO: issue #22 — 註冊 Hangfire
        // TODO: issue #23 — 註冊 Microsoft Graph
        // TODO: issue #26 — 註冊 Azure Blob Storage

        return services;
    }
}
