# Auth / Identity 內部設計 (issue #2 [2.1.1])

本文件說明 IsoDocs 後端與 Azure AD / Entra ID 的整合方式。

## 1. 高階架構

```
[SPA (issue #34 [2.1.2])] --MSAL.js--> [Azure AD]
         |  (拿回 access_token)
         v
[GET /api/me]  Authorization: Bearer <access_token>
         |
         v
[IsoDocs.Api]
  ├─ Microsoft.Identity.Web 驗證 token 簽名/過期/audience/issuer
  ├─ ClaimsPrincipalAuthExtensions 上拿 oid/tid/email/name/roles/scp
  └─ IUserSyncService.UpsertFromAzureAdAsync 同步 dbo.Users
```

本系統針對 **API mode** (Bearer Token) 設計，沒有 server-side cookie session。
實際的 OIDC `authorization_code + PKCE` 流程由前端 MSAL.js 走，後端只負責
驗證請求上的 access_token。

## 2. Azure AD 應用程式註冊 (手動)

需在 Azure portal 走一次，以下是推薦設定：

1. **Application registration** → New registration。名稱：`IsoDocs API`。
   - Single tenant（內部使用）。
   - Redirect URI 留空（API mode 不需要）。
2. **Expose an API** → 設定 `Application ID URI`，預設 `api://<client-id>` 即可；
   加一個 scope：`access_as_user`。
3. **Token configuration** → Add optional claims (Access Token)：`email`、`upn`、
   `preferred_username`、`department`、`jobTitle`、`groups` (選項)。
4. **App roles** (選項) → 依需求設 `Admin` / `Auditor` / `Editor` 等，對應
   本系統 issue #6 [2.2.1] 的 Roles。
5. **API permissions** → Microsoft Graph: `User.Read.All` (delegated)；需管理者同意。
   (給 issue #23 的離職同步使用。)
6. 另註一個 SPA 的 application registration `IsoDocs Web`。Redirect URI 填
   `http://localhost:5173/auth/callback` (dev) 與 production URL。在 SPA 此 app 裡
   加 API permissions → IsoDocs API → `access_as_user`。

## 3. 使用端設定

以 user-secrets 或環境變數提供（**請勿 commit ClientId/TenantId 到儲存庫**）：

```bash
dotnet user-secrets --project src/IsoDocs.Api set "AzureAd:TenantId"  "<tenant-id>"
dotnet user-secrets --project src/IsoDocs.Api set "AzureAd:ClientId"  "<api-client-id>"
dotnet user-secrets --project src/IsoDocs.Api set "AzureAd:Audience"  "api://<api-client-id>"
```

亦可用環境變數：`AzureAd__TenantId` / `AzureAd__ClientId` / `AzureAd__Audience`。

## 4. Token claim 對應表

| AzureAdUserPrincipal | Claim (預設) | Schema URI fallback |
| -------------------- | ------------- | ------------------- |
| `AzureAdObjectId`    | `oid`         | `http://schemas.microsoft.com/identity/claims/objectidentifier` |
| `TenantId`           | `tid`         | `http://schemas.microsoft.com/identity/claims/tenantid` |
| `Email`              | `email` → `preferred_username` → `ClaimTypes.Email` → `upn` → `ClaimTypes.Upn` | — |
| `DisplayName`        | `name` → `ClaimTypes.Name` | — |
| `Department`         | `department`  | (需 token configuration optional claim) |
| `JobTitle`           | `jobTitle`    | (需 token configuration optional claim) |
| `Roles`              | `roles`＋`ClaimTypes.Role`（不重複合併） | — |
| `Scopes`             | `scp` 以空格分隔 | `http://schemas.microsoft.com/identity/claims/scope` |

## 5. User upsert 規則

`IUserSyncService.UpsertFromAzureAdAsync(principal)`：

1. 以 `principal.AzureAdObjectId` 在 `dbo.Users` 查詢。
2. 存在：呼叫 `User.UpdateProfile(email, displayName, department, jobTitle)`；
   若 `IsActive == false` 則呼叫 `User.Activate()`（Azure AD 為 single source of truth，重出現 = 重新啟用）。
3. 不存在：`new User(Guid.NewGuid(), oid, email, displayName)` → `UpdateProfile(...)` 補全 dept/title → `Users.Add()`。
4. `SaveChangesAsync()` 一次。`AuditableEntityInterceptor` 自動設 `UpdatedAt`。

更詳盡的邊界條件見 `tests/IsoDocs.Application.UnitTests/Auth/UserSyncServiceTests.cs`。

## 6. 離職人員同步 (跨 issue 依賴路線圖)

```
[Hangfire 排程 (issue #22 [6.3])]
   └─每日跑一次 IUserDeactivationSyncJob (本輪尚未實作)
         └─呼叫 GraphServiceClient (issue #23 [6.1]) 拿 Azure AD users
               └─以「本系統有 / Azure AD 已關閉」的使用者集合
                     └─逐個呼 IUserSyncService.DeactivateByAzureObjectIdAsync
```

本 issue #2 [2.1.1] 的驗收條件「離職人員 Azure AD 失效自動禁用」在本 issue
中完成「**提供給下游調用的接口 (`DeactivateByAzureObjectIdAsync`)**」；
實際的排程作業本體交給 issue #22、#23 一起完工（需 Hangfire 與 Microsoft Graph 均到位）。

## 7. 端點一覽

| Method | Path | Auth | 說明 |
| ------ | ---- | ---- | ---- |
| GET    | `/api/auth/login`    | Anonymous | 回傳 SPA 需要的 OIDC 設定 (authority/clientId/scopes)。 |
| POST   | `/api/auth/logout`   | Authorize | 提示 SPA 自行呼叫 MSAL.logoutRedirect()。 |
| GET    | `/api/me`            | Authorize | 以 token 同步 User 並回傳 CurrentUserDto。 |
| GET    | `/api/health`        | Anonymous | 存活檢查。 |

## 8. 驗證管道 / 本輪未完成

### CI workflow (本輪新增)

`.github/workflows/dotnet.yml` 已加入。觸發條件：
- `push` 到 `main` 或任何 `feature/**` 分支
- `pull_request` 進 `main`

工作步驟：`dotnet restore → build (Release) → test (Application.UnitTests + Api.IntegrationTests)`，
測試結果以 `.trx` 上傳為 artifact 保留 7 天。

意義：sandbox 因 proxy 擋住 .NET SDK / NuGet 無法本機驗證，
但 GitHub-hosted runner 不受此限。本分支的所有 push 與後續 PR 會自動跑全套測試，
把實際的 build/test 狀態暴露出來，**不再依賴人類接手者本機跑一遍**。

### 仍待人類接手 / 後續 issue 處理

- [ ] **Azure AD 應用程式註冊** (手動動作，本 repo 無法自動化)。
  詳見 §2。今後可以考慮以 IaC (Bicep / Terraform) 記述這個註冊步驟。
- [ ] **整合測試初次 CI 跑綠**：本輪推上去後第一次 CI run 若失敗，
  常見可能原因：
  1. `record CurrentUserDto` 反序列化 — `System.Net.Http.Json` 預設 `JsonSerializerDefaults.Web`
     已開 `PropertyNameCaseInsensitive`，理論上 OK；若實測失敗可在
     `MeEndpointTests` 改用顯式 `JsonSerializerOptions { PropertyNameCaseInsensitive = true }`。
  2. `CustomWebApplicationFactory` 的 `services.AddAuthentication(defaultScheme: "Test")` 後續呼叫
     是否成功覆蓋既有 `JwtBearerDefaults.AuthenticationScheme`。若失敗，
     需要 `services.PostConfigure<AuthenticationOptions>(opt => opt.DefaultScheme = "Test")`。
- [ ] **JWT 簽章端到端測試**：可考慮再開 `tests/IsoDocs.Api.JwtTests/`，用
  `Microsoft.IdentityModel.Tokens` 自簽 JWT 驗證真實 JwtBearer middleware 的
  「過期 / audience 不符 / issuer 不符」三種失敗路徑。本 issue 不強求，留給後續優化。
- [ ] **App roles 與 IsoDocs Roles 表的映射**。這是 issue #6 [2.2.1] 的議題，
  `MeController` 現在只呈現 Token 中的 raw roles，選擇在 RBAC 落地時再接上。
