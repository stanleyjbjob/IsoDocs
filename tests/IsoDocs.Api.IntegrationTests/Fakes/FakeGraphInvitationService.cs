using IsoDocs.Application.Users;

namespace IsoDocs.Api.IntegrationTests.Fakes;

public sealed class FakeGraphInvitationService : IGraphInvitationService
{
    public List<(string Email, string DisplayName)> SentInvitations { get; } = new();
    public bool ShouldFail { get; set; }

    public Task<GraphInvitationResult> InviteGuestAsync(
        string email,
        string displayName,
        string inviteRedirectUrl,
        CancellationToken cancellationToken = default)
    {
        if (ShouldFail)
            throw new InvalidOperationException("Simulated MS Graph failure.");

        SentInvitations.Add((email, displayName));
        return Task.FromResult(new GraphInvitationResult(
            Guid.NewGuid().ToString(),
            $"https://myapp.example.com/accept?email={Uri.EscapeDataString(email)}"));
    }
}
