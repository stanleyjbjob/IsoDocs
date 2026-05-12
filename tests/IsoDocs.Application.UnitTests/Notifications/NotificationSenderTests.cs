using FluentAssertions;
using IsoDocs.Application.Notifications;
using IsoDocs.Domain.Communications;
using IsoDocs.Infrastructure.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace IsoDocs.Application.UnitTests.Notifications;

public sealed class NotificationSenderTests
{
    private readonly ITeamsNotificationService _teams = Substitute.For<ITeamsNotificationService>();
    private readonly IEmailNotificationService _email = Substitute.For<IEmailNotificationService>();
    private readonly INotificationRepository _repo = Substitute.For<INotificationRepository>();
    private readonly GraphNotificationSettings _settings = new()
    {
        MaxRetryCount = 1,
        RetryDelayMs = 0
    };

    private NotificationSender CreateSut() => new(
        _teams, _email, _repo,
        Options.Create(_settings),
        NullLogger<NotificationSender>.Instance);

    private static NotificationRequest BuildRequest(
        NotificationChannel[]? channels = null) => new(
        RecipientUserId: Guid.NewGuid(),
        RecipientEmail: "user@example.com",
        RecipientAzureAdObjectId: "aad-obj-id",
        Type: NotificationType.NodeAssigned,
        Subject: "測試主旨",
        Body: "測試內文",
        Channels: channels);

    [Fact]
    public async Task SendAsync_TeamsAndEmail_CallsBothServices()
    {
        var sut = CreateSut();
        var request = BuildRequest(new[] { NotificationChannel.Teams, NotificationChannel.Email });

        await sut.SendAsync(request);

        await _teams.Received(1).SendMessageAsync(
            "aad-obj-id", "測試主旨", "測試內文", Arg.Any<CancellationToken>());
        await _email.Received(1).SendEmailAsync(
            "user@example.com", "測試主旨", "測試內文", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_PersistsOneNotificationRecordPerChannel()
    {
        var sut = CreateSut();
        var request = BuildRequest(new[] { NotificationChannel.Teams, NotificationChannel.Email });

        await sut.SendAsync(request);

        await _repo.Received(2).AddAsync(
            Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenTeamsFails_RetriesUpToMaxRetryCount()
    {
        _settings.MaxRetryCount = 2;
        _teams.SendMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
              .ThrowsAsync(new Exception("Teams 連線失敗"));

        var sut = CreateSut();
        var request = BuildRequest(new[] { NotificationChannel.Teams });

        await sut.SendAsync(request);

        // 首次 + MaxRetryCount 次重試 = 3 次呼叫
        await _teams.Received(3).SendMessageAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenEmailSucceeds_SaveChangesCalledOnce()
    {
        var sut = CreateSut();
        var request = BuildRequest(new[] { NotificationChannel.Email });

        await sut.SendAsync(request);

        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_DefaultChannels_SendsTeamsAndEmail()
    {
        var sut = CreateSut();
        var request = BuildRequest(channels: null); // 未指定 → 預設 Teams + Email

        await sut.SendAsync(request);

        await _teams.Received(1).SendMessageAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _email.Received(1).SendEmailAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
