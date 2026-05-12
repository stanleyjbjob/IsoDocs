using FluentAssertions;
using IsoDocs.Application.Cases.Queries;
using IsoDocs.Domain.Cases;
using NSubstitute;
using Xunit;

namespace IsoDocs.Application.UnitTests.Cases;

public sealed class ListCasesQueryHandlerTests
{
    private readonly ICaseQueryService _queryService = Substitute.For<ICaseQueryService>();
    private readonly ListCasesQueryHandler _handler;

    public ListCasesQueryHandlerTests()
    {
        _handler = new ListCasesQueryHandler(_queryService);
    }

    [Fact]
    public async Task Handle_DelegatesToQueryService_WithGivenFilter()
    {
        var filter = new ListCasesFilter(Status: CaseStatus.InProgress, Page: 1, PageSize: 10);
        var expected = new PagedResult<CaseSummaryDto>(Array.Empty<CaseSummaryDto>(), 0, 1, 10);
        _queryService.ListAsync(filter, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _handler.Handle(new ListCasesQuery(filter), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        await _queryService.Received(1).ListAsync(filter, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsPaginationMetadata()
    {
        var dto = new CaseSummaryDto(
            Guid.NewGuid(), "ITCT-F01-260001", "測試案件",
            CaseStatus.InProgress,
            Guid.NewGuid(), "工作需求單",
            null, null,
            DateTimeOffset.UtcNow, null, null, null, null);
        var filter = new ListCasesFilter();
        _queryService.ListAsync(filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CaseSummaryDto>(new[] { dto }, 1, 1, 20));

        var result = await _handler.Handle(new ListCasesQuery(filter), CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle()
            .Which.CaseNumber.Should().Be("ITCT-F01-260001");
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(3, 3)]
    public async Task Handle_PageNumberIsPassedAsProvided(int inputPage, int _)
    {
        var filter = new ListCasesFilter(Page: inputPage);
        _queryService.ListAsync(Arg.Any<ListCasesFilter>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CaseSummaryDto>(Array.Empty<CaseSummaryDto>(), 0, inputPage, 20));

        var result = await _handler.Handle(new ListCasesQuery(filter), CancellationToken.None);

        // Handler delegates as-is; clamping is done in the query service
        await _queryService.Received(1).ListAsync(
            Arg.Is<ListCasesFilter>(f => f.Page == inputPage),
            Arg.Any<CancellationToken>());
        _ = result; // suppress warning
    }
}
