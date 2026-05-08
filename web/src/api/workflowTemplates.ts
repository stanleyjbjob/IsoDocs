/**
 * 流程範本 API client。
 *
 * 端點契約對齊 issue #12 [3.2.1] 後端：
 * - GET    /api/workflow-templates                       列出所有範本（支援 ?includeInactive=true）
 * - GET    /api/workflow-templates/{id}                   取得單一範本（最新版本內容）
 * - POST   /api/workflow-templates                        建立範本（draft 狀態，version=0）
 * - PUT    /api/workflow-templates/{id}                   更新 draft（不會 bump version）
 * - PUT    /api/workflow-templates/{id}/publish           發行新版本（version+=1，PublishedAt=NOW）
 * - GET    /api/workflow-templates/{id}/versions          取得範本版本歷史（最新在前）
 *
 * **重要設計**（對應驗收條件「範本異動僅套用新案件，進行中案件沿用建立時的 TemplateVersion」）：
 * 範本有兩種狀態：
 * 1. **Draft**（version=0 或 publishedAt=null）：草稿，不影響任何案件
 * 2. **Published**：已發行，新案件建立時會 freeze 當下的 TemplateVersion
 *
 * 已發行的範本仍可繼續編輯（編輯的是新的 draft），按下「發行」後才會 bump version + 更新 publishedAt。
 */

import { apiClient } from './client';
import type { WorkflowNodeType } from '../lib/workflowNodeTypes';

export interface WorkflowNode {
  /** 1-based 節點順序，由 UI 維護 */
  nodeOrder: number;
  /** 範本內唯一識別字串（例如 'start' / 'handler-1' / 'approve-1' / 'end'） */
  nodeKey: string;
  /** 顯示標籤 */
  label: string;
  /** 節點類型，對應 `lib/workflowNodeTypes.ts` `WorkflowNodeType` */
  nodeType: WorkflowNodeType;
  /** 必要角色（後端 Role.Id）。`handle` / `approve` 強制需要；`notify` 可選；`start` / `end` 不需要 */
  requiredRoleId?: string;
  /** 節點說明（給管理者看的） */
  description?: string;
  /** 節點預估處理時間（小時，可選） */
  expectedHours?: number;
}

/**
 * 流程範本（最新編輯狀態）。
 *
 * 已發行版本的歷史快照存於 `WorkflowTemplateVersion`。
 */
export interface WorkflowTemplate {
  /** 範本穩定 ID（version 變動不變） */
  id: string;
  /** Machine-readable 識別碼，例如 'work_request' / 'spec_change'。一旦建立不可改。 */
  code: string;
  /** 顯示名稱 */
  name: string;
  /** 範本說明 */
  description?: string;
  /** 目前已發行版本號。0 表示還是 draft，未發行過 */
  version: number;
  /** 節點清單，依 nodeOrder 升冪 */
  nodes: WorkflowNode[];
  /** 上次發行時間 ISO 字串。null 表示尚未發行 */
  publishedAt: string | null;
  /** 啟用狀態（停用後新案件不能選此範本，但既有案件仍可正常運作） */
  isActive: boolean;
  /** 是否有未發行的草稿變更（最新編輯內容 vs 已發行版本） */
  hasDraftChanges: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface WorkflowTemplateCreatePayload {
  code: string;
  name: string;
  description?: string;
  nodes: WorkflowNode[];
}

export interface WorkflowTemplateUpdatePayload {
  /** 不允許在 PUT 改 code — 變更會破壞案件 reference */
  code?: never;
  name?: string;
  description?: string;
  /** 節點清單；未傳則不變 */
  nodes?: WorkflowNode[];
  isActive?: boolean;
}

export interface WorkflowTemplatePublishPayload {
  /** 發行說明（會寫入 WorkflowTemplateVersion.changeNote 做稽核） */
  changeNote?: string;
}

export interface WorkflowTemplateVersion {
  id: string;
  templateId: string;
  version: number;
  /** 該版本完整快照 */
  snapshot: Pick<
    WorkflowTemplate,
    'id' | 'code' | 'name' | 'description' | 'nodes' | 'isActive'
  > & { version: number };
  publishedAt: string;
  publishedBy?: string;
  changeNote?: string;
}

// ---------- API 函式 ----------

export async function listWorkflowTemplates(includeInactive = false): Promise<WorkflowTemplate[]> {
  const { data } = await apiClient.get<WorkflowTemplate[]>('/workflow-templates', {
    params: includeInactive ? { includeInactive: true } : undefined,
  });
  return data;
}

export async function getWorkflowTemplate(id: string): Promise<WorkflowTemplate> {
  const { data } = await apiClient.get<WorkflowTemplate>(`/workflow-templates/${id}`);
  return data;
}

export async function createWorkflowTemplate(
  payload: WorkflowTemplateCreatePayload,
): Promise<WorkflowTemplate> {
  const { data } = await apiClient.post<WorkflowTemplate>('/workflow-templates', payload);
  return data;
}

export async function updateWorkflowTemplate(
  id: string,
  payload: WorkflowTemplateUpdatePayload,
): Promise<WorkflowTemplate> {
  const { data } = await apiClient.put<WorkflowTemplate>(`/workflow-templates/${id}`, payload);
  return data;
}

export async function publishWorkflowTemplate(
  id: string,
  payload?: WorkflowTemplatePublishPayload,
): Promise<WorkflowTemplate> {
  const { data } = await apiClient.put<WorkflowTemplate>(
    `/workflow-templates/${id}/publish`,
    payload ?? {},
  );
  return data;
}

export async function deactivateWorkflowTemplate(id: string): Promise<WorkflowTemplate> {
  return updateWorkflowTemplate(id, { isActive: false });
}

export async function activateWorkflowTemplate(id: string): Promise<WorkflowTemplate> {
  return updateWorkflowTemplate(id, { isActive: true });
}

export async function listWorkflowTemplateVersions(
  id: string,
): Promise<WorkflowTemplateVersion[]> {
  const { data } = await apiClient.get<WorkflowTemplateVersion[]>(
    `/workflow-templates/${id}/versions`,
  );
  return data;
}
