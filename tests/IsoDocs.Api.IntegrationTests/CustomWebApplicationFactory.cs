using IsoDocs.Api.IntegrationTests.Fakes;
using IsoDocs.Application.Attachments;
using IsoDocs.Application.Auth;
using IsoDocs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IsoDocs.Api.IntegrationTests;

/// <summary>
/// 整合測試共用 <see cref="WebApplicationFactory{TEntryPoint}"/>：
/// <list type="number">
///   <item>注入假的 AzureAd 設定，啟用 [Authorize] pipeline。</item>
///   <item>替換 <see cref="IUserSyncService"/> 為 <see cref="FakeUserSyncService"/>。</item>
///   <item>替換 <see cref="IBlobStorageService"/> 為 <see cref="FakeBlobStorageService"/>。</item>
///   <item>使用 InMemory EF Core DbContext（避免需要 SQL Server）。</item>
///   <item>把預設 authentication scheme 改為 Test，讓 [Authorize] 走 <see cref="TestAuthHandler"/>。</item>
/// </list>
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"IsoDocs_Test_{Guid.NewGuid()}";

    public FakeUserSyncService UserSync { get; } = new();
    public FakeBlobStorageService BlobStorage { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureAd:Instance"] = "https://login.microsoftonline.com",
                ["AzureAd:TenantId"] = "00000000-0000-0000-0000-000000000001",
                ["AzureAd:ClientId"] = "00000000-0000-0000-0000-000000000002",
                ["AzureAd:Audience"] = "api://00000000-0000-0000-0000-000000000002",
                ["AzureAd:Scopes"] = "access_as_user"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // 替換 DbContext 為 InMemory（避免需要 SQL Server 連線）
            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<IsoDocsDbContext>));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            services.AddDbContext<IsoDocsDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

            // 替換 IUserSyncService 為 Fake
            services.RemoveAll<IUserSyncService>();
            services.AddSingleton<IUserSyncService>(UserSync);

            // 替換 IBlobStorageService 為 Fake
            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton<IBlobStorageService>(BlobStorage);

            // 替換 Authentication 為 Test scheme
            services
                .AddAuthentication(defaultScheme: TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }
}
