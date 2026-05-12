using IsoDocs.Application.Cases.Export;

namespace IsoDocs.Api.IntegrationTests.Fakes;

public sealed class FakeCasePdfDataProvider : ICasePdfDataProvider
{
    private CasePdfData? _data;

    public void SetData(CasePdfData? data) => _data = data;

    public Task<CasePdfData?> GetAsync(Guid caseId, CancellationToken cancellationToken = default)
        => Task.FromResult(_data);
}

public sealed class FakeCasePdfExporter : ICasePdfExporter
{
    public byte[] Export(CasePdfData data) =>
        System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 fake-pdf-for-testing");
}
