using IsoDocs.Application.Cases.Export;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IsoDocs.Infrastructure.Pdf;

/// <summary>
/// QuestPDF Community License 實作案件 PDF 匯出。
/// 中文字型需求：Linux 請安裝 fonts-noto-cjk；Windows 已內建 Microsoft YaHei。
/// </summary>
internal sealed class CasePdfExporter : ICasePdfExporter
{
    private static readonly string FontFamily = DetectFontFamily();

    static CasePdfExporter()
    {
        Settings.License = LicenseType.Community;
    }

    public byte[] Export(CasePdfData data)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(ts => ts.FontFamily(FontFamily).FontSize(9));

                page.Header().Element(c => RenderHeader(c, data));
                page.Content().PaddingTop(10).Element(c => RenderContent(c, data));
                page.Footer().Element(RenderFooter);
            });
        }).GeneratePdf();
    }

    private static void RenderHeader(IContainer container, CasePdfData data)
    {
        container.BorderBottom(1).PaddingBottom(8).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("IsoDocs 案件報告").FontSize(16).Bold();
                col.Item().Text($"{data.CaseNumber}　{data.Title}").FontSize(10);
            });
            row.ConstantItem(120).AlignRight().Column(col =>
            {
                col.Item().Text(StatusLabel(data.Status)).FontSize(11).Bold();
            });
        });
    }

    private static void RenderContent(IContainer container, CasePdfData data)
    {
        container.Column(col =>
        {
            col.Spacing(14);
            col.Item().Element(c => RenderBasicInfo(c, data));
            if (data.Fields.Count > 0)
                col.Item().Element(c => RenderFields(c, data.Fields));
            if (data.Nodes.Count > 0)
                col.Item().Element(c => RenderNodes(c, data.Nodes));
            if (data.Actions.Count > 0)
                col.Item().Element(c => RenderActions(c, data.Actions));
            if (data.Comments.Count > 0)
                col.Item().Element(c => RenderComments(c, data.Comments));
            if (data.Attachments.Count > 0)
                col.Item().Element(c => RenderAttachments(c, data.Attachments));
        });
    }

    private static void RenderBasicInfo(IContainer container, CasePdfData data)
    {
        container.Column(col =>
        {
            col.Item().Text("■ 基本資訊").FontSize(11).Bold();
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.ConstantColumn(90);
                    cd.RelativeColumn();
                    cd.ConstantColumn(90);
                    cd.RelativeColumn();
                });

                void InfoRow(string l1, string v1, string l2 = "", string v2 = "")
                {
                    table.Cell().Padding(3).Text(l1).Bold();
                    table.Cell().Padding(3).Text(v1);
                    table.Cell().Padding(3).Text(l2).Bold();
                    table.Cell().Padding(3).Text(v2);
                }

                InfoRow("標題", data.Title, "建立者", data.InitiatedByUserName);
                InfoRow("建立時間", FormatDate(data.InitiatedAt), "狀態", StatusLabel(data.Status));
                InfoRow("預計完成", Format(data.ExpectedCompletionAt),
                        "原始預計完成", Format(data.OriginalExpectedAt));
                if (data.CustomerName is not null || data.CustomVersionNumber is not null)
                    InfoRow("客戶", data.CustomerName ?? "—",
                            "自訂版號", data.CustomVersionNumber ?? "—");
                if (data.ClosedAt.HasValue || data.VoidedAt.HasValue)
                    InfoRow("結案時間", Format(data.ClosedAt), "作廢時間", Format(data.VoidedAt));
            });
        });
    }

    private static void RenderFields(IContainer container, IReadOnlyList<CaseFieldPdfItem> fields)
    {
        container.Column(col =>
        {
            col.Item().Text("■ 自訂欄位").FontSize(11).Bold();
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.ConstantColumn(130);
                    cd.RelativeColumn();
                });
                table.Header(h =>
                {
                    h.Cell().Background("#E8E8E8").Padding(3).Text("欄位代碼").Bold();
                    h.Cell().Background("#E8E8E8").Padding(3).Text("值").Bold();
                });
                foreach (var f in fields)
                {
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(f.FieldCode);
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(f.ValueJson);
                }
            });
        });
    }

    private static void RenderNodes(IContainer container, IReadOnlyList<CaseNodePdfItem> nodes)
    {
        container.Column(col =>
        {
            col.Item().Text("■ 流程節點").FontSize(11).Bold();
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.ConstantColumn(28);
                    cd.RelativeColumn(3);
                    cd.ConstantColumn(60);
                    cd.RelativeColumn(2);
                    cd.ConstantColumn(115);
                    cd.ConstantColumn(115);
                });
                table.Header(h =>
                {
                    foreach (var t in new[] { "#", "節點", "狀態", "承辦人", "開始", "完成" })
                        h.Cell().Background("#E8E8E8").Padding(3).Text(t).Bold();
                });
                foreach (var n in nodes)
                {
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(n.NodeOrder.ToString());
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(n.NodeName);
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(NodeStatusLabel(n.Status));
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(n.AssigneeUserName ?? "—");
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(Format(n.StartedAt));
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(Format(n.CompletedAt));
                }
            });
        });
    }

    private static void RenderActions(IContainer container, IReadOnlyList<CaseActionPdfItem> actions)
    {
        container.Column(col =>
        {
            col.Item().Text("■ 動作記錄").FontSize(11).Bold();
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(2);
                    cd.RelativeColumn(2);
                    cd.ConstantColumn(115);
                    cd.RelativeColumn(4);
                });
                table.Header(h =>
                {
                    foreach (var t in new[] { "動作", "操作人員", "時間", "說明" })
                        h.Cell().Background("#E8E8E8").Padding(3).Text(t).Bold();
                });
                foreach (var a in actions)
                {
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(a.ActionType);
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(a.ActorUserName);
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(FormatDate(a.ActionAt));
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(a.Comment ?? string.Empty);
                }
            });
        });
    }

    private static void RenderComments(IContainer container, IReadOnlyList<CommentPdfItem> comments)
    {
        container.Column(col =>
        {
            col.Spacing(6);
            col.Item().Text("■ 留言").FontSize(11).Bold();
            foreach (var cm in comments)
            {
                col.Item().BorderLeft(2).PaddingLeft(8).Column(inner =>
                {
                    inner.Item().Row(row =>
                    {
                        row.RelativeItem().Text(cm.AuthorUserName).Bold();
                        row.ConstantItem(115).AlignRight()
                           .Text(FormatDate(cm.CreatedAt)).FontSize(8);
                    });
                    inner.Item().PaddingTop(2).Text(cm.Body);
                });
            }
        });
    }

    private static void RenderAttachments(IContainer container, IReadOnlyList<AttachmentPdfItem> attachments)
    {
        container.Column(col =>
        {
            col.Item().Text("■ 附件清單").FontSize(11).Bold();
            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(cd =>
                {
                    cd.RelativeColumn(3);
                    cd.RelativeColumn(2);
                    cd.ConstantColumn(55);
                    cd.RelativeColumn(2);
                    cd.ConstantColumn(115);
                });
                table.Header(h =>
                {
                    foreach (var t in new[] { "檔名", "類型", "大小", "上傳者", "上傳時間" })
                        h.Cell().Background("#E8E8E8").Padding(3).Text(t).Bold();
                });
                foreach (var at in attachments)
                {
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(at.FileName);
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(at.ContentType);
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(FormatSize(at.SizeBytes));
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(at.UploadedByUserName);
                    table.Cell().BorderBottom(0.5f).Padding(3).Text(FormatDate(at.UploadedAt));
                }
            });
        });
    }

    private static void RenderFooter(IContainer container)
    {
        container.BorderTop(0.5f).PaddingTop(4).Row(row =>
        {
            row.RelativeItem()
               .Text("IsoDocs 自動產生，請勿作為正式文件使用")
               .FontSize(7).FontColor("#888888");
            row.ConstantItem(80).AlignRight().Text(x =>
            {
                x.Span("第 ").FontSize(8);
                x.CurrentPageNumber().Style(ts => ts.FontSize(8));
                x.Span(" / ").FontSize(8);
                x.TotalPages().Style(ts => ts.FontSize(8));
                x.Span(" 頁").FontSize(8);
            });
        });
    }

    private static string DetectFontFamily()
    {
        if (OperatingSystem.IsWindows()) return "Microsoft YaHei";
        if (OperatingSystem.IsMacOS()) return "PingFang SC";
        return "Noto Sans CJK SC";
    }

    private static string StatusLabel(string s) => s switch
    {
        "InProgress" => "進行中",
        "Closed" => "已結案",
        "Voided" => "已作廢",
        _ => s
    };

    private static string NodeStatusLabel(string s) => s switch
    {
        "Pending" => "待處理",
        "InProgress" => "處理中",
        "Completed" => "已完成",
        "Returned" => "已退回",
        "Skipped" => "已跳過",
        _ => s
    };

    private static string FormatDate(DateTimeOffset dt) =>
        dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    private static string Format(DateTimeOffset? dt) =>
        dt.HasValue ? FormatDate(dt.Value) : "—";

    private static string FormatSize(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1048576 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / 1048576.0:F1} MB"
    };
}
