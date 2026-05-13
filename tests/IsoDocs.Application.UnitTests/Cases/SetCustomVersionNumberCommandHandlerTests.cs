using FluentAssertions;
using IsoDocs.Application.Cases.Commands;
using IsoDocs.Application.UnitTests.Fakes;
using IsoDocs.Domain.Common;
using Xunit;

namespace IsoDocs.Application.UnitTests.Cases;

public class SetCustomVersionNumberCommandHandlerTests
{
    [Theory]
    [InlineData("v1.0")]
    [InlineData("1.0.0")]
    [InlineData("A")]
    [InlineData("REV-01")]
    [InlineData("2026-A")]
    public async Task Valid_VersionNumber_Should_Persist(string version)
    {
        var repo = new FakeCaseRepository();
        var @case = repo.Seed();
        var handler = new SetCustomVersionNumberCommandHandler(repo);

        var result = await handler.Handle(
            new SetCustomVersionNumberCommand(@case.Id, version), CancellationToken.None);

        result.Should().Be(version);
        repo.Store[@case.Id].CustomVersionNumber.Should().Be(version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a b")]
    [InlineData("版號")]
    [InlineData("toolongversionstring0123")]
    public async Task Invalid_VersionNumber_Should_Throw_DomainException(string version)
    {
        var repo = new FakeCaseRepository();
        var @case = repo.Seed();
        var handler = new SetCustomVersionNumberCommandHandler(repo);

        var act = async () => await handler.Handle(
            new SetCustomVersionNumberCommand(@case.Id, version), CancellationToken.None);
        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task NonExistent_Case_Should_Throw_NotFound()
    {
        var repo = new FakeCaseRepository();
        var handler = new SetCustomVersionNumberCommandHandler(repo);

        var act = async () => await handler.Handle(
            new SetCustomVersionNumberCommand(Guid.NewGuid(), "v1.0"), CancellationToken.None);
        (await act.Should().ThrowAsync<DomainException>())
            .Which.Code.Should().Be("case.not_found");
    }
}
