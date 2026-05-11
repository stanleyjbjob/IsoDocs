using FluentAssertions;
using IsoDocs.Application.DocumentTypes;
using IsoDocs.Application.DocumentTypes.Commands;
using IsoDocs.Domain.Common;
using IsoDocs.Domain.Workflows;
using NSubstitute;

namespace IsoDocs.Application.UnitTests.DocumentTypes;

public sealed class CreateDocumentTypeCommandTests
{
    private readonly IDocumentTypeRepository _repo = Substitute.For<IDocumentTypeRepository>();
    private readonly CreateDocumentTypeCommandHandler _handler;

    public CreateDocumentTypeCommandTests()
    {
        _handler = new CreateDocumentTypeCommandHandler(_repo);
    }

    [Fact]
    public async Task Handle_NewCode_CreatesAndReturnsDto()
    {
        _repo.FindByCompanyAndCodeAsync("ITCT", "F01", default)
            .ReturnsForAnyArgs((DocumentType?)null);

        var cmd = new CreateDocumentTypeCommand("ITCT", "F01", "工作需求單");
        var dto = await _handler.Handle(cmd, default);

        dto.CompanyCode.Should().Be("ITCT");
        dto.Code.Should().Be("F01");
        dto.Name.Should().Be("工作需求單");
        dto.IsActive.Should().BeTrue();

        await _repo.Received(1).AddAsync(Arg.Any<DocumentType>(), default);
        await _repo.Received(1).SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_DuplicateCode_ThrowsDomainException()
    {
        var existing = new DocumentType(Guid.NewGuid(), "ITCT", "F01", "工作需求單", 2026);
        _repo.FindByCompanyAndCodeAsync("ITCT", "F01", default)
            .ReturnsForAnyArgs(existing);

        var cmd = new CreateDocumentTypeCommand("ITCT", "F01", "重複");

        var act = () => _handler.Handle(cmd, default);

        await act.Should().ThrowAsync<DomainException>()
            .Where(e => e.Code == DocumentTypeErrorCodes.CodeDuplicate);
    }
}
