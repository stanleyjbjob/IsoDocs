# IsoDocs.Api.IntegrationTests

針對 `IsoDocs.Api` 的端到端 (in-process) 整合測試專案，對應 issue #2 [2.1.1] 的驗收條件「撰寫整合測試」。

## 設計

以 ASP.NET Core 的 `WebApplicationFactory<Program>` 在記憶體中啟動真實的 API pipeline，但替換兩個外部依賴：

1. **`IUserSyncService`** → `FakeUserSyncService`：不接 SQL Server，避免測試需要實體資料庫；同時可主動 `ForcePermanentDeactivation(oid)` 模擬「強制停用」場景。
2. **Authentication**：以 `TestAuthHandler` 取代 `Microsoft.Identity.Web` 的 JWT Bearer。`TestAuthHandler` 直接從 `Authorization: Test type1=value1,type2=value2` header 還原 `ClaimsPrincipal`，避開「需要真實 Azure AD JWKs 才能驗 token 簽章」的限制。

注入給 `Program.cs` 的假設定 (`AzureAd:ClientId` / `TenantId` 非空) 會讓 `isAzureAdConfigured = true`，啟用 `[Authorize]` pipeline 與 `FallbackPolicy.RequireAuthenticatedUser`。

## 涵蓋情境

| 端點 | 案例 | 預期 |
| ---- | ---- | ---- |
| `GET  /api/health` | 不帶 Authorization | 200 (`[AllowAnonymous]`) |
| `GET  /api/auth/login` | 不帶 Authorization | 200 + OIDC 設定 JSON |
| `POST /api/auth/logout` | 不帶 Authorization | 401 |
| `POST /api/auth/logout` | Test scheme + claims | 200 |
| `GET  /api/me` | 不帶 Authorization | 401 |
| `GET  /api/me` | Test scheme + 完整 claims | 200 + `CurrentUserDto`，`UserSync` upsert 一次 |
| `GET  /api/me` | 強制停用使用者 | 403 + `AUTH/USER_DEACTIVATED` |
| `GET  /api/me` | Test scheme 但缺 oid | 401 + `AUTH/MISSING_OID` |

## 執行方式

```bash
# 還原 + 編譯（沙盒環境若 NuGet 被 proxy 擋住，需先放開或設 NUGET_PACKAGES 鏡像）
dotnet restore
dotnet build

# 跑整合測試
dotnet test tests/IsoDocs.Api.IntegrationTests/IsoDocs.Api.IntegrationTests.csproj
```

## 已知限制 / 後續可擴充

- 目前未驗證真實的 JWT 簽章流程；對「token 過期」「audience 不符」「issuer 不符」等情境，需要另外接 `Microsoft.IdentityModel.Tokens` 自簽 JWT 的測試專案 (考慮再開 `tests/IsoDocs.Api.JwtTests/`)。
- `FakeUserSyncService` 為 in-memory，跨測試類別 (各自獨立 fixture) 不會洩漏狀態，但同 fixture 內測試會共用 store；若需測試隔離請拆 fixture。
- 目前未涵蓋 issue #6 [2.2.1] 的 RBAC policy 檢查（[Authorize(Roles=...)] 路徑），等 RBAC 落地後補。
- 本專案骨架在 sandbox 環境 (proxy 擋 NuGet) 中無法 `dotnet build` 驗證；首次跑於本機環境的人若遇編譯錯誤，多半在：(a) PackageReference 版本對齊 IsoDocs.Application.UnitTests.csproj、(b) namespace import 漏掉 `using Xunit;` 或 `using FluentAssertions;`。

## 參考

- `docs/auth.md` — 完整 Auth 設計
- `tests/IsoDocs.Application.UnitTests/Auth/UserSyncServiceTests.cs` — Domain 層單元測試
- ASP.NET Core 整合測試文件：<https://learn.microsoft.com/aspnet/core/test/integration-tests>
