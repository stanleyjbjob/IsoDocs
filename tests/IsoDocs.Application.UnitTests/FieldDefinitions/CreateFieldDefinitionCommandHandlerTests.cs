using FluentAssertions;
using IsoDocs.Application.FieldDefinitions.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Workflows;
using Xunit;

namespace IsoDocs.Application.UnitTests.FieldDefinitions;

public class CreateFieldDefinitionCommandHandlerTests
{
    [Fact]
    public async Task Create_With_Valid_Input_Should_Persist_And_Return_Dto()
    {
        var repo = new FakeFieldDefinitionRepository();
        var handler = new CreateFieldDefinitionCommandHandler(repo);
        var cmd = new CreateFieldDefinitionCommand(
            Code: "TITLE",
            Name: "標題",
            Type: FieldType.Text,
            IsRequired: true,
            ValidationJson: null,
            OptionsJson: null);

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Should().NotBeNull();
        dto.Code.Should().Be("TITLE");
        dto.Name.Should().Be("標題");
        dto.Type.Should().Be(FieldType.Text);
        dto.IsRequired.Should().BeTrue();
        dto.Version.Should().Be(1);
        dto.IsActive.Should().BeTrue();
        repo.Store.Should().ContainKey(dto.Id);
    }

    [Fact]
    public async Task Create_With_Duplicate_Code_Should_Throw_DomainException()
    {
        var repo = new FakeFieldDefinitionRepository();
        repo.Seed("TITLE", "標題");
        var handler = new CreateFieldDefinitionCommandHandler(repo);
        var cmd = new CreateFieldDefinitionCommand("TITLE", "另一個標題", FieldType.Text, false, null, null);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("FIELD_DEF/CODE_DUPLICATE");
    }

    [Fact]
    public async Task Create_With_SingleSelect_Type_Should_Accept_OptionsJson()
    {
        var repo = new FakeFieldDefinitionRepository();
        var handler = new CreateFieldDefinitionCommandHandler(repo);
        var optionsJson = "[\"選項A\",\"選項B\",\"選項C\"]";
        var cmd = new CreateFieldDefinitionCommand(
            Code: "CATEGORY",
            Name: "類別",
            Type: FieldType.SingleSelect,
            IsRequired: false,
            ValidationJson: null,
            OptionsJson: optionsJson);

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.OptionsJson.Should().Be(optionsJson);
        dto.Type.Should().Be(FieldType.SingleSelect);
    }

    [Fact]
    public async Task Create_Different_Types_Should_All_Succeed()
    {
        var repo = new FakeFieldDefinitionRepository();
        var handler = new CreateFieldDefinitionCommandHandler(repo);

        foreach (var type in new[] { FieldType.Text, FieldType.Number, FieldType.Date, FieldType.Boolean, FieldType.MultiSelect })
        {
            var cmd = new CreateFieldDefinitionCommand(
                Code: type.ToString().ToUpper(),
                Name: $"欄位_{type}",
                Type: type,
                IsRequired: false,
                ValidationJson: null,
                OptionsJson: null);

            var dto = await handler.Handle(cmd, CancellationToken.None);
            dto.Type.Should().Be(type);
        }
    }
}
