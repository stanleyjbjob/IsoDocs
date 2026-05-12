using FluentAssertions;
using IsoDocs.Application.Notifications;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Communications;
using IsoDocs.Infrastructure.Jobs;
using IsoDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace IsoDocs.Application.UnitTests.Jobs;

public class OverdueCheckJobTests
{
    private static IsoDocsDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<IsoDocsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new IsoDocsDbContext(opts);
    }

    private static Case BuildOverdueCase(Guid id)
    {
        var past = DateTimeOffset.UtcNow.AddDays(-1);
        return new Case(id, $"CASE-{id.ToString()[..8]}", "Test Case",
            Guid.NewGuid(), Guid.NewGuid(), 1, 1, Guid.NewGuid(), past, null);
    }

    private static CaseNode BuildActiveNode(Guid caseId, Guid assigneeId)
    {
        return new CaseNode(Guid.NewGuid(), caseId, Guid.NewGuid(), 1, "審核節點", assigneeId, null);
    }

    [Fact]
    public async Task ExecuteAsync_NoOverdueCases_SendsNoNotifications()
    {
        using var db = CreateDb();
        var sender = Substitute.For<INotificationSender>();
        var repo = Substitute.For<INotificationRepository>();
        var job = new OverdueCheckJob(db, sender, repo, NullLogger<OverdueCheckJob>.Instance);

        await job.ExecuteAsync();

        await sender.DidNotReceive().SendAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_OverdueCaseWithActiveNode_SendsThreeChannelNotifications()
    {
        using var db = CreateDb();
        var caseId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        var overdueCase = BuildOverdueCase(caseId);
        var node = BuildActiveNode(caseId, assigneeId);
        // Manually accept the node so it becomes InProgress
        node.Accept(assigneeId);

        db.Cases.Add(overdueCase);
        db.CaseNodes.Add(node);
        await db.SaveChangesAsync();

        var sender = Substitute.For<INotificationSender>();
        var repo = Substitute.For<INotificationRepository>();
        var job = new OverdueCheckJob(db, sender, repo, NullLogger<OverdueCheckJob>.Instance);

        await job.ExecuteAsync();

        // 每個逾期節點發送 3 個通道通知（InApp, Email, Teams）
        await sender.Received(3).SendAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await repo.Received(3).AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_CaseNotOverdue_SendsNoNotifications()
    {
        using var db = CreateDb();
        var caseId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        // 建立未逾期的案件（預計完成時間在未來）
        var futureCase = new Case(
            caseId, "CASE-FUTURE", "Future Case",
            Guid.NewGuid(), Guid.NewGuid(), 1, 1, Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(30), null);
        var node = BuildActiveNode(caseId, assigneeId);
        node.Accept(assigneeId);

        db.Cases.Add(futureCase);
        db.CaseNodes.Add(node);
        await db.SaveChangesAsync();

        var sender = Substitute.For<INotificationSender>();
        var repo = Substitute.For<INotificationRepository>();
        var job = new OverdueCheckJob(db, sender, repo, NullLogger<OverdueCheckJob>.Instance);

        await job.ExecuteAsync();

        await sender.DidNotReceive().SendAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_OverdueCaseWithNoAssignee_SendsNoNotifications()
    {
        using var db = CreateDb();
        var caseId = Guid.NewGuid();

        var overdueCase = BuildOverdueCase(caseId);
        // 節點沒有承辦人
        var node = new CaseNode(Guid.NewGuid(), caseId, Guid.NewGuid(), 1, "待指派節點", null, null);

        db.Cases.Add(overdueCase);
        db.CaseNodes.Add(node);
        await db.SaveChangesAsync();

        var sender = Substitute.For<INotificationSender>();
        var repo = Substitute.For<INotificationRepository>();
        var job = new OverdueCheckJob(db, sender, repo, NullLogger<OverdueCheckJob>.Instance);

        await job.ExecuteAsync();

        await sender.DidNotReceive().SendAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SendFails_MarksNotificationFailed_AndContinues()
    {
        using var db = CreateDb();
        var caseId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();

        var overdueCase = BuildOverdueCase(caseId);
        var node = BuildActiveNode(caseId, assigneeId);
        node.Accept(assigneeId);

        db.Cases.Add(overdueCase);
        db.CaseNodes.Add(node);
        await db.SaveChangesAsync();

        var sender = Substitute.For<INotificationSender>();
        sender.SendAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
              .Returns(_ => Task.FromException(new InvalidOperationException("Teams API unavailable")));

        var capturedNotifications = new List<Notification>();
        var repo = Substitute.For<INotificationRepository>();
        repo.AddAsync(Arg.Do<Notification>(n => capturedNotifications.Add(n)), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var job = new OverdueCheckJob(db, sender, repo, NullLogger<OverdueCheckJob>.Instance);

        // 不應拋出例外
        await job.ExecuteAsync();

        // 所有通知都標記為失敗
        capturedNotifications.Should().HaveCount(3);
        capturedNotifications.Should().AllSatisfy(n =>
        {
            n.RetryCount.Should().Be(1);
            n.LastError.Should().Contain("Teams API unavailable");
            n.SentAt.Should().BeNull();
        });
    }
}
