using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using FluentAssertions;
using IsoDocs.Api.IntegrationTests.Fakes;
using IsoDocs.Application.Cases.Export;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace IsoDocs.Api.IntegrationTests.Endpoints;

public sealed class CasesExportEndpointTests : IClassFixture<CasesExportWebApplicationFactory>
{
    private readonly CasesExportWebApplicationFactory _factory;

    public CasesExportEndpointTests(CasesExportWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExportPdf_Without_Auth_Returns_401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/cases/{Guid.NewGuid()}/export/pdf");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportPdf_CaseNotFound_Returns_422()
    {
        _factory.PdfDataProvider.SetData(null);
        var client = AuthenticatedClient();
        var response = await client.GetAsync($"/api/cases/{Guid.NewGuid()}/export/pdf");
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ExportPdf_ValidCase_Returns_200_With_PdfContentType()
    {
        _factory.PdfDataProvider.SetData(BuildSamplePdfData());
        var client = AuthenticatedClient();
        var response = await client.GetAsync($"/api/cases/{Guid.NewGuid()}/export/pdf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
    }

    private HttpClient AuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthHandler.SchemeName,
            TestAuthHandler.EncodeClaims(new[]
            {
                new Claim("oid", Guid.NewGuid().ToString()),
                new Claim("tid", "00000000-0000-0000-0000-000000000001"),
                new Claim("email", "testuser@example.com"),
                new Claim("name", "Test User")
            }));
        return client;
    }

    private static CasePdfData BuildSamplePdfData() => new(
        CaseNumber: "ITCT-F01-260001",
        Title: "測試案件",
        Status: "InProgress",
        InitiatedByUserName: "Alice",
        InitiatedAt: DateTimeOffset.UtcNow.AddDays(-3),
        ExpectedCompletionAt: DateTimeOffset.UtcNow.AddDays(7),
        OriginalExpectedAt: DateTimeOffset.UtcNow.AddDays(7),
        ClosedAt: null,
        VoidedAt: null,
        CustomVersionNumber: null,
        CustomerName: "ABC Corp",
        Fields: new[] { new CaseFieldPdfItem("SUBJECT", "\"測試主題\"") },
        Nodes: new[] { new CaseNodePdfItem(1, "初審", "InProgress", "Alice",
            DateTimeOffset.UtcNow.AddDays(-2), null, null) },
        Actions: new[] { new CaseActionPdfItem("Accept", "Alice", null,
            DateTimeOffset.UtcNow.AddDays(-2)) },
        Comments: new[] { new CommentPdfItem("Bob", "這是一則留言",
            DateTimeOffset.UtcNow.AddDays(-1)) },
        Attachments: Array.Empty<AttachmentPdfItem>());
}

public sealed class CasesExportWebApplicationFactory : CustomWebApplicationFactory
{
    public FakeCasePdfDataProvider PdfDataProvider { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICasePdfDataProvider>();
            services.RemoveAll<ICasePdfExporter>();
            services.AddSingleton<ICasePdfDataProvider>(PdfDataProvider);
            services.AddSingleton<ICasePdfExporter>(new FakeCasePdfExporter());
        });
    }
}
