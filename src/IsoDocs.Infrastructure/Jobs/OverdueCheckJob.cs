using IsoDocs.Application.Notifications;
using IsoDocs.Domain.Cases;
using IsoDocs.Domain.Communications;
using IsoDocs.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IsoDocs.Infrastructure.Jobs;

/// <summary>
/// 每日逾期稽催排程任務（issue [6.3]）。
/// 查詢所有進行中且已超過預計完成時間的案件，
/// 僅通知當前節點（InProgress/Pending）的承辦人。
/// </summary>
public class OverdueCheckJob
{
    private readonly IsoDocsDbContext _db;
    private readonly INotificationSender _sender;
    private readonly INotificationRepository _notificationRepo;
    private readonly ILogger<OverdueCheckJob> _logger;

    public OverdueCheckJob(
        IsoDocsDbContext db,
        INotificationSender sender,
        INotificationRepository notificationRepo,
        ILogger<OverdueCheckJob> logger)
    {
        _db = db;
        _sender = sender;
        _notificationRepo = notificationRepo;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        // 取得進行中且已逾期的案件，以及各案件當前活躍節點的承辦人
        var overdueWithNodes = await (
            from c in _db.Cases
            join n in _db.CaseNodes on c.Id equals n.CaseId
            where c.Status == CaseStatus.InProgress
               && c.ExpectedCompletionAt.HasValue
               && c.ExpectedCompletionAt.Value < now
               && (n.Status == CaseNodeStatus.InProgress || n.Status == CaseNodeStatus.Pending)
               && n.AssigneeUserId.HasValue
            select new
            {
                CaseId = c.Id,
                c.CaseNumber,
                c.Title,
                c.ExpectedCompletionAt,
                NodeId = n.Id,
                n.NodeName,
                AssigneeUserId = n.AssigneeUserId!.Value
            }
        ).ToListAsync(cancellationToken);

        _logger.LogInformation("[逾期稽催] 發現 {Count} 個逾期節點待通知", overdueWithNodes.Count);

        foreach (var item in overdueWithNodes)
        {
            var subject = $"【逾期提醒】案件 {item.CaseNumber} 已超過預計完成時間";
            var body = $"案件「{item.Title}」（{item.CaseNumber}）預計於 {item.ExpectedCompletionAt:yyyy-MM-dd} 完成，"
                     + $"目前節點「{item.NodeName}」尚未結案，請儘速處理。";

            foreach (var channel in new[] { NotificationChannel.InApp, NotificationChannel.Email, NotificationChannel.Teams })
            {
                var notification = new Notification(
                    id: Guid.NewGuid(),
                    recipientUserId: item.AssigneeUserId,
                    type: NotificationType.Overdue,
                    channel: channel,
                    subject: subject,
                    body: body,
                    caseId: item.CaseId,
                    payloadJson: null);

                await _notificationRepo.AddAsync(notification, cancellationToken);

                try
                {
                    await _sender.SendAsync(notification, cancellationToken);
                    notification.MarkSent();
                }
                catch (Exception ex)
                {
                    notification.MarkFailed(ex.Message);
                    _logger.LogWarning(ex, "[逾期稽催] 通知發送失敗 NotificationId={Id}", notification.Id);
                }
            }
        }

        await _notificationRepo.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[逾期稽催] 本次排程完成，共處理 {Count} 個通知", overdueWithNodes.Count * 3);
    }
}
