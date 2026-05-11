using FluentAssertions;
using IsoDocs.Application.Customers.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Common;
using Xunit;

namespace IsoDocs.Application.UnitTests.Customers;

public class CreateCustomerCommandHandlerTests
{
    [Fact]
    public async Task Create_With_Valid_Input_Should_Persist_And_Return_Dto()
    {
        var repo = new FakeCustomerRepository();
        var handler = new CreateCustomerCommandHandler(repo);
        var cmd = new CreateCustomerCommand("CUST-001", "台積電", "王小明", "wang@tsmc.com", "02-12345678", null);

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.Should().NotBeNull();
        dto.Code.Should().Be("CUST-001");
        dto.Name.Should().Be("台積電");
        dto.IsActive.Should().BeTrue();
        repo.Store.Should().ContainKey(dto.Id);
    }

    [Fact]
    public async Task Create_With_Duplicate_Code_Should_Throw_DomainException()
    {
        var repo = new FakeCustomerRepository();
        repo.Seed("CUST-001", "台積電");
        var handler = new CreateCustomerCommandHandler(repo);
        var cmd = new CreateCustomerCommand("CUST-001", "聯發科", null, null, null, null);

        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Code.Should().Be("CUSTOMER/CODE_DUPLICATE");
    }

    [Fact]
    public async Task Create_With_Minimal_Fields_Should_Succeed()
    {
        var repo = new FakeCustomerRepository();
        var handler = new CreateCustomerCommandHandler(repo);
        var cmd = new CreateCustomerCommand("CUST-MIN", "最小客戶", null, null, null, null);

        var dto = await handler.Handle(cmd, CancellationToken.None);

        dto.ContactPerson.Should().BeNull();
        dto.IsActive.Should().BeTrue();
    }
}
