using FluentAssertions;
using IsoDocs.Domain.Workflows;

namespace IsoDocs.Application.UnitTests.DocumentTypes;

public sealed class DocumentTypeAcquireNextTests
{
    private static DocumentType CreateDocType(int sequenceYear = 2026, int currentSequence = 0)
        => new(Guid.NewGuid(), "ITCT", "F01", "工作需求單", sequenceYear)
            .WithSequence(currentSequence);

    [Fact]
    public void AcquireNext_SameYear_IncrementsSequence()
    {
        var dt = CreateDocType(2026, 75);

        var code = dt.AcquireNext(2026);

        code.Should().Be("ITCT-F01-260076");
        dt.CurrentSequence.Should().Be(76);
    }

    [Fact]
    public void AcquireNext_NewYear_ResetsSequenceAndUsesNewYear()
    {
        var dt = CreateDocType(2025, 999);

        var code = dt.AcquireNext(2026);

        code.Should().Be("ITCT-F01-260001");
        dt.CurrentSequence.Should().Be(1);
        dt.SequenceYear.Should().Be(2026);
    }

    [Fact]
    public void AcquireNext_FirstCode_SequenceStartsAt1()
    {
        var dt = CreateDocType(2026, 0);

        var code = dt.AcquireNext(2026);

        code.Should().Be("ITCT-F01-260001");
    }

    [Fact]
    public void AcquireNext_YearTwoDigitsFormatted()
    {
        var dt = CreateDocType(2099, 0);

        var code = dt.AcquireNext(2099);

        code.Should().StartWith("ITCT-F01-99");
    }

    [Fact]
    public void AcquireNext_CalledMultipleTimes_SequenceMonotonicallyIncreases()
    {
        var dt = CreateDocType(2026, 0);

        var codes = Enumerable.Range(0, 5).Select(_ => dt.AcquireNext(2026)).ToList();

        codes.Should().BeEquivalentTo(new[]
        {
            "ITCT-F01-260001",
            "ITCT-F01-260002",
            "ITCT-F01-260003",
            "ITCT-F01-260004",
            "ITCT-F01-260005"
        }, opts => opts.WithStrictOrdering());
    }
}

/// <summary>
/// 測試用擴充方法，允許設定 CurrentSequence（繞過 protected setter）。
/// </summary>
file static class DocumentTypeTestExtensions
{
    internal static DocumentType WithSequence(this DocumentType dt, int sequence)
    {
        // 透過反射設定 protected 欄位以建立測試前置狀態
        var prop = typeof(DocumentType).GetProperty(nameof(DocumentType.CurrentSequence));
        prop!.SetValue(dt, sequence);
        return dt;
    }
}
