using FluentAssertions;
using IsoDocs.Application.Cases.Queries;
using IsoDocs.Domain.Cases;
using NSubstitute;
using Xunit;

namespace IsoDocs.Application.UnitTests.Cases;

public sealed class SearchCasesQueryHandlerTests
{
    private readonly ICaseQueryService _queryService = Substitute.For<ICaseQueryService>();
    private readonly SearchCasesQueryHandler _handler;

    public SearchCasesQueryHandlerTests()
    {
        _handler = new SearchCasesQueryHandler(_queryService);
    }

    [Fact]
    public async Task Handle_DelegatesToQueryServiceWithKeyword()
    {
        const string keyword = "規格";
        var filter = new ListCasesFilter(Page: 1, PageSize: 20);
        _queryService.SearchAsync(keyword, filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CaseSummaryDto>(Array.Empty<CaseSummaryDto>(), 0, 1, 20));

        await _handler.Handle(new SearchCasesQuery(keyword, filter), CancellationToken.None);

        await _queryService.Received(1).SearchAsync(keyword, filter, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsSearchResults()
    {
        var dto = new CaseSummaryDto(
            Guid.NewGuid(), "ITCT-F01-260001", "規格變更申請",
            CaseStatus.InProgress,
            Guid.NewGuid(), "工作需求單",
            null, null,
            DateTimeOffset.UtcNow, null, null, null, null);
        var filter = new ListCasesFilter();
        _queryService.SearchAsync("規格", filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CaseSummaryDto>(new[] { dto }, 1, 1, 20));

        var result = await _handler.Handle(new SearchCasesQuery("規格", filter), CancellationToken.None);

        result.Items.Should().ContainSingle()
            .Which.Title.Should().Contain("規格");
    }

    [Fact]
    public async Task Handle_EmptyResults_WhenNoMatch()
    {
        var filter = new ListCasesFilter();
        _queryService.SearchAsync("不存在的關鍵字", filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CaseSummaryDto>(Array.Empty<CaseSummaryDto>(), 0, 1, 20));

        var result = await _handler.Handle(new SearchCasesQuery("不存在的關鍵字", filter), CancellationToken.None);

        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
    }
}
