using IsoDocs.Domain.Cases;
using MediatR;

namespace IsoDocs.Application.Cases;

/// <summary>
/// 案件動作 MediatR 通知事件。每次執行案件動作後發布，供 issue #24 通知模組訂閱。
/// </summary>
public sealed record CaseActionNotification(
    Guid CaseId,
    Guid? CaseNodeId,
    CaseActionType ActionType,
    Guid ActorUserId) : INotification;
