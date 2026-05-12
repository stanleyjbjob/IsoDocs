/**
 * 流程節點類型目錄。
 *
 * 對應後端 #12 [3.2.1] `WorkflowNode.NodeType` 欄位。
 * 後端實際上只儲存字串 key（'start' / 'handle' / ...），
 * UI 標籤、顏色、限制條件由前端 catalog 維護。
 */

export type WorkflowNodeType = 'start' | 'handle' | 'approve' | 'notify' | 'end';

export interface NodeTypeMeta {
  /** 節點類型 key（後端儲存的字串） */
  type: WorkflowNodeType;
  /** 中文顯示標籤 */
  label: string;
  /** 簡短說明（顯示於設計器右側 / tooltip） */
  description: string;
  /** antd Tag 顏色 */
  color: string;
  /** 是否強制需要 `requiredRoleId`（角色） */
  requiresRole: boolean;
  /** 是否可作為第一個節點 */
  canBeFirst: boolean;
  /** 是否可作為最後節點 */
  canBeLast: boolean;
}

export const NODE_TYPES: readonly NodeTypeMeta[] = [
  {
    type: 'start',
    label: '起始',
    description:
      '案件發起時的入口節點。通常由發起人填寫案件主檔，不需指派角色。每個範本恰好需要 1 個 start 節點，且必須是第一個。',
    color: 'green',
    requiresRole: false,
    canBeFirst: true,
    canBeLast: false,
  },
  {
    type: 'handle',
    label: '處理',
    description:
      '由指定角色執行的處理節點，例如「業務確認」「工程師回覆」。需指派必要角色。',
    color: 'blue',
    requiresRole: true,
    canBeFirst: false,
    canBeLast: false,
  },
  {
    type: 'approve',
    label: '核准',
    description:
      '由指定角色核准 / 退回的節點。退回會回到前一個處理節點。需指派必要角色。',
    color: 'gold',
    requiresRole: true,
    canBeFirst: false,
    canBeLast: false,
  },
  {
    type: 'notify',
    label: '通知',
    description:
      '只發通知不需動作的節點，例如「副知 PM」。可選擇性指派角色（不指派則用案件預設關係人）。',
    color: 'purple',
    requiresRole: false,
    canBeFirst: false,
    canBeLast: false,
  },
  {
    type: 'end',
    label: '結束',
    description:
      '案件結束節點。不需指派角色。每個範本恰好需要 1 個 end 節點，且必須是最後一個。',
    color: 'default',
    requiresRole: false,
    canBeFirst: false,
    canBeLast: true,
  },
];

export function getNodeTypeMeta(type: string): NodeTypeMeta | null {
  return NODE_TYPES.find((n) => n.type === type) ?? null;
}

/**
 * 驗證節點清單結構（基本檢查，不取代後端最終驗證）。
 *
 * 規則：
 * - 至少 2 個節點（start + end）
 * - 第一個必為 start、最後一個必為 end
 * - 中間不得再出現 start / end
 * - 所有 nodeKey 必須唯一
 * - requiresRole=true 的節點必須指派 requiredRoleId
 */
export interface NodeValidationIssue {
  nodeKey: string | null;
  nodeOrder: number | null;
  message: string;
}

export function validateNodes(
  nodes: readonly { nodeOrder: number; nodeKey: string; nodeType: string; requiredRoleId?: string }[],
): NodeValidationIssue[] {
  const issues: NodeValidationIssue[] = [];

  if (nodes.length < 2) {
    issues.push({
      nodeKey: null,
      nodeOrder: null,
      message: '範本至少需要 2 個節點（起始 + 結束）',
    });
    return issues;
  }

  // 依 order 排序後檢查
  const sorted = [...nodes].sort((a, b) => a.nodeOrder - b.nodeOrder);

  // 檢查 nodeKey 唯一
  const seen = new Set<string>();
  for (const n of sorted) {
    if (!n.nodeKey) {
      issues.push({ nodeKey: null, nodeOrder: n.nodeOrder, message: '節點 nodeKey 不可空白' });
      continue;
    }
    if (seen.has(n.nodeKey)) {
      issues.push({
        nodeKey: n.nodeKey,
        nodeOrder: n.nodeOrder,
        message: `nodeKey 重複：${n.nodeKey}`,
      });
    }
    seen.add(n.nodeKey);
  }

  // 第一個與最後一個
  const first = sorted[0];
  const last = sorted[sorted.length - 1];
  if (first.nodeType !== 'start') {
    issues.push({
      nodeKey: first.nodeKey,
      nodeOrder: first.nodeOrder,
      message: '第一個節點必須是「起始」(start)',
    });
  }
  if (last.nodeType !== 'end') {
    issues.push({
      nodeKey: last.nodeKey,
      nodeOrder: last.nodeOrder,
      message: '最後一個節點必須是「結束」(end)',
    });
  }

  // 中間節點不得是 start / end
  for (let i = 1; i < sorted.length - 1; i += 1) {
    const n = sorted[i];
    if (n.nodeType === 'start' || n.nodeType === 'end') {
      issues.push({
        nodeKey: n.nodeKey,
        nodeOrder: n.nodeOrder,
        message: `中間節點不能是 ${n.nodeType}`,
      });
    }
  }

  // 必要角色檢查
  for (const n of sorted) {
    const meta = getNodeTypeMeta(n.nodeType);
    if (meta?.requiresRole && !n.requiredRoleId) {
      issues.push({
        nodeKey: n.nodeKey,
        nodeOrder: n.nodeOrder,
        message: `${meta.label}節點必須指派必要角色`,
      });
    }
  }

  return issues;
}
