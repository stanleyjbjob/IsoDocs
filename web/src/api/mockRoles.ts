/**
 * 開發模式假資料 + axios interceptor。
 *
 * 啟用方式：在 `web/.env.development` 設 `VITE_USE_MOCK_RBAC=true`。
 * 後端 #6 [2.2.1] 落地後可關掉這個 flag，前端會自動改打真實 API。
 *
 * 攔截規則（僅 GET / POST / PUT 到 `/roles*` 與 `/users*`）：
 * - GET  /roles                  → MOCK_ROLES
 * - POST /roles                  → push 進 MOCK_ROLES，回傳新建 Role
 * - PUT  /roles/{id}             → 更新 MOCK_ROLES[id]
 * - GET  /users                  → MOCK_USERS
 * - PUT  /users/{id}/roles       → 更新 MOCK_USERS[id].roles
 *
 * **不會**攔截非 RBAC 端點，避免污染整個 axios client。
 */

import type { AxiosInstance } from 'axios';
import type { Role, UserSummary } from './roles';

const NOW = () => new Date().toISOString();

const MOCK_ROLES: Role[] = [
  {
    id: 'role-admin',
    name: '系統管理者',
    description: '擁有所有權限，包含管理者儀表板與稽核軌跡',
    permissions: [
      'cases.view_all',
      'cases.create',
      'cases.assign',
      'cases.approve',
      'cases.void',
      'cases.reopen',
      'templates.manage',
      'templates.publish',
      'fields.manage',
      'customers.manage',
      'roles.view',
      'roles.manage',
      'users.view',
      'users.invite',
      'users.assign_roles',
      'users.set_delegation',
      'system.view_audit',
      'system.admin_dashboard',
    ],
    isActive: true,
    createdAt: '2026-04-01T00:00:00Z',
    updatedAt: '2026-04-15T00:00:00Z',
  },
  {
    id: 'role-consultant',
    name: '顧問',
    description: '可發起、處理、作廢案件',
    permissions: [
      'cases.view',
      'cases.create',
      'cases.accept',
      'cases.reply_close',
      'cases.approve',
      'cases.reject',
      'cases.spawn_child',
      'cases.void',
      'cases.comment',
      'cases.attach',
      'cases.export_pdf',
      'templates.view',
      'fields.view',
      'customers.view',
    ],
    isActive: true,
    createdAt: '2026-04-01T00:00:00Z',
    updatedAt: '2026-04-01T00:00:00Z',
  },
  {
    id: 'role-developer',
    name: '開發人員',
    description: '處理派單、回覆案件',
    permissions: [
      'cases.view',
      'cases.accept',
      'cases.reply_close',
      'cases.comment',
      'cases.attach',
      'templates.view',
    ],
    isActive: true,
    createdAt: '2026-04-01T00:00:00Z',
    updatedAt: '2026-04-01T00:00:00Z',
  },
  {
    id: 'role-viewer',
    name: '檢視者',
    description: '只能檢視案件、留言、附件',
    permissions: ['cases.view', 'cases.comment', 'templates.view'],
    isActive: false,
    createdAt: '2026-04-01T00:00:00Z',
    updatedAt: '2026-04-20T00:00:00Z',
  },
];

const MOCK_USERS: UserSummary[] = [
  {
    id: 'user-1',
    email: 'alice@example.com',
    displayName: 'Alice Chen',
    department: '技術部',
    jobTitle: '技術主管',
    isActive: true,
    roles: [{ roleId: 'role-admin', roleName: '系統管理者', effectiveFrom: '2026-01-01T00:00:00Z' }],
  },
  {
    id: 'user-2',
    email: 'bob@example.com',
    displayName: 'Bob Liu',
    department: '顧問部',
    jobTitle: '資深顧問',
    isActive: true,
    roles: [
      { roleId: 'role-consultant', roleName: '顧問', effectiveFrom: '2026-01-01T00:00:00Z' },
    ],
  },
  {
    id: 'user-3',
    email: 'carol@example.com',
    displayName: 'Carol Wang',
    department: '技術部',
    jobTitle: '工程師',
    isActive: true,
    roles: [
      {
        roleId: 'role-developer',
        roleName: '開發人員',
        effectiveFrom: '2026-01-01T00:00:00Z',
      },
      // 複合角色範例：同時是顧問
      {
        roleId: 'role-consultant',
        roleName: '顧問',
        effectiveFrom: '2026-03-01T00:00:00Z',
        effectiveTo: '2026-06-30T00:00:00Z',
      },
    ],
  },
  {
    id: 'user-4',
    email: 'david@example.com',
    displayName: 'David Lin',
    department: '營業部',
    jobTitle: '業務',
    isActive: false,
    roles: [],
  },
];

// ---------- helpers ----------

function matchUrl(url: string | undefined, path: string): boolean {
  if (!url) return false;
  return url === path || url.endsWith(path);
}

function matchUrlPattern(url: string | undefined, pattern: RegExp): RegExpMatchArray | null {
  if (!url) return null;
  return url.match(pattern);
}

function makeId(prefix: string): string {
  return `${prefix}-${Math.random().toString(36).slice(2, 10)}`;
}

function roleByIdOrThrow(id: string): Role {
  const role = MOCK_ROLES.find((r) => r.id === id);
  if (!role) throw new Error(`Mock: role not found: ${id}`);
  return role;
}

function userByIdOrThrow(id: string): UserSummary {
  const user = MOCK_USERS.find((u) => u.id === id);
  if (!user) throw new Error(`Mock: user not found: ${id}`);
  return user;
}

/**
 * 安裝 mock interceptor。在 main.tsx bootstrap 時依環境變數決定是否啟用。
 * - 命中 RBAC 路由：以 mock 結果短路，response.data 為 mock 物件
 * - 不命中：放行給真實後端
 */
export function installMockRbacInterceptor(client: AxiosInstance): void {
  client.interceptors.request.use(async (config) => {
    const url = config.url ?? '';
    const method = (config.method ?? 'get').toLowerCase();

    // GET /roles
    if (method === 'get' && matchUrl(url, '/roles')) {
      return Promise.reject({
        __mock: true,
        config,
        status: 200,
        data: structuredClone(MOCK_ROLES),
      });
    }

    // POST /roles
    if (method === 'post' && matchUrl(url, '/roles')) {
      const payload = config.data ? JSON.parse(config.data as string) : {};
      const newRole: Role = {
        id: makeId('role'),
        name: payload.name ?? '未命名角色',
        description: payload.description,
        permissions: payload.permissions ?? [],
        isActive: true,
        createdAt: NOW(),
        updatedAt: NOW(),
      };
      MOCK_ROLES.push(newRole);
      return Promise.reject({ __mock: true, config, status: 201, data: structuredClone(newRole) });
    }

    // PUT /roles/{id}
    const rolePut = matchUrlPattern(url, /\/roles\/([^/?]+)$/);
    if (method === 'put' && rolePut) {
      const id = rolePut[1];
      const role = roleByIdOrThrow(id);
      const payload = config.data ? JSON.parse(config.data as string) : {};
      if (payload.name !== undefined) role.name = payload.name;
      if (payload.description !== undefined) role.description = payload.description;
      if (payload.permissions !== undefined) role.permissions = payload.permissions;
      if (payload.isActive !== undefined) role.isActive = payload.isActive;
      role.updatedAt = NOW();
      return Promise.reject({ __mock: true, config, status: 200, data: structuredClone(role) });
    }

    // GET /users
    if (method === 'get' && matchUrl(url, '/users')) {
      return Promise.reject({
        __mock: true,
        config,
        status: 200,
        data: structuredClone(MOCK_USERS),
      });
    }

    // PUT /users/{id}/roles
    const userRolesPut = matchUrlPattern(url, /\/users\/([^/]+)\/roles$/);
    if (method === 'put' && userRolesPut) {
      const id = userRolesPut[1];
      const user = userByIdOrThrow(id);
      const payload = config.data ? JSON.parse(config.data as string) : { roles: [] };
      user.roles = (payload.roles ?? []).map(
        (r: { roleId: string; effectiveFrom?: string; effectiveTo?: string | null }) => {
          const role = MOCK_ROLES.find((mr) => mr.id === r.roleId);
          return {
            roleId: r.roleId,
            roleName: role?.name,
            effectiveFrom: r.effectiveFrom ?? NOW(),
            effectiveTo: r.effectiveTo ?? null,
          };
        },
      );
      return Promise.reject({ __mock: true, config, status: 200, data: structuredClone(user) });
    }

    // 不命中 → 放行
    return config;
  });

  // 把上面 reject 出來的 mock response 轉回 resolved response
  client.interceptors.response.use(
    (response) => response,
    (error) => {
      if (error && error.__mock) {
        return Promise.resolve({
          data: error.data,
          status: error.status,
          statusText: 'OK (mock)',
          headers: {},
          config: error.config,
        });
      }
      return Promise.reject(error);
    },
  );
}

export const __TEST_ONLY = { MOCK_ROLES, MOCK_USERS };
