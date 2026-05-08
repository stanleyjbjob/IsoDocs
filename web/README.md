# IsoDocs Web

IsoDocs 系統前端，使用 React 18 + Vite + TypeScript。

## 技術堆疊

- React 18
- Vite 5
- TypeScript 5（strict 模式）
- Ant Design 5（UI 元件庫）
- TanStack Query v5（伺服器狀態管理）
- React Router v6（路由）
- Axios（HTTP client，含 Token 攔截器）
- ESLint + Prettier

## 專案結構

```
web/
├── src/
│   ├── api/                # API client 封裝
│   │   ├── client.ts       # axios instance + 攔截器
│   │   └── index.ts        # API barrel export
│   ├── contexts/           # React Context
│   │   └── AuthContext.tsx # 認證狀態（預留 MSAL）
│   ├── lib/
│   │   └── queryClient.ts  # TanStack QueryClient 設定
│   ├── pages/              # 路由頁面
│   │   ├── HomePage.tsx
│   │   ├── LoginPage.tsx
│   │   └── NotFoundPage.tsx
│   ├── types/
│   │   └── env.d.ts        # Vite 環境變數型別
│   ├── App.tsx             # 根元件
│   ├── main.tsx            # 進入點
│   └── router.tsx          # 路由設定
├── .env.example            # 環境變數範本
├── index.html
├── package.json
├── tsconfig.json
└── vite.config.ts
```

## 本地開發

```bash
cd web
npm install
cp .env.example .env.development.local  # 視需要調整
npm run dev
```

預設啟動於 http://localhost:5173，並將 `/api/*` 透過 Vite proxy 轉發至 `VITE_API_BASE_URL`。

## 常用指令

| 指令 | 說明 |
| --- | --- |
| `npm run dev` | 啟動開發伺服器 |
| `npm run build` | 編譯型別並建置正式版 |
| `npm run preview` | 預覽建置後的 dist |
| `npm run lint` | ESLint 檢查 |
| `npm run lint:fix` | ESLint 自動修正 |
| `npm run format` | Prettier 格式化 |
| `npm run typecheck` | TypeScript 型別檢查 |

## 認證整合

本骨架已預留 `AuthContext`，目前提供 mock 介面（getToken/login/logout）。
後續 issue #34 [2.1.2] 會接入 `@azure/msal-react` 完成 Azure AD 登入流程，
API client 已先行支援 Bearer Token 自動附帶與 401 重定向。
