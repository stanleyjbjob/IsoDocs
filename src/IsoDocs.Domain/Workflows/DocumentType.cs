using IsoDocs.Domain.Common;

namespace IsoDocs.Domain.Workflows;

/// <summary>
/// 文件類型（issue [5.1.1]）。例如 F01 = 工作需求單。
/// 編碼格式：{CompanyCode}-{Code}-{YearTwoDigits}{Sequence}（例：ITCT-F01-260076）。
/// 流水號依文件類型各自累計，並以 SequenceYear 判斷每年自動重置。
/// </summary>
public class DocumentType : Entity<Guid>, IAggregateRoot
{
    public string CompanyCode { get; protected set; } = string.Empty;
    public string Code { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public int SequenceYear { get; protected set; }
    public int CurrentSequence { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    /// <summary>樂觀鎖定欄位，避免併發取號競爭。</summary>
    public byte[] RowVersion { get; protected set; } = Array.Empty<byte>();

    private DocumentType() { }

    public DocumentType(Guid id, string companyCode, string code, string name, int sequenceYear)
    {
        Id = id;
        CompanyCode = companyCode;
        Code = code;
        Name = name;
        SequenceYear = sequenceYear;
    }

    /// <summary>
    /// 取下一個流水號。若年度切換則自動重置。
    /// 注意：實際併發控制需搭配 EF Core 樂觀鎖（RowVersion）或交易隔離。
    /// </summary>
    public string AcquireNext(int currentYear)
    {
        if (currentYear != SequenceYear)
        {
            SequenceYear = currentYear;
            CurrentSequence = 0;
        }
        CurrentSequence += 1;
        UpdatedAt = DateTimeOffset.UtcNow;
        var yearTwoDigits = (SequenceYear % 100).ToString("D2");
        return $"{CompanyCode}-{Code}-{yearTwoDigits}{CurrentSequence:D4}";
    }
}
