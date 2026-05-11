using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using IsoDocs.Application.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IsoDocs.Infrastructure.Graph;

internal sealed class GraphInvitationService : IGraphInvitationService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GraphInvitationService> _logger;

    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";

    public GraphInvitationService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<GraphInvitationService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<GraphInvitationResult> InviteGuestAsync(
        string email,
        string displayName,
        string inviteRedirectUrl,
        CancellationToken cancellationToken = default)
    {
        var token = await AcquireAppTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning(
                "MS Graph app token 不可用（dev/test 模式），為 {Email} 產生替代 OID。", email);
            return new GraphInvitationResult(
                Guid.NewGuid().ToString(),
                $"{inviteRedirectUrl}?email={Uri.EscapeDataString(email)}");
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var body = new
        {
            invitedUserEmailAddress = email,
            invitedUserDisplayName = displayName,
            inviteRedirectUrl,
            sendInvitationMessage = true,
        };

        var response = await client.PostAsJsonAsync(
            $"{GraphBaseUrl}/invitations", body, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GraphInvitationResponse>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("MS Graph 邀請 API 回應為空。");

        return new GraphInvitationResult(
            result.InvitedUser?.Id ?? Guid.NewGuid().ToString(),
            result.InviteRedeemUrl ?? string.Empty);
    }

    private async Task<string?> AcquireAppTokenAsync(CancellationToken ct)
    {
        var tenantId = _configuration["AzureAd:TenantId"];
        var clientId = _configuration["AzureAd:ClientId"];
        var clientSecret = _configuration["AzureAd:ClientSecret"];

        if (string.IsNullOrWhiteSpace(tenantId) ||
            string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret))
        {
            return null;
        }

        var client = _httpClientFactory.CreateClient();
        var tokenUrl = $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
        });

        var response = await client.PostAsync(tokenUrl, form, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("MS Graph token 取得失敗，狀態碼：{Status}", response.StatusCode);
            return null;
        }

        var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
        return tokenResponse?.AccessToken;
    }

    private sealed class GraphInvitationResponse
    {
        [JsonPropertyName("invitedUser")]
        public InvitedUserInfo? InvitedUser { get; set; }

        [JsonPropertyName("inviteRedeemUrl")]
        public string? InviteRedeemUrl { get; set; }
    }

    private sealed class InvitedUserInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}
