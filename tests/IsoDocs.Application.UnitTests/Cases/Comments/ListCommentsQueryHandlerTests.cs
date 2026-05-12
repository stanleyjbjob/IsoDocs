using FluentAssertions;
using IsoDocs.Application.Cases.Comments.Queries;
using IsoDocs.Application.UnitTests.Fakes;
using Xunit;

namespace IsoDocs.Application.UnitTests.Cases.Comments;

public class ListCommentsQueryHandlerTests
{
    private static readonly Guid CaseId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public async Task List_Returns_All_Active_Comments_For_Case()
    {
        var repo = new FakeCommentRepository();
        repo.Seed(CaseId, UserId, "第一則");
        repo.Seed(CaseId, UserId, "第二則");
        var handler = new ListCommentsQueryHandler(repo);

        var result = await handler.Handle(new ListCommentsQuery(CaseId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(c => c.Body).Should().BeEquivalentTo(new[] { "第一則", "第二則" });
    }

    [Fact]
    public async Task List_Returns_Empty_For_Case_With_No_Comments()
    {
        var repo = new FakeCommentRepository();
        var handler = new ListCommentsQueryHandler(repo);

        var result = await handler.Handle(new ListCommentsQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task List_Excludes_Soft_Deleted_Comments()
    {
        var repo = new FakeCommentRepository();
        var deleted = repo.Seed(CaseId, UserId, "已刪除");
        deleted.SoftDelete();
        repo.Seed(CaseId, UserId, "正常留言");
        var handler = new ListCommentsQueryHandler(repo);

        var result = await handler.Handle(new ListCommentsQuery(CaseId), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Body.Should().Be("正常留言");
    }

    [Fact]
    public async Task List_Maps_All_Dto_Fields_Correctly()
    {
        var repo = new FakeCommentRepository();
        repo.Seed(CaseId, UserId, "DTO 模擬測試");
        var handler = new ListCommentsQueryHandler(repo);

        var result = await handler.Handle(new ListCommentsQuery(CaseId), CancellationToken.None);

        var dto = result.Single();
        dto.CaseId.Should().Be(CaseId);
        dto.AuthorUserId.Should().Be(UserId);
        dto.Body.Should().Be("DTO 模擬測試");
        dto.ParentCommentId.Should().BeNull();
        dto.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
