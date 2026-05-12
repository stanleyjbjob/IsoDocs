namespace IsoDocs.Application.Cases.Export;

public interface ICasePdfExporter
{
    byte[] Export(CasePdfData data);
}
