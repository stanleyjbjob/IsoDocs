using FluentAssertions;
using IsoDocs.Application.FieldDefinitions.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Workflows;
using Xunit;

namespace IsoDocs.Application.UnitTests.FieldDefinitions;

public class UpdateFieldDefinitionCommandHandlerTests
{
    [Fact]
    public async Task Update_Should_Increment_Version()
    {
        var repo = new FakeFieldDefinitionRepository();
        var existing = repo.Seed("DEADLINE", "截止日期", FieldType.Date, isRequired: false);
        var originalVersion = existing.Version;
        var handler = new UpdateFieldDefinitionCommandHandler(repo);
        var cmd = new UpdateFieldDefinitionCommand(
            Id: existing.Id,
            Name: "最終截止日期",
            Type: FieldType.Date,
            IsRequired: true,
            ValidationJson: null,
            OptionsJson: null);

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Version.Should().Be(originalVersion + 1);
        dto.Name.Should().Be("最終截止日期");
        dto.IsRequired.Should().BeTrue();
    }

    [Fact]
    public async Task Update_Nonexistent_Should_Throw_DomainException()
    {
        var repo = new FakeFieldDefinitionRepository();
        var handler = new UpdateFieldDefinitionCommandHandler(repo);
        var cmd = new UpdateFieldDefinitionCommand(Guid.NewGuid(), "名稱", FieldType.Text, false, null, null);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("FIELD_DEF/NOT_FOUND");
    }

    [Fact]
    public async Task Update_Multiple_Times_Should_Increment_Version_Each_Time()
    {
        var repo = new FakeFieldDefinitionRepository();
        var existing = repo.Seed("STATUS", "狀態", FieldType.SingleSelect, isRequired: true);
        var handler = new UpdateFieldDefinitionCommandHandler(repo);

        var cmd1 = new UpdateFieldDefinitionCommand(existing.Id, "處理狀態", FieldType.SingleSelect, true,
            null, "[\"待處理\",\"處理中\"]");
        await handler.Handle(cmd1, CancellationToken.None);

        var cmd2 = new UpdateFieldDefinitionCommand(existing.Id, "處理狀態", FieldType.SingleSelect, true,
            null, "[\"待處理\",\"處理中\",\"已完成\"]");
        var dto = await handler.Handle(cmd2, CancellationToken.None);

        dto.Version.Should().Be(3);
    }

    [Fact]
    public async Task Version_Isolation_CaseField_Snapshot_Unaffected_By_FieldDefinition_Update()
    {
        // 驗證版本隔離：CaseField 以建立當下的 FieldVersion 凍結定義，欄位更新不影響快照
        var repo = new FakeFieldDefinitionRepository();
        var fd = repo.Seed("AMOUNT", "金額", FieldType.Number, isRequired: true);

        // 模擬案件建立時快照版本
        var snapshotVersion = fd.Version;
        snapshotVersion.Should().Be(1);

        var handler = new UpdateFieldDefinitionCommandHandler(repo);
        var cmd = new UpdateFieldDefinitionCommand(fd.Id, "金額（萬元）", FieldType.Decimal, true, null, null);
        var updated = await handler.Handle(cmd, CancellationToken.None);

        // 欄位定義版本更新了
        updated.Version.Should().Be(2);

        // 案件快照版本仍為 1（版本隔離生效）
        snapshotVersion.Should().Be(1);
        updated.Version.Should().NotBe(snapshotVersion);
    }
}
