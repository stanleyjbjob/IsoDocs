namespace IsoDocs.Domain.Cases;

/// <summary>
/// 案件動作類型。全部寫入 CaseAction 軌跡。
/// </summary>
public enum CaseActionType
{
    Initiate = 1,        // 發起
    Assign = 2,          // 指派
    Accept = 3,          // 接單
    ReplyClose = 4,      // 回覆結案
    Approve = 5,         // 核准/結案
    Reject = 6,          // 退回至前一處理節點
    SpawnChild = 7,      // 衖生子流程
    Void = 8,            // 作廢
    VoidCascade = 9,     // 連鎖作廢（主單作廢連動子流程）
    Reopen = 10,         // 重開新案
    Comment = 11,        // 留言
    UpdateExpected = 12, // 修改預計完成時間
    SignOff = 13         // 文件發行簽核
}
