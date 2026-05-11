using IsoDocs.Api.IntegrationTests.Fakes;
using IsoDocs.Application.Auth;
using IsoDocs.Application.Users;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IsoDocs.Api.IntegrationTests;

/// <summary>
/// 整合測試共用 <see cref="WebApplicationFactory{TEntryPoint}"/>：
/// <list type="number">
///   <item>注入假的 AzureAd 設定，讓 <c>Program.cs</c> 走進 <c>isAzureAdConfigured = true</c> 分支，啟用 [Authorize] pipeline。</item>
///   <item>替換 <see cref="IUserSyncService"/> 為 <see cref="FakeUserSyncService"/>，避免接 SQL Server。</item>
///   <item>把預設 authentication scheme 改為 <c>Test</c>，並註冊 <see cref="TestAuthHandler"/>，
///       讓 [Authorize] 走測試替身解析 claims。</item>
/// </list>
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeUserSyncService UserSync { get; } = new();
    public FakeUserRepository UserRepo { get; } = new();
    public FakeGraphInvitationService GraphInvitation { get; } = new();

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
            services.RemoveAll<IUserSyncService>();
            services.AddSingleton<IUserSyncService>(UserSync);

            services.RemoveAll<IUserRepository>();
            services.AddSingleton<IUserRepository>(UserRepo);

            services.RemoveAll<IGraphInvitationService>();
            services.AddSingleton<IGraphInvitationService>(GraphInvitation);

            services
                .AddAuthentication(defaultScheme: TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName,
                    _ => { });
        });
    }
}
