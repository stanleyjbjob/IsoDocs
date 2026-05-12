using IsoDocs.Application.Auth;
using IsoDocs.Application.Authorization;
using IsoDocs.Application.Attachments;
using IsoDocs.Application.Identity.Roles;
using IsoDocs.Application.Identity.UserRoles;
using IsoDocs.Infrastructure.Auth;
using IsoDocs.Infrastructure.Authorization;
using IsoDocs.Infrastructure.Blob;
using IsoDocs.Infrastructure.Persistence;
using IsoDocs.Infrastructure.Persistence.Interceptors;
using IsoDocs.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IsoDocs.Infrastructure;

/// <summary>
/// Infrastructure 層的 DI 註冊入口。集中註冊 EF Core DbContext 與外部服務。
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

        // issue #26 [7.2] — Azure Blob Storage 附件上傳下載
        var blobConnectionString = configuration["AzureBlob:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(blobConnectionString))
        {
            services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
        }
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();

        // TODO: issue #22 — 註冊 Hangfire
        // TODO: issue #23 — 註冊 Microsoft Graph（提供離職同步所需的 GraphServiceClient）

        return services;
    }
}
