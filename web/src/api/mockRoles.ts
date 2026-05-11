import { rolesApi, type Role, type UserSummary, type UserRoleAssignment } from './roles';
import { ALL_PERMISSION_CODES, type PermissionCode } from '../lib/permissions';

const MOCK_ROLES: Role[] = [
  {
    id: 'role-admin',
    name: 'Admin',
    description: '系統管理者，具備全部權限',
    permissions: [...ALL_PERMISSION_CODES],
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'role-consultant',
    name: 'Consultant',
    description: '顧問，可建立與處理案件',
    permissions: ['cases.read', 'cases.create', 'cases.assign', 'customers.read', 'field_definitions.read'],
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'role-reviewer',
    name: 'Reviewer',
    description: '審核者，可核准或退回案件',
    permissions: ['cases.read', 'cases.approve', 'customers.read'],
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  },
  {
    id: 'role-readonly',
    name: 'ReadOnly',
    description: '唯讀，只能查看案件',
    permissions: ['cases.read', 'customers.read'],
    isActive: false,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
  },
];

const MOCK_USERS: UserSummary[] = [
  {
    id: 'user-alice',
    email: 'alice@example.com',
    displayName: 'Alice Wang',
    department: 'IT',
    jobTitle: 'System Admin',
    isActive: true,
  },
  {
    id: 'user-bob',
    email: 'bob@example.com',
    displayName: 'Bob Chen',
    department: 'Engineering',
    jobTitle: 'Consultant',
    isActive: true,
  },
  {
    id: 'user-carol',
    email: 'carol@example.com',
    displayName: 'Carol Lin',
    department: 'Quality',
    jobTitle: 'Reviewer',
    isActive: true,
  },
  {
    id: 'user-dave',
    email: 'dave@example.com',
    displayName: 'Dave Huang',
    department: 'Sales',
    jobTitle: 'Junior Consultant',
    isActive: false,
  },
];

const MOCK_USER_ROLES: Record<string, UserRoleAssignment[]> = {
  'user-alice': [{ roleId: 'role-admin', roleName: 'Admin' }],
  'user-bob': [
    { roleId: 'role-consultant', roleName: 'Consultant' },
    { roleId: 'role-reviewer', roleName: 'Reviewer' },
  ],
  'user-carol': [{ roleId: 'role-reviewer', roleName: 'Reviewer' }],
  'user-dave': [{ roleId: 'role-consultant', roleName: 'Consultant' }],
};

export function installMockRbac(): void {
  rolesApi.list = async () => JSON.parse(JSON.stringify(MOCK_ROLES)) as Role[];

  rolesApi.create = async (payload) => {
    const role: Role = {
      id: `role-${Date.now()}`,
      name: payload.name,
      description: payload.description ?? '',
      permissions: payload.permissions,
      isActive: true,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
    MOCK_ROLES.push(role);
    return role;
  };

  rolesApi.update = async (id, payload) => {
    const idx = MOCK_ROLES.findIndex((r) => r.id === id);
    if (idx >= 0) {
      MOCK_ROLES[idx] = {
        ...MOCK_ROLES[idx],
        ...payload,
        id,
        updatedAt: new Date().toISOString(),
      } as Role;
      return MOCK_ROLES[idx];
    }
    throw new Error(`Role ${id} not found`);
  };

  rolesApi.deactivate = async (id) => {
    const role = MOCK_ROLES.find((r) => r.id === id);
    if (role) role.isActive = false;
  };

  rolesApi.activate = async (id) => {
    const role = MOCK_ROLES.find((r) => r.id === id);
    if (role) role.isActive = true;
  };

  rolesApi.listUsers = async () => JSON.parse(JSON.stringify(MOCK_USERS)) as UserSummary[];

  rolesApi.getUserRoles = async (userId) =>
    JSON.parse(JSON.stringify(MOCK_USER_ROLES[userId] ?? [])) as UserRoleAssignment[];

  rolesApi.assignUserRoles = async (userId, assignments) => {
    MOCK_USER_ROLES[userId] = assignments;
  };

  rolesApi.getEffectivePermissions = async () => [...ALL_PERMISSION_CODES] as PermissionCode[];
}
