export type PermissionCode =
  | 'cases.read'
  | 'cases.create'
  | 'cases.assign'
  | 'cases.approve'
  | 'templates.read'
  | 'templates.write'
  | 'templates.publish'
  | 'templates.delete'
  | 'field_definitions.read'
  | 'field_definitions.write'
  | 'field_definitions.deactivate'
  | 'field_definitions.delete'
  | 'customers.read'
  | 'customers.write'
  | 'customers.deactivate'
  | 'customers.delete'
  | 'roles.read'
  | 'roles.manage'
  | 'roles.deactivate'
  | 'roles.delete'
  | 'users.read'
  | 'users.assign_roles'
  | 'users.invite'
  | 'users.deactivate'
  | 'system.admin'
  | 'system.audit'
  | 'system.config'
  | 'system.hangfire';

export interface PermissionItem {
  code: PermissionCode;
  label: string;
}

export interface PermissionCategory {
  key: string;
  label: string;
  permissions: PermissionItem[];
}

export const PERMISSION_CATEGORIES: PermissionCategory[] = [
  {
    key: 'cases',
    label: '案件管理',
    permissions: [
      { code: 'cases.read', label: '查看案件' },
      { code: 'cases.create', label: '建立案件' },
      { code: 'cases.assign', label: '指派/接單/核准/退回' },
      { code: 'cases.approve', label: '作廢案件' },
    ],
  },
  {
    key: 'templates',
    label: '流程範本',
    permissions: [
      { code: 'templates.read', label: '查看流程範本' },
      { code: 'templates.write', label: '編輯流程範本' },
      { code: 'templates.publish', label: '發行新版本' },
      { code: 'templates.delete', label: '刪除流程範本' },
    ],
  },
  {
    key: 'field_definitions',
    label: '自訂欄位',
    permissions: [
      { code: 'field_definitions.read', label: '查看欄位定義' },
      { code: 'field_definitions.write', label: '建立/編輯欄位' },
      { code: 'field_definitions.deactivate', label: '停用欄位' },
      { code: 'field_definitions.delete', label: '刪除欄位' },
    ],
  },
  {
    key: 'customers',
    label: '客戶管理',
    permissions: [
      { code: 'customers.read', label: '查看客戶' },
      { code: 'customers.write', label: '建立/編輯客戶' },
      { code: 'customers.deactivate', label: '停用客戶' },
      { code: 'customers.delete', label: '刪除客戶' },
    ],
  },
  {
    key: 'roles',
    label: '角色管理',
    permissions: [
      { code: 'roles.read', label: '查看角色' },
      { code: 'roles.manage', label: '建立/編輯角色與權限' },
      { code: 'roles.deactivate', label: '停用角色' },
      { code: 'roles.delete', label: '刪除角色' },
    ],
  },
  {
    key: 'users',
    label: '使用者管理',
    permissions: [
      { code: 'users.read', label: '查看使用者' },
      { code: 'users.assign_roles', label: '指派使用者角色' },
      { code: 'users.invite', label: '邀請使用者' },
      { code: 'users.deactivate', label: '停用使用者' },
    ],
  },
  {
    key: 'system',
    label: '系統管理',
    permissions: [
      { code: 'system.admin', label: '系統管理者（全權）' },
      { code: 'system.audit', label: '稽核軌跡查詢' },
      { code: 'system.config', label: '系統設定' },
      { code: 'system.hangfire', label: 'Hangfire 排程管理' },
    ],
  },
];

export const ALL_PERMISSION_CODES: PermissionCode[] = PERMISSION_CATEGORIES.flatMap((c) =>
  c.permissions.map((p) => p.code),
);
