using IsoDocs.Application.Common.Messaging;
using IsoDocs.Domain.Common;

namespace IsoDocs.Application.Cases.Export;

public sealed record ExportCasePdfQuery(Guid CaseId) : IQuery<byte[]>;

public sealed class ExportCasePdfQueryHandler : IQueryHandler<ExportCasePdfQuery, byte[]>
{
    private readonly ICasePdfDataProvider _dataProvider;
    private readonly ICasePdfExporter _exporter;

    public ExportCasePdfQueryHandler(ICasePdfDataProvider dataProvider, ICasePdfExporter exporter)
    {
        _dataProvider = dataProvider;
        _exporter = exporter;
    }

    public async Task<byte[]> Handle(ExportCasePdfQuery request, CancellationToken cancellationToken)
    {
        var data = await _dataProvider.GetAsync(request.CaseId, cancellationToken)
            ?? throw new DomainException("case.not_found", $"找不到案件 {request.CaseId}。");
        return _exporter.Export(data);
    }
}
