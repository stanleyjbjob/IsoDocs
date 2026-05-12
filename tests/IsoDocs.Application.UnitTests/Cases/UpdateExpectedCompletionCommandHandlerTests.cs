using FluentAssertions;
using IsoDocs.Application.Cases.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Common;
using Xunit;

namespace IsoDocs.Application.UnitTests.Cases;

public class UpdateExpectedCompletionCommandHandlerTests
{
    private static Case MakeCase(DateTimeOffset? expectedAt = null)
        => new(Guid.NewGuid(), "ITCT-F01-260001", "測試案件",
            Guid.NewGuid(), Guid.NewGuid(), 1, 1, Guid.NewGuid(), expectedAt, null);

    private static CaseNode MakeNode(Guid caseId, bool inProgress = false)
    {
        var node = new CaseNode(Guid.NewGuid(), caseId, Guid.NewGuid(), 1, "處理節點",
            Guid.NewGuid(), null);
        if (inProgress) node.Accept(Guid.NewGuid());
        return node;
    }

    [Fact]
    public async Task FirstTime_Setting_Writes_OriginalExpectedAt()
    {
        var @case = MakeCase(expectedAt: null);
        var repo = new FakeCaseRepository();
        repo.SeedCase(@case);
        var handler = new UpdateExpectedCompletionCommandHandler(repo);
        var expected = DateTimeOffset.UtcNow.AddDays(7);

        var dto = await handler.Handle(new UpdateExpectedCompletionCommand(@case.Id, expected), CancellationToken.None);

        @case.OriginalExpectedAt.Should().NotBeNull();
        @case.ExpectedCompletionAt.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
        @case.OriginalExpectedAt.Should().BeCloseTo(expected, TimeSpan.FromSeconds(1));
        dto.OriginalExpectedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SubsequentModification_DoesNotChange_OriginalExpectedAt()
    {
        var originalDate = DateTimeOffset.UtcNow.AddDays(5);
        var @case = MakeCase(expectedAt: originalDate);
        var repo = new FakeCaseRepository();
        repo.SeedCase(@case);
        var handler = new UpdateExpectedCompletionCommandHandler(repo);
        var newDate = DateTimeOffset.UtcNow.AddDays(10);

        await handler.Handle(new UpdateExpectedCompletionCommand(@case.Id, newDate), CancellationToken.None);

        @case.OriginalExpectedAt.Should().BeCloseTo(originalDate, TimeSpan.FromSeconds(1));
        @case.ExpectedCompletionAt.Should().BeCloseTo(newDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SubsequentModification_WithActiveNode_Sets_ModifiedExpectedAt()
    {
        var @case = MakeCase(expectedAt: DateTimeOffset.UtcNow.AddDays(5));
        var node = MakeNode(@case.Id, inProgress: true);
        var repo = new FakeCaseRepository();
        repo.SeedCase(@case);
        repo.SeedNode(node);
        var handler = new UpdateExpectedCompletionCommandHandler(repo);
        var newDate = DateTimeOffset.UtcNow.AddDays(10);

        await handler.Handle(new UpdateExpectedCompletionCommand(@case.Id, newDate), CancellationToken.None);

        node.ModifiedExpectedAt.Should().BeCloseTo(newDate, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task SubsequentModification_NoActiveNode_DoesNotThrow()
    {
        var @case = MakeCase(expectedAt: DateTimeOffset.UtcNow.AddDays(5));
        var repo = new FakeCaseRepository();
        repo.SeedCase(@case);
        var handler = new UpdateExpectedCompletionCommandHandler(repo);

        var act = async () => await handler.Handle(
            new UpdateExpectedCompletionCommand(@case.Id, DateTimeOffset.UtcNow.AddDays(10)),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CaseNotFound_Throws_DomainException()
    {
        var repo = new FakeCaseRepository();
        var handler = new UpdateExpectedCompletionCommandHandler(repo);

        var act = async () => await handler.Handle(
            new UpdateExpectedCompletionCommand(Guid.NewGuid(), DateTimeOffset.UtcNow.AddDays(5)),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("case.not_found");
    }
}
