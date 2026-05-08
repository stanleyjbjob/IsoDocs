using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IsoDocs.Infrastructure;

/// <summary>
/// Infrastructure 層的 DI 註冊入口。後續會在此註冊 EF Core DbContext、外部服務（Microsoft Graph、Azure Blob、Hangfire）等。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TODO: issue #5 — 註冊 EF Core DbContext
        // TODO: issue #2 — 註冊 Microsoft.Identity.Web
        // TODO: issue #22 — 註冊 Hangfire
        // TODO: issue #23 — 註冊 Microsoft Graph
        // TODO: issue #26 — 註冊 Azure Blob Storage

        _ = configuration; // 暫時保留參數，避免未使用警告

        return services;
    }
}
