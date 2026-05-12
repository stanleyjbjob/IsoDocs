using FluentAssertions;
using IsoDocs.Application.Cases.Comments.Commands;
using IsoDocs.Application.Cases.Comments.Events;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Common;
using MediatR;
using NSubstitute;
using Xunit;

namespace IsoDocs.Application.UnitTests.Cases.Comments;

public class AddCommentCommandHandlerTests
{
    private static readonly Guid CaseId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task Add_To_Existing_Case_Should_Persist_And_Return_Dto()
    {
        var repo = new FakeCommentRepository();
        repo.SeedCase(CaseId);
        var publisher = Substitute.For<IPublisher>();
        var handler = new AddCommentCommandHandler(repo, publisher);
        var cmd = new AddCommentCommand(CaseId, UserId, "測試留言", null);

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Should().NotBeNull();
        dto.CaseId.Should().Be(CaseId);
        dto.AuthorUserId.Should().Be(UserId);
        dto.Body.Should().Be("測試留言");
        dto.ParentCommentId.Should().BeNull();
        await publisher.Received(1).Publish(
            Arg.Any<NewCommentCreatedNotification>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_To_Nonexistent_Case_Should_Throw_DomainException()
    {
        var repo = new FakeCommentRepository();
        var publisher = Substitute.For<IPublisher>();
        var handler = new AddCommentCommandHandler(repo, publisher);
        var cmd = new AddCommentCommand(Guid.NewGuid(), UserId, "留言", null);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("COMMENT/CASE_NOT_FOUND");
    }

    [Fact]
    public async Task Add_With_Parent_Comment_Should_Set_ParentCommentId()
    {
        var repo = new FakeCommentRepository();
        repo.SeedCase(CaseId);
        var parentId = Guid.NewGuid();
        var publisher = Substitute.For<IPublisher>();
        var handler = new AddCommentCommandHandler(repo, publisher);
        var cmd = new AddCommentCommand(CaseId, UserId, "回覆留言", parentId);

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.ParentCommentId.Should().Be(parentId);
    }

    [Fact]
    public async Task Add_Should_Not_Publish_When_Save_Fails()
    {
        var repo = new FakeCommentRepository();
        repo.SeedCase(CaseId);
        var publisher = Substitute.For<IPublisher>();
        var handler = new AddCommentCommandHandler(repo, publisher);
        var cmd = new AddCommentCommand(CaseId, UserId, "留言", null);

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Id.Should().NotBeEmpty();
        dto.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
