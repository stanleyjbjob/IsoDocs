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

// issue #11 [3.1.2] 自訂欄位
export type {
  FieldDefinition,
  FieldDefinitionCreatePayload,
  FieldDefinitionUpdatePayload,
  FieldDefinitionVersion,
} from './fieldDefinitions';
export {
  listFieldDefinitions,
  createFieldDefinition,
  updateFieldDefinition,
  activateFieldDefinition,
  deactivateFieldDefinition,
  listFieldDefinitionVersions,
} from './fieldDefinitions';
export { installMockFieldsInterceptor } from './mockFieldDefinitions';

// issue #13 [3.2.2] 流程範本設計器（前端）
export type {
  WorkflowNode,
  WorkflowTemplate,
  WorkflowTemplateCreatePayload,
  WorkflowTemplateUpdatePayload,
  WorkflowTemplatePublishPayload,
  WorkflowTemplateVersion,
} from './workflowTemplates';
export {
  listWorkflowTemplates,
  getWorkflowTemplate,
  createWorkflowTemplate,
  updateWorkflowTemplate,
  publishWorkflowTemplate,
  activateWorkflowTemplate,
  deactivateWorkflowTemplate,
  listWorkflowTemplateVersions,
} from './workflowTemplates';
export { installMockTemplatesInterceptor } from './mockWorkflowTemplates';
