using IsoDocs.Application.Auth;
using IsoDocs.Application.Users;
using IsoDocs.Infrastructure.Auth;
using IsoDocs.Infrastructure.Graph;
using IsoDocs.Infrastructure.Persistence;
using IsoDocs.Infrastructure.Persistence.Interceptors;
using IsoDocs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IsoDocs.Infrastructure;

/// <summary>
/// Infrastructure 層的 DI 註冊入口。集中註冊 EF Core DbContext 與外部服務（後續：Microsoft Graph、Azure Blob、Hangfire）。
/// </summary>
public static class DependencyInjection
{
    public const string DefaultConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // SaveChanges 攝截器：自動維護 UpdatedAt
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

        // issue #3 [2.3.1] — 邀請成員
        services.AddHttpClient();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IGraphInvitationService, GraphInvitationService>();

        // TODO: issue #22 — 註冊 Hangfire
        // TODO: issue #26 — 註冊 Azure Blob Storage

        return services;
    }
}
