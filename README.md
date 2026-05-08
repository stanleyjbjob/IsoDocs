# IsoDocs

ISO 文件管理系統 — 工作需求單與簽核流程平台。

## 技術棧

- 後端：ASP.NET Core 8 + MediatR (CQRS) + FluentValidation + EF Core
- 前端：React 18 + Vite + TypeScript（後續加入）
- 資料庫：Azure SQL Database / SQL Server 2022
- 身份認證：Azure AD / Entra ID（Microsoft.Identity.Web）
- 排程：Hangfire
- 通知：Microsoft Graph API（Teams / Email）
- 儲存：Azure Blob Storage

## 解決方案結構

```
IsoDocs/
├── src/
│   ├── IsoDocs.Api/             # Web API 入口（Controllers, Middleware, DI）
│   ├── IsoDocs.Application/     # CQRS Command/Query, Behaviors, Validators
│   ├── IsoDocs.Domain/          # 領域模型、Entity、Value Object
│   └── IsoDocs.Infrastructure/  # EF Core, 外部服務整合
├── tests/
│   └── IsoDocs.Application.UnitTests/
├── docs/
│   └── architecture.md          # 架構說明文件
└── IsoDocs.sln
```

## 啟動

```bash
# 還原套件
dotnet restore

# 建置
dotnet build

# 執行 API（預設 http://localhost:5000）
dotnet run --project src/IsoDocs.Api
```

## 開發進度

請參考 [GitHub Issues](https://github.com/stanleyjbjob/IsoDocs/issues)。每張 issue 對應一個獨立的開發任務，由排程式 Claude 接力推進。

## 架構說明

詳見 [docs/architecture.md](docs/architecture.md)。
