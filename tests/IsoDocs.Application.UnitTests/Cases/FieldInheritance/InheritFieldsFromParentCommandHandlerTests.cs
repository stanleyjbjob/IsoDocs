using FluentAssertions;
using IsoDocs.Application.Cases.FieldInheritance;
using IsoDocs.Application.UnitTests.Fakes;

namespace IsoDocs.Application.UnitTests.Cases.FieldInheritance;

public class InheritFieldsFromParentCommandHandlerTests
{
    private static readonly Guid ParentId = Guid.NewGuid();
    private static readonly Guid ChildId = Guid.NewGuid();
    private static readonly Guid FieldDef1 = Guid.NewGuid();
    private static readonly Guid FieldDef2 = Guid.NewGuid();
    private static readonly Guid FieldDef3 = Guid.NewGuid();

    private static (InheritFieldsFromParentCommandHandler handler, FakeCaseFieldRepository repo) Build()
    {
        var repo = new FakeCaseFieldRepository();
        return (new InheritFieldsFromParentCommandHandler(repo), repo);
    }

    [Fact]
    public async Task Inherits_Only_Fields_Marked_As_Inheritable()
    {
        var (handler, repo) = Build();
        repo.Seed(ParentId, FieldDef1, "FIELD_A", "\"alpha\"");
        repo.Seed(ParentId, FieldDef2, "FIELD_B", "\"beta\"");
        repo.Seed(ParentId, FieldDef3, "FIELD_C", "\"gamma\"");

        // 只繼承 FieldDef1 與 FieldDef2
        var cmd = new InheritFieldsFromParentCommand(ChildId, ParentId, new[] { FieldDef1, FieldDef2 });
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dto => dto.InheritedFromCaseId.Should().Be(ParentId));
        result.Should().AllSatisfy(dto => dto.CaseId.Should().Be(ChildId));

        var fieldCodes = result.Select(r => r.FieldCode).ToList();
        fieldCodes.Should().BeEquivalentTo(new[] { "FIELD_A", "FIELD_B" });

        // FieldDef3 不應被繼承
        var childFields = repo.Store.Where(f => f.CaseId == ChildId).ToList();
        childFields.Should().HaveCount(2);
        childFields.Should().NotContain(f => f.FieldDefinitionId == FieldDef3);
    }

    [Fact]
    public async Task Inherited_Field_Has_Correct_Value_And_Version()
    {
        var (handler, repo) = Build();
        repo.Seed(ParentId, FieldDef1, "FIELD_A", "\"some-value\"", fieldVersion: 3);

        var cmd = new InheritFieldsFromParentCommand(ChildId, ParentId, new[] { FieldDef1 });
        var result = await handler.Handle(cmd, CancellationToken.None);

        var dto = result.Single();
        dto.ValueJson.Should().Be("\"some-value\"");

        var stored = repo.Store.Single(f => f.CaseId == ChildId);
        stored.FieldVersion.Should().Be(3);
        stored.InheritedFromCaseId.Should().Be(ParentId);
    }

    [Fact]
    public async Task Returns_Empty_When_No_Matching_Inheritable_Fields_Exist_In_Parent()
    {
        var (handler, repo) = Build();
        // 主單有 FieldDef1，但指定繼承的是 FieldDef2（主單沒有）
        repo.Seed(ParentId, FieldDef1, "FIELD_A");

        var cmd = new InheritFieldsFromParentCommand(ChildId, ParentId, new[] { FieldDef2 });
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().BeEmpty();
        repo.Store.Where(f => f.CaseId == ChildId).Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_Empty_When_Inheritable_List_Is_Empty()
    {
        var (handler, repo) = Build();
        repo.Seed(ParentId, FieldDef1, "FIELD_A");

        var cmd = new InheritFieldsFromParentCommand(ChildId, ParentId, Array.Empty<Guid>());
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Child_Can_Update_Inherited_Field_Independently()
    {
        var (handler, repo) = Build();
        repo.Seed(ParentId, FieldDef1, "FIELD_A", "\"original\"");

        var cmd = new InheritFieldsFromParentCommand(ChildId, ParentId, new[] { FieldDef1 });
        await handler.Handle(cmd, CancellationToken.None);

        var childField = repo.Store.Single(f => f.CaseId == ChildId);

        // 子流程獨立修改，不影響主單
        childField.UpdateValue("\"modified-by-child\"");

        childField.ValueJson.Should().Be("\"modified-by-child\"");
        childField.InheritedFromCaseId.Should().Be(ParentId, "繼承來源仍保留以供前端顯示");

        var parentField = repo.Store.Single(f => f.CaseId == ParentId);
        parentField.ValueJson.Should().Be("\"original\"", "主單欄位不受子流程修改影響");
    }

    [Fact]
    public async Task Inherits_From_Correct_Parent_When_Multiple_Cases_Exist()
    {
        var (handler, repo) = Build();
        var otherParent = Guid.NewGuid();

        repo.Seed(ParentId, FieldDef1, "FIELD_A", "\"from-target-parent\"");
        repo.Seed(otherParent, FieldDef1, "FIELD_A", "\"from-other-parent\"");

        var cmd = new InheritFieldsFromParentCommand(ChildId, ParentId, new[] { FieldDef1 });
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Single().ValueJson.Should().Be("\"from-target-parent\"");
    }
}
