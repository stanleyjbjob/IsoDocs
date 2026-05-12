import { apiClient } from './client';

// ============================================================================
// 對齊 issue #9 [5.2.1] / #15 [5.2.2] / #17 [5.3.1] / #18 [5.3.3] / #20 [5.4.1] /
// #32 [5.3.4] / #33 [5.4.2] 的 RESTful 契約定義
// ============================================================================

export type CaseStatus = 'in_progress' | 'completed' | 'voided';
export type NodeStatus = 'pending' | 'in_progress' | 'completed' | 'skipped' | 'rejected';
export type CaseRelationType = 'spawn' | 'reopen';
export type NodeType = 'start' | 'handle' | 'approve' | 'notify' | 'end';

export type CaseActionType =
  | 'create'
  | 'assign'
  | 'accept'
  | 'reply_close'
  | 'approve'
  | 'reject'
  | 'void'
  | 'spawn_child'
  | 'reopen'
  | 'expected_completion_change'
  | 'comment';

export interface CaseFieldValue {
  fieldDefinitionId: string;
  fieldVersionId: string;
  code: string;
  label: string;
  fieldType: string;
  value: unknown;
  required: boolean;
}

export interface CaseNodeProgress {
  nodeId: string;
  nodeKey: string;
  label: string;
  nodeType: NodeType;
  requiredRoleId: string | null;
  requiredRoleName: string | null;
  assigneeUserId: string | null;
  assigneeName: string | null;
  status: NodeStatus;
  expectedHours: number | null;
  enteredAt: string | null;
  completedAt: string | null;
  modifiedExpectedAt: string | null;
  comment: string | null;
}

export interface CaseAction {
  id: string;
  caseId: string;
  caseNodeId: string | null;
  actionType: CaseActionType;
  actorUserId: string;
  actorName: string;
  actionAt: string;
  comment: string | null;
  metadata?: Record<string, unknown>;
}

export interface CaseRelationItem {
  caseId: string;
  caseNumber: string;
  title: string;
  status: CaseStatus;
  relationType: CaseRelationType;
  iAmChild: boolean;
}

export interface CaseSummary {
  id: string;
  caseNumber: string;
  title: string;
  status: CaseStatus;
  templateId: string;
  templateCode: string;
  templateName: string;
  templateVersion: number;
  documentTypeCode: string;
  customerId: string | null;
  customerName: string | null;
  initiatorUserId: string;
  initiatorName: string;
  initiatedAt: string;
  originalExpectedAt: string | null;
  expectedCompletionAt: string | null;
  completedAt: string | null;
  voidedAt: string | null;
  customVersion: string | null;
  currentNodeKey: string | null;
  currentAssigneeName: string | null;
  currentAssigneeUserId: string | null;
}

export interface CaseDetail extends CaseSummary {
  description: string | null;
  fields: CaseFieldValue[];
  nodes: CaseNodeProgress[];
  actions: CaseAction[];
  relations: CaseRelationItem[];
  currentNodeId: string | null;
}

export interface CaseListParams {
  status?: CaseStatus;
  mineOnly?: boolean;
  templateCode?: string;
  customerId?: string;
}

export interface CaseCreatePayload {
  templateId: string;
  documentTypeCode: string;
  title: string;
  description?: string;
  customerId?: string | null;
  expectedCompletionAt?: string | null;
  customVersion?: string | null;
  initialAssigneeUserId?: string | null;
  fieldValues: Array<{ fieldDefinitionId: string; value: unknown }>;
}

export interface CaseAssignPayload {
  userId: string;
  comment?: string;
}

export interface CaseActionPayload {
  comment?: string;
}

export interface SpawnChildPayload {
  templateId: string;
  title: string;
  description?: string;
  copyFieldDefinitionIds?: string[];
}

export interface ReopenPayload {
  title: string;
  description?: string;
  templateId?: string;
}

export interface UpdateExpectedCompletionPayload {
  expectedCompletionAt: string;
  comment?: string;
}

export const casesApi = {
  list: (params?: CaseListParams) =>
    apiClient.get<{ items: CaseSummary[]; totalCount: number }>('/cases', { params }).then((r) => r.data),

  get: (id: string) => apiClient.get<CaseDetail>(`/cases/${id}`).then((r) => r.data),

  create: (payload: CaseCreatePayload) =>
    apiClient.post<CaseDetail>('/cases', payload).then((r) => r.data),

  assign: (id: string, payload: CaseAssignPayload) =>
    apiClient.put<CaseDetail>(`/cases/${id}/assign`, payload).then((r) => r.data),

  accept: (id: string, payload: CaseActionPayload) =>
    apiClient.post<CaseDetail>(`/cases/${id}/actions/accept`, payload).then((r) => r.data),

  replyClose: (id: string, payload: CaseActionPayload) =>
    apiClient.post<CaseDetail>(`/cases/${id}/actions/reply-close`, payload).then((r) => r.data),

  approve: (id: string, payload: CaseActionPayload) =>
    apiClient.post<CaseDetail>(`/cases/${id}/actions/approve`, payload).then((r) => r.data),

  reject: (id: string, payload: CaseActionPayload) =>
    apiClient.post<CaseDetail>(`/cases/${id}/actions/reject`, payload).then((r) => r.data),

  voidCase: (id: string, payload: CaseActionPayload) =>
    apiClient.post<CaseDetail>(`/cases/${id}/actions/void`, payload).then((r) => r.data),

  spawnChild: (id: string, payload: SpawnChildPayload) =>
    apiClient.post<CaseDetail>(`/cases/${id}/actions/spawn-child`, payload).then((r) => r.data),

  reopen: (id: string, payload: ReopenPayload) =>
    apiClient.post<CaseDetail>(`/cases/${id}/actions/reopen`, payload).then((r) => r.data),

  updateExpectedCompletion: (id: string, payload: UpdateExpectedCompletionPayload) =>
    apiClient.put<CaseDetail>(`/cases/${id}/expected-completion`, payload).then((r) => r.data),
};

export const CASE_STATUS_META: Record<CaseStatus, { label: string; color: string }> = {
  in_progress: { label: '進行中', color: 'processing' },
  completed: { label: '已結案', color: 'success' },
  voided: { label: '已作廢', color: 'default' },
};

export const NODE_STATUS_META: Record<NodeStatus, { label: string; color: string }> = {
  pending: { label: '待進入', color: 'default' },
  in_progress: { label: '進行中', color: 'processing' },
  completed: { label: '已完成', color: 'success' },
  skipped: { label: '已略過', color: 'warning' },
  rejected: { label: '退回', color: 'error' },
};

export const ACTION_TYPE_LABEL: Record<CaseActionType, string> = {
  create: '發起案件',
  assign: '指派',
  accept: '接單',
  reply_close: '回覆結案',
  approve: '核准',
  reject: '退回',
  void: '作廢',
  spawn_child: '衍生子流程',
  reopen: '結案後重開',
  expected_completion_change: '修改預計完成時間',
  comment: '留言',
};
