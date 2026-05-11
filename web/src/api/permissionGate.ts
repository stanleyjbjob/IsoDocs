/**
 * 前端權限判斷 hooks。
 *
 * 來源：MSAL ID Token 的 `roles` claim（Azure AD App Roles）→ AuthContext.user.roles。
 *
 * **設計取捨**：
 * - Azure AD App Roles 與 IsoDocs 自訂角色（#6 [2.2.1]）是兩個層級：
 *   - App Roles 由 Azure portal 設，存在 ID Token，前端可即時讀
 *   - IsoDocs Roles 由管理者於系統內建立，permissions 存 DB（後端 RBAC 檢查的真實依據）
 * - 前端的 `hasPermission()` 為**樂觀檢查**：根據 user.roles + 角色清單的 permissions 推導
 *   實際是否能執行仍由後端最終決定（深度防禦）
 * - 在後端 #6 RBAC 落地前，這份 hook 用 mock 的角色 → permissions 對應；落地後改打 /api/me 取最新
 *
 * 之後可以考慮把 effective permissions 計算搬到後端 `/api/me` 一次回傳，
 * 前端就不用維護 roles → permissions mapping。但本輪先做最小可用版本。
 */

import { useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../contexts/AuthContext';
import { listRoles, type Role } from './roles';

/** 預設管理者角色名稱（與 mockRoles.ts 內 'role-admin' 對應 + Azure AD app role 'IsoDocs.Admin'） */
const ADMIN_ROLE_NAMES = new Set<string>(['IsoDocs.Admin', '系統管理者', 'admin', 'Admin']);

export interface PermissionContext {
  /** 使用者擁有的所有 effective permissions（合併自所有 active 角色的 permissions union） */
  permissions: Set<string>;
  /** 使用者目前的角色名稱（顯示用） */
  roleNames: string[];
  /** 是否為管理者（角色名稱命中 ADMIN_ROLE_NAMES 任一個） */
  isAdmin: boolean;
  /** 是否還在載入角色資料 */
  isLoading: boolean;
}

/**
 * 取得當前使用者的權限脈絡。
 *
 * 注意：這個 hook 內含一個 useQuery，會打 GET /api/roles 取得「角色 → permissions」映射，
 * 然後與 user.roles（角色名稱清單）做 join 算出 effective permissions。
 *
 * 後端 #6 [2.2.1] 落地後可以考慮把這段移到 /api/me 回傳，避免每個權限檢查都觸發 /api/roles。
 */
export function usePermissionContext(): PermissionContext {
  const { user, isAuthenticated } = useAuth();

  const { data: allRoles, isLoading } = useQuery({
    queryKey: ['roles', 'effective'],
    queryFn: listRoles,
    enabled: isAuthenticated,
    staleTime: 5 * 60 * 1000, // 5 分鐘
  });

  return useMemo<PermissionContext>(() => {
    const userRoleNames = user?.roles ?? [];
    const isAdmin = userRoleNames.some((r) => ADMIN_ROLE_NAMES.has(r));

    if (!user || !allRoles) {
      return {
        permissions: new Set<string>(),
        roleNames: userRoleNames,
        isAdmin,
        isLoading,
      };
    }

    const permissions = new Set<string>();
    for (const role of allRoles as Role[]) {
      if (!role.isActive) continue;
      // 比對：使用者的 role 名單若包含此 Role.name 或 Role.id，則合併其 permissions
      if (userRoleNames.includes(role.name) || userRoleNames.includes(role.id)) {
        for (const p of role.permissions) {
          permissions.add(p);
        }
      }
    }

    return {
      permissions,
      roleNames: userRoleNames,
      isAdmin,
      isLoading,
    };
  }, [user, allRoles, isLoading]);
}

/** 樂觀檢查：使用者是否擁有指定權限 */
export function useHasPermission(...keys: string[]): boolean {
  const { permissions, isAdmin } = usePermissionContext();
  if (isAdmin) return true; // admin 視為擁有全部權限（簡化）
  return keys.every((k) => permissions.has(k));
}

/** 任一命中即可（OR） */
export function useHasAnyPermission(...keys: string[]): boolean {
  const { permissions, isAdmin } = usePermissionContext();
  if (isAdmin) return true;
  return keys.some((k) => permissions.has(k));
}

/** 是否為管理者（純角色名稱比對，不依賴 /api/roles） */
export function useIsAdmin(): boolean {
  const { isAdmin } = usePermissionContext();
  return isAdmin;
}
