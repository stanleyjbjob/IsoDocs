export { apiClient, createApiClient } from './client';
export type {
  Role,
  RoleCreatePayload,
  RoleUpdatePayload,
  UserRoleAssignment,
  UserSummary,
  AssignUserRolesPayload,
} from './roles';
export {
  listRoles,
  createRole,
  updateRole,
  deactivateRole,
  activateRole,
  listUsers,
  assignUserRoles,
} from './roles';
export { installMockRbacInterceptor } from './mockRoles';
export {
  usePermissionContext,
  useHasPermission,
  useHasAnyPermission,
  useIsAdmin,
} from './permissionGate';
