using FluentAssertions;
using IsoDocs.Application.Cases.Queries;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Cases;

namespace IsoDocs.Application.UnitTests.Cases;

public sealed class GetCaseRelationsQueryTests
{
    private static CaseRelation MakeRelation(Guid parentId, Guid childId,
        CaseRelationType type = CaseRelationType.Subprocess)
        => new(Guid.NewGuid(), parentId, childId, type, Guid.NewGuid());

    [Fact]
    public async Task Handle_ReturnsBothParentAndChildRelations()
    {
        var caseId = Guid.NewGuid();
        var otherId1 = Guid.NewGuid();
        var otherId2 = Guid.NewGuid();
        var unrelatedId = Guid.NewGuid();

        var repo = new FakeCaseRelationRepository();
        // caseId 作為父：有一個子流程
        repo.Seed(MakeRelation(caseId, otherId1));
        // caseId 作為子：屬於另一個父
        repo.Seed(MakeRelation(otherId2, caseId));
        // 與此案件無關的關聯（不應出現）
        repo.Seed(MakeRelation(otherId1, unrelatedId));

        var handler = new GetCaseRelationsQueryHandler(repo);
        var result = await handler.Handle(new GetCaseRelationsQuery(caseId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().Contain(r => r.ParentCaseId == caseId && r.ChildCaseId == otherId1);
        result.Should().Contain(r => r.ParentCaseId == otherId2 && r.ChildCaseId == caseId);
    }

    [Fact]
    public async Task Handle_NoRelations_ReturnsEmpty()
    {
        var handler = new GetCaseRelationsQueryHandler(new FakeCaseRelationRepository());
        var result = await handler.Handle(new GetCaseRelationsQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RelationTypeLabelMappedCorrectly()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var repo = new FakeCaseRelationRepository();
        repo.Seed(MakeRelation(parentId, childId, CaseRelationType.Subprocess));
        repo.Seed(MakeRelation(parentId, Guid.NewGuid(), CaseRelationType.Reopen));
        repo.Seed(MakeRelation(parentId, Guid.NewGuid(), CaseRelationType.Reference));

        var handler = new GetCaseRelationsQueryHandler(repo);
        var result = await handler.Handle(new GetCaseRelationsQuery(parentId), CancellationToken.None);

        result.Should().Contain(r => r.RelationType == CaseRelationType.Subprocess && r.RelationTypeLabel == "子流程");
        result.Should().Contain(r => r.RelationType == CaseRelationType.Reopen    && r.RelationTypeLabel == "重開新案");
        result.Should().Contain(r => r.RelationType == CaseRelationType.Reference  && r.RelationTypeLabel == "參考關聯");
    }
}
