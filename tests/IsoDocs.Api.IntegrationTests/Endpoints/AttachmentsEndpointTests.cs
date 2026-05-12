using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using IsoDocs.Application.Attachments;
using Xunit;

namespace IsoDocs.Api.IntegrationTests.Endpoints;

public class AttachmentsEndpointTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AttachmentsEndpointTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_Without_Auth_Returns_401()
    {
        var client = _factory.CreateClient();
        var caseId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/cases/{caseId}/attachments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InitiateUpload_Without_Auth_Returns_401()
    {
        var client = _factory.CreateClient();
        var caseId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/cases/{caseId}/attachments",
            new { fileName = "test.png", contentType = "image/png", sizeBytes = 1024 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InitiateUpload_With_Valid_Auth_Returns_UploadToken()
    {
        var oid = Guid.NewGuid().ToString();
        var client = AuthenticatedClient(oid, "alice@example.com", "Alice");
        var caseId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            $"/api/cases/{caseId}/attachments",
            new { fileName = "report.pdf", contentType = "application/pdf", sizeBytes = 2048L });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<UploadInitiationDto>();
        dto.Should().NotBeNull();
        dto!.AttachmentId.Should().NotBeEmpty();
        dto.UploadUrl.Should().Contain("fake.blob.core.windows.net");
        dto.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task List_With_Valid_Auth_Returns_Empty_List_For_New_Case()
    {
        var oid = Guid.NewGuid().ToString();
        var client = AuthenticatedClient(oid, "alice@example.com", "Alice");
        var caseId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/cases/{caseId}/attachments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await response.Content.ReadFromJsonAsync<List<AttachmentDto>>();
        list.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task GetDownloadUrl_For_Existing_Attachment_Returns_DownloadDto()
    {
        var oid = Guid.NewGuid().ToString();
        var client = AuthenticatedClient(oid, "alice@example.com", "Alice");
        var caseId = Guid.NewGuid();

        // Step 1: 發起上傳取得 attachmentId
        var uploadResponse = await client.PostAsJsonAsync(
            $"/api/cases/{caseId}/attachments",
            new { fileName = "photo.png", contentType = "image/png", sizeBytes = 512L });
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var upload = await uploadResponse.Content.ReadFromJsonAsync<UploadInitiationDto>();
        upload.Should().NotBeNull();

        // Step 2: 取得下載 URL
        var downloadResponse = await client.GetAsync(
            $"/api/cases/{caseId}/attachments/{upload!.AttachmentId}");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await downloadResponse.Content.ReadFromJsonAsync<AttachmentDownloadDto>();
        dto.Should().NotBeNull();
        dto!.AttachmentId.Should().Be(upload.AttachmentId);
        dto.FileName.Should().Be("photo.png");
        dto.DownloadUrl.Should().Contain("fake.blob.core.windows.net");
        dto.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GetDownloadUrl_With_Wrong_CaseId_Returns_500_Or_404()
    {
        var oid = Guid.NewGuid().ToString();
        var client = AuthenticatedClient(oid, "alice@example.com", "Alice");
        var caseId = Guid.NewGuid();
        var wrongCaseId = Guid.NewGuid();

        // 上傳到 caseId
        var uploadResponse = await client.PostAsJsonAsync(
            $"/api/cases/{caseId}/attachments",
            new { fileName = "doc.pdf", contentType = "application/pdf", sizeBytes = 1024L });
        var upload = await uploadResponse.Content.ReadFromJsonAsync<UploadInitiationDto>();

        // 用錯誤的 caseId 查詢，應回 4xx
        var downloadResponse = await client.GetAsync(
            $"/api/cases/{wrongCaseId}/attachments/{upload!.AttachmentId}");
        ((int)downloadResponse.StatusCode).Should().BeGreaterThanOrEqualTo(400);
    }

    private HttpClient AuthenticatedClient(string oid, string email, string name)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            TestAuthHandler.SchemeName,
            TestAuthHandler.EncodeClaims(new[]
            {
                new Claim("oid", oid),
                new Claim("tid", "00000000-0000-0000-0000-000000000001"),
                new Claim("email", email),
                new Claim("name", name)
            }));
        return client;
    }
}
