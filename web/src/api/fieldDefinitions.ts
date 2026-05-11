/**
 * 自訂欄位定義 API client。
 *
 * 端點契約對齊 issue #7 [3.1.1] 後端：
 * - GET    /api/field-definitions                  列出所有欄位（支援 ?includeInactive=true）
 * - POST   /api/field-definitions                  建立欄位（自動 version=1）
 * - PUT    /api/field-definitions/{id}              更新欄位 → 後端自動建立新 FieldVersion 快照
 * - GET    /api/field-definitions/{id}/versions     取得欄位的版本歷史（最新在前）
 *
 * **重要設計**（對應驗收條件「異動時顯示警告說明（不影響既有紀錄）」）：
 * 後端在每次 PUT 時會自動 increment `version` 並建立 `FieldVersion` 快照。已建立的案件記錄會 reference 當時的 `FieldVersion`，因此修改不影響歷史紀錄；只影響「之後建立的案件」。
 *
 * 後端 #7 落地前，可由 `mockFieldDefinitions.ts` 提供假資料（VITE_USE_MOCK_FIELDS=true 時 axios interceptor 會攔截）。
 */

import { apiClient } from './client';
import type { FieldType, FieldConfig } from '../lib/fieldTypes';

/**
 * 欄位定義（最新版本）。
 *
 * `id` 在欄位生命週期內不變（version 變動不影響 id）。歷史快照存於 `FieldVersion`。
 */
export interface FieldDefinition {
  /** 欄位穩定 ID（version 變動不變） */
  id: string;
  /** Machine-readable 識別碼，例如 'case.expected_completion_date'。一旦建立不可改。 */
  code: string;
  /** 顯示標籤（中文友善名稱） */
  label: string;
  /** 給管理者看的簡短說明 */
  description?: string;
  /** 欄位類型，對應 `lib/fieldTypes.ts` `FieldType` */
  fieldType: FieldType;
  /** 是否必填（驗收條件「必填設定」） */
  isRequired: boolean;
  /** 型別特定 config，對應 `lib/fieldTypes.ts` `FieldConfig` */
  config?: FieldConfig;
  /** 目前最新版本號。建立時 = 1，每次 PUT 後 += 1 */
  version: number;
  /** 啟用狀態。停用後新案件不再使用，但歷史紀錄仍能渲染（透過 FieldVersion 快照） */
  isActive: boolean;
  /** 建立時間 ISO 字串 */
  createdAt: string;
  /** 最後修改時間 ISO 字串（每次 PUT 都會更新） */
  updatedAt: string;
}

export interface FieldDefinitionCreatePayload {
  code: string;
  label: string;
  description?: string;
  fieldType: FieldType;
  isRequired: boolean;
  config?: FieldConfig;
}

export interface FieldDefinitionUpdatePayload {
  label?: string;
  description?: string;
  /** 不允許在 PUT 改類型 — 變更類型會破壞歷史資料相容性，需要建立新欄位代替 */
  fieldType?: never;
  /** 不允許在 PUT 改 code — 同上 */
  code?: never;
  isRequired?: boolean;
  config?: FieldConfig;
  isActive?: boolean;
  /** 異動說明（可選，會寫入 FieldVersion.changeNote 做稽核） */
  changeNote?: string;
}

/**
 * 一個欄位的歷史快照。
 *
 * 後端 `FieldVersion` 表的對應前端型別。每次 PUT 都會新增一筆。
 */
export interface FieldDefinitionVersion {
  id: string;
  fieldDefinitionId: string;
  version: number;
  /** 該版本當時的完整快照（不含 version / createdAt / updatedAt 這幾個 meta） */
  snapshot: Omit<FieldDefinition, 'version' | 'createdAt' | 'updatedAt'>;
  /** 該版本建立時間 ISO 字串 */
  createdAt: string;
  /** 建立者顯示名稱（後端 join 後可帶回） */
  createdBy?: string;
  /** 異動說明 */
  changeNote?: string;
}

// ---------- API 函式 ----------

export async function listFieldDefinitions(includeInactive = false): Promise<FieldDefinition[]> {
  const { data } = await apiClient.get<FieldDefinition[]>('/field-definitions', {
    params: includeInactive ? { includeInactive: true } : undefined,
  });
  return data;
}

export async function createFieldDefinition(
  payload: FieldDefinitionCreatePayload,
): Promise<FieldDefinition> {
  const { data } = await apiClient.post<FieldDefinition>('/field-definitions', payload);
  return data;
}

export async function updateFieldDefinition(
  id: string,
  payload: FieldDefinitionUpdatePayload,
): Promise<FieldDefinition> {
  const { data } = await apiClient.put<FieldDefinition>(`/field-definitions/${id}`, payload);
  return data;
}

export async function deactivateFieldDefinition(
  id: string,
  changeNote?: string,
): Promise<FieldDefinition> {
  return updateFieldDefinition(id, { isActive: false, changeNote });
}

export async function activateFieldDefinition(
  id: string,
  changeNote?: string,
): Promise<FieldDefinition> {
  return updateFieldDefinition(id, { isActive: true, changeNote });
}

export async function listFieldDefinitionVersions(
  id: string,
): Promise<FieldDefinitionVersion[]> {
  const { data } = await apiClient.get<FieldDefinitionVersion[]>(
    `/field-definitions/${id}/versions`,
  );
  return data;
}
