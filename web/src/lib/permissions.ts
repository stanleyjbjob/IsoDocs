/**
 * 系統權限目錄（前端常數）。
 *
 * 對應後端 #6 [2.2.1] 的 `Role.Permissions` JSON 結構。
 * 後端不需要知道 UI 標籤，所以這份 catalog 由前端維護；後端只儲存 permission key 字串清單。
 *
 * **Key 命名規則**：`<resource>.<action>`，全小寫底線分隔。
 * **新增權限流程**：
 * 1. 在這份檔案加上 PermissionDef
 * 2. 後端 #6 不需改任何東西（純字串 key）
 * 3. 角色設定 UI 會自動列出新權限
 */

export interface PermissionDef {
  /** 權限唯一 key，例如 'cases.create'。後端 Role.Permissions JSON 儲存的就是這個字串。 */
  key: string;
  /** 中文顯示標籤 */
  label: string;
  /** 簡短說明（可選） */
  description?: string;
  /** 分類 key，用於 UI grouping */
  category: PermissionCategoryKey;
}

export type PermissionCategoryKey =
  | 'cases'
  | 'templates'
  | 'fields'
  | 'customers'
  | 'roles'
  | 'users'
  | 'system';

export interface PermissionCategory {
  key: PermissionCategoryKey;
  label: string;
  description?: string;
}

export const PERMISSION_CATEGORIES: readonly PermissionCategory[] = [
  { key: 'cases', label: '案件' },
  { key: 'templates', label: '流程範本' },
  { key: 'fields', label: '自訂欄位' },
  { key: 'customers', label: '客戶資料' },
  { key: 'roles', label: '角色管理' },
  { key: 'users', label: '使用者管理' },
  { key: 'system', label: '系統管理' },
];

/**
 * 權限目錄。issue 推進時逐步擴充。
 *
 * 使用方式：
 * - 角色設定 UI 列舉這份 catalog 顯示 checkbox 樹
 * - 前端 PermissionGate / hasPermission() 判斷時對 user 的 effective permissions 做 includes() 比對
 * - 後端 RBAC 端點（#6）只負責 JSON 字串清單的存取，不關心顯示
 */
export const PERMISSIONS: readonly PermissionDef[] = [
  // ===== 案件 =====
  { key: 'cases.view', label: '檢視案件', category: 'cases' },
  { key: 'cases.view_all', label: '檢視全公司案件', description: '管理者全公司視角', category: 'cases' },
  { key: 'cases.create', label: '發起案件', category: 'cases' },
  { key: 'cases.assign', label: '指派承辦', category: 'cases' },
  { key: 'cases.accept', label: '接單', category: 'cases' },
  { key: 'cases.reply_close', label: '回覆結案', category: 'cases' },
  { key: 'cases.approve', label: '核准／結案', category: 'cases' },
  { key: 'cases.reject', label: '退回', category: 'cases' },
  { key: 'cases.spawn_child', label: '衍生子流程', category: 'cases' },
  { key: 'cases.void', label: '作廢案件', description: '主單作廢會連鎖子流程', category: 'cases' },
  { key: 'cases.reopen', label: '結案後重開', category: 'cases' },
  { key: 'cases.comment', label: '留言', category: 'cases' },
  { key: 'cases.attach', label: '上傳附件', category: 'cases' },
  { key: 'cases.export_pdf', label: '匯出 PDF', category: 'cases' },

  // ===== 流程範本 =====
  { key: 'templates.view', label: '檢視範本', category: 'templates' },
  { key: 'templates.manage', label: '建立／編輯範本', category: 'templates' },
  { key: 'templates.publish', label: '發行範本新版本', category: 'templates' },

  // ===== 自訂欄位 =====
  { key: 'fields.view', label: '檢視欄位', category: 'fields' },
  { key: 'fields.manage', label: '建立／編輯欄位', category: 'fields' },

  // ===== 客戶資料 =====
  { key: 'customers.view', label: '檢視客戶', category: 'customers' },
  { key: 'customers.manage', label: '建立／編輯／停用客戶', category: 'customers' },

  // ===== 角色管理 =====
  { key: 'roles.view', label: '檢視角色清單', category: 'roles' },
  { key: 'roles.manage', label: '建立／編輯／停用角色', category: 'roles' },

  // ===== 使用者管理 =====
  { key: 'users.view', label: '檢視使用者清單', category: 'users' },
  { key: 'users.invite', label: '邀請新成員', category: 'users' },
  { key: 'users.assign_roles', label: '指派使用者角色', category: 'users' },
  { key: 'users.set_delegation', label: '設定代理', category: 'users' },

  // ===== 系統管理 =====
  { key: 'system.view_audit', label: '檢視稽核軌跡', category: 'system' },
  { key: 'system.admin_dashboard', label: '管理者儀表板', category: 'system' },
];

/** 將權限清單依分類 group。供 UI 使用。 */
export function groupPermissionsByCategory(): Record<PermissionCategoryKey, PermissionDef[]> {
  const result = {
    cases: [] as PermissionDef[],
    templates: [] as PermissionDef[],
    fields: [] as PermissionDef[],
    customers: [] as PermissionDef[],
    roles: [] as PermissionDef[],
    users: [] as PermissionDef[],
    system: [] as PermissionDef[],
  };
  for (const p of PERMISSIONS) {
    result[p.category].push(p);
  }
  return result;
}

/** 把一個 permission key 字串解析回 PermissionDef（找不到回 null）。 */
export function findPermission(key: string): PermissionDef | null {
  return PERMISSIONS.find((p) => p.key === key) ?? null;
}
