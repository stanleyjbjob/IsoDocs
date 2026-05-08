namespace IsoDocs.Domain.Common;

/// <summary>
/// 領域層自訂例外，用於違反業務規則時拋出。會由全域例外處理中介軟體轉為 400/422。
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public DomainException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }
}
