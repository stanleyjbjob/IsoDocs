namespace IsoDocs.Application.Cases.Export;

public interface ICasePdfDataProvider
{
    Task<CasePdfData?> GetAsync(Guid caseId, CancellationToken cancellationToken = default);
}
