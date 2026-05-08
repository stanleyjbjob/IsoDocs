# 角色與權限管理前端設計（issue #8 [2.2.2]）

## 目的

實作管理者介面，讓管理者可自訂角色、設定權限、並指派使用者的複合角色。對應 issue #8 驗收條件：

- 角色清單頁面（建立／編輯／停用）
- 角色權限設定介面
- 使用者清單與角色指派介面
- 支援一次指派多個角色
- 權限異動即時生效
- 操作權限受 RBAC 控管

## 相關 Issue 依賴關係

- 本 issue #8 [2.2.2] 是**純前端** issue
- 那部分後端 API（`/api/roles`、`/api/users/{id}/roles`）实作在 issue #6 [2.2.1]
- 本分支以**契約對齊的方式**實作前端，提供 mock interceptor 讓前端可獨立開發
- #6 落地後，關掉 `VITE_USE_MOCK_RBAC` flag 即可無縫換成真實 API

## 架構

```
web/src
  api/
    roles.ts                 → 型別 + axios fetch 函式（對齊 #6 契約）
    mockRoles.ts             → dev 假資料 + interceptor（VITE_USE_MOCK_RBAC=true 引入）
    permissionGate.ts        → useHasPermission / useIsAdmin / usePermissionContext hooks
  lib/
    permissions.ts           → 權限目錄常數（名稱、分類）
  pages/admin/
    AdminLayout.tsx          → 左側 sidebar + 可 admin gate
    RolesPage.tsx            → 角色清單頁
    RoleEditDrawer.tsx       → 角色建立／編輯 drawer（進階權限樹狀勾選）
    UsersPage.tsx            → 使用者清單頁
    UserRolesAssignDrawer.tsx → 複合角色指派 drawer（包含 effectiveFrom/effectiveTo）
```

## 權限儲存與檢查流程

```mermaid
flowchart LR
    A[Azure AD App Roles] -->|ID Token claims.roles| B[AuthContext.user.roles]
    B --> C[usePermissionContext]
    D[GET /api/roles<br/>(含 active+permissions)] --> C
    C --> E{useHasPermission(key)}
    C --> F{useIsAdmin}
    E -->|true| G[顯示動作按鈕]
    E -->|false| H[隱藏或 disabled]
    F -->|true| I[/admin/* 路由可進入]
    F -->|false| J[Result 403]
```

**進階防禦**：前端的權限檢查為**樂觀型**（控制顯示與高階体驗），最終是否能執行仍由後端決定。前端沒有權限時仍然可能看到按鈕被連鎖顯示（測試時）但實際打後端會被 403。

## 權限目錄與 Permissions JSON

詳見 `web/src/lib/permissions.ts`。Permission key 命名規約：

- 格式：`<resource>.<action>`，全小寫底線隔
- 分類：cases / templates / fields / customers / roles / users / system

後端 #6 [2.2.1] `Role.Permissions` 儲存以 JSON 字串陣列為標準表示：

```json
["cases.create", "cases.view", "templates.publish"]
```

新增權限只需在 `permissions.ts` PERMISSIONS 陣列加一項，後端不需任何變動。

## API 契約（對齊 #6 [2.2.1] issue body）

| 方法 · 路徑 | 說明 |
| --- | --- |
| `GET /api/roles` | 列出所有角色（含停用者） |
| `POST /api/roles` | 建立角色 |
| `PUT /api/roles/{id}` | 更新角色，isActive=false 即為「停用」 |
| `GET /api/users` | 列出使用者清單（含生效中角色指派） |
| `PUT /api/users/{id}/roles` | 以取代式設定使用者角色指派（進來的列表即為完整狀態） |

請 #6 落地時保證以上契約。請參考 `web/src/api/roles.ts` 的 TypeScript 型別。

## Mock 模式使用方式

```bash
# .env.development
VITE_USE_MOCK_RBAC=true
```

啟用後：

- `apiClient` 的 `/roles*` 與 `/users*` 請求會被拦截、不打後端
- Mock 資料位於 `web/src/api/mockRoles.ts`（含 4 個角色、幾個使用者，Alice 為 admin）
- console 會顯示 `[IsoDocs] Mock RBAC interceptor enabled` 提醒模式已啟用
- Mock 狀態仅存於記憶體，重新整理頁面即重置

## 上線切換

#6 [2.2.1] 同步完成到 main 後：

1. 在本檔案預期位置減一個 commit，把 `.env.development` 設為 `VITE_USE_MOCK_RBAC=false`
2. 也可以讓個別開發者由 user 設定自己要 mock 或接真實 API
3. Production build (`npm run build`) 預設讀取 `.env.production`，這裡**不要**設 `VITE_USE_MOCK_RBAC`，避免上 production 不慎走 mock

## Azure AD App Roles 對接建議

為讓「是否 admin」判斷能在未拉 `/api/roles` 前即可作業：

1. 在 Azure AD App registration 「App roles」加一個 `IsoDocs.Admin`
2. 指派給該為系統管理者的使用者
3. ID Token 的 `roles` claim 就會包含 `"IsoDocs.Admin"`。本分支 `permissionGate.ts` 的
   `ADMIN_ROLE_NAMES` set 已包含這個名稱
4. 未設定、但 mock 模式下使用者被 assigned `系統管理者` (中文名稱) 也能生效

## 未盡事項

- [ ] **需 #6 [2.2.1] 落地**：本前端只能跑 mock，真實后端未接上之前無法端到端驗證
- [ ] **考慮改為 `/api/me` 一次回 effective permissions**：現在 `usePermissionContext`
  是拉 `/api/roles` 后在前端 join，后端可提供現成 effective permissions、避免不必要的拉取
- [ ] **CI 驗識**：`web.yml` workflow 會 typecheck + build。sandbox 加 sample/lint 可能需要調整
- [ ] **端到端測試**：人類接手者本機跑 `npm run dev` 驗證一輪 UX flow。如果有
  規則作業上需要調整的地方請補上註記

## 與其他 issue 的交互

- **#2 [2.1.1] / #34 [2.1.2]**：AuthContext + MSAL 已提供 user.roles，本 issue 直接讀取
- **#6 [2.2.1]**：後端 RBAC 落地后、本 issue 才能端到端跑起來
- **#3 [2.3.1]**：使用者清單未來要加上「邀請成員」按鈕，本 issue 的 UsersPage 預留了按鈕位置
- **#8 [2.2.2]**：本 issue
