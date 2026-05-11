/**
 * RBAC API client。
 *
 * 端點契約對齊 issue #6 [2.2.1]：
 * - GET    /api/roles                 列出所有角色
 * - POST   /api/roles                 建立角色
 * - PUT    /api/roles/{id}            更新角色（含停用 isActive=false）
 * - GET    /api/users                 列出所有使用者（簡化版，分頁交給後端）
 * - PUT    /api/users/{id}/roles      指派複合角色（取代式：傳入 roleIds 即覆寫）
 *
 * 後端 #6 落地前，可由 `mockRoles.ts` 提供假資料（VITE_USE_MOCK_RBAC=true 時 axios interceptor 會攔截）。
 */

import { apiClient } from './client';

export interface Role {
  id: string;
  name: string;
  description?: string;
  /** Permission key 字串清單，對應 `lib/permissions.ts` PERMISSIONS 的 key */
  permissions: string[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface RoleCreatePayload {
  name: string;
  description?: string;
  permissions: string[];
}

export interface RoleUpdatePayload {
  name?: string;
  description?: string;
  permissions?: string[];
  isActive?: boolean;
}

export interface UserRoleAssignment {
  /** 對應後端 UserRole.RoleId */
  roleId: string;
  /** 顯示用：對應角色當下名稱（後端可選擇 join 後回傳） */
  roleName?: string;
  /** ISO 日期字串。null/undefined 表示「即日生效」 */
  effectiveFrom?: string | null;
  /** ISO 日期字串。null 表示無到期日 */
  effectiveTo?: string | null;
}

export interface UserSummary {
  id: string;
  email: string;
  displayName: string;
  /** 部門 / 職稱（可選） */
  department?: string;
  jobTitle?: string;
  isActive: boolean;
  /** 當前生效中的角色指派（後端應只回傳 EffectiveFrom <= now AND (EffectiveTo IS NULL OR EffectiveTo > now)） */
  roles: UserRoleAssignment[];
}

export interface AssignUserRolesPayload {
  /** 取代式：傳入後即為使用者的完整角色清單 */
  roles: Array<{
    roleId: string;
    effectiveFrom?: string | null;
    effectiveTo?: string | null;
  }>;
}

// ---------- API 函式 ----------

export async function listRoles(): Promise<Role[]> {
  const { data } = await apiClient.get<Role[]>('/roles');
  return data;
}

export async function createRole(payload: RoleCreatePayload): Promise<Role> {
  const { data } = await apiClient.post<Role>('/roles', payload);
  return data;
}

export async function updateRole(id: string, payload: RoleUpdatePayload): Promise<Role> {
  const { data } = await apiClient.put<Role>(`/roles/${id}`, payload);
  return data;
}

export async function deactivateRole(id: string): Promise<Role> {
  return updateRole(id, { isActive: false });
}

export async function activateRole(id: string): Promise<Role> {
  return updateRole(id, { isActive: true });
}

export async function listUsers(): Promise<UserSummary[]> {
  const { data } = await apiClient.get<UserSummary[]>('/users');
  return data;
}

export async function assignUserRoles(
  userId: string,
  payload: AssignUserRolesPayload,
): Promise<UserSummary> {
  const { data } = await apiClient.put<UserSummary>(`/users/${userId}/roles`, payload);
  return data;
}
