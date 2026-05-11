import { apiClient } from './client';
import type { PermissionCode } from '../lib/permissions';

export interface Role {
  id: string;
  name: string;
  description: string;
  permissions: PermissionCode[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateRolePayload {
  name: string;
  description?: string;
  permissions: PermissionCode[];
}

export interface UpdateRolePayload {
  name: string;
  description?: string;
  permissions: PermissionCode[];
  isActive?: boolean;
}

export interface UserSummary {
  id: string;
  email: string;
  displayName: string;
  department?: string;
  jobTitle?: string;
  isActive: boolean;
}

export interface UserRoleAssignment {
  roleId: string;
  roleName?: string;
  effectiveFrom?: string;
  effectiveTo?: string;
}

export const rolesApi = {
  list: (): Promise<Role[]> =>
    apiClient.get<Role[]>('/roles').then((r) => r.data),

  create: (payload: CreateRolePayload): Promise<Role> =>
    apiClient.post<Role>('/roles', payload).then((r) => r.data),

  update: (id: string, payload: UpdateRolePayload): Promise<Role> =>
    apiClient.put<Role>(`/roles/${id}`, payload).then((r) => r.data),

  deactivate: (id: string): Promise<void> =>
    apiClient.post(`/roles/${id}/deactivate`).then(() => undefined),

  activate: (id: string): Promise<void> =>
    apiClient.post(`/roles/${id}/activate`).then(() => undefined),

  listUsers: (): Promise<UserSummary[]> =>
    apiClient.get<UserSummary[]>('/users').then((r) => r.data),

  getUserRoles: (userId: string): Promise<UserRoleAssignment[]> =>
    apiClient.get<UserRoleAssignment[]>(`/users/${userId}/roles`).then((r) => r.data),

  assignUserRoles: (userId: string, assignments: UserRoleAssignment[]): Promise<void> =>
    apiClient.put(`/users/${userId}/roles`, assignments).then(() => undefined),

  getEffectivePermissions: (): Promise<PermissionCode[]> =>
    apiClient.get<PermissionCode[]>('/roles/effective').then((r) => r.data),
};
