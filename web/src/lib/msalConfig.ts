import { LogLevel, PublicClientApplication, type Configuration } from '@azure/msal-browser';

/**
 * MSAL 設定（issue #34 [2.1.2]）。
 *
 * 設定值由 Vite 編譯時注入的環境變數提供：
 *   - VITE_AZURE_CLIENT_ID    Azure portal「應用程式註冊」上的 Application (client) ID
 *   - VITE_AZURE_TENANT_ID    所屬租用戶 Directory (tenant) ID
 *   - VITE_AZURE_REDIRECT_URI 重新導向 URI（預設為前端自身網域）
 *   - VITE_AZURE_API_SCOPE    呼叫 IsoDocs 後端時要求的 scope
 *                             （格式：api://<API_CLIENT_ID>/access_as_user，由 issue #2 [2.1.1] 在
 *                             Azure portal「公開 API」設定）
 *
 * 詳細註冊步驟見 docs/auth.md §2（issue #2 [2.1.1] 維護）。
 */

const tenantId = import.meta.env.VITE_AZURE_TENANT_ID ?? '';
const clientId = import.meta.env.VITE_AZURE_CLIENT_ID ?? '';
const redirectUri =
  import.meta.env.VITE_AZURE_REDIRECT_URI ??
  (typeof window !== 'undefined' ? window.location.origin : 'http://localhost:5173');
const apiScope = import.meta.env.VITE_AZURE_API_SCOPE ?? '';

/**
 * 是否已正確設定 Azure AD（讓 dev 預覽可以在沒有設好 env 的情況下啟動）。
 */
export const isMsalConfigured = Boolean(clientId) && Boolean(tenantId);

const authority = tenantId
  ? `https://login.microsoftonline.com/${tenantId}`
  : 'https://login.microsoftonline.com/common';

export const msalConfig: Configuration = {
  auth: {
    clientId: clientId || 'PLACEHOLDER_CLIENT_ID',
    authority,
    redirectUri,
    navigateToLoginRequestUrl: true,
  },
  cache: {
    // sessionStorage：分頁關閉即清空，較適合企業內部系統
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      logLevel: import.meta.env.DEV ? LogLevel.Warning : LogLevel.Error,
      piiLoggingEnabled: false,
      loggerCallback: (level, message) => {
        if (level <= LogLevel.Error) {
          // eslint-disable-next-line no-console
          console.error('[MSAL]', message);
        } else if (level === LogLevel.Warning && import.meta.env.DEV) {
          // eslint-disable-next-line no-console
          console.warn('[MSAL]', message);
        }
      },
    },
  },
};

/**
 * 登入時要求的 scopes（OIDC 標準）。
 * 真正呼叫 IsoDocs API 的 access token 透過 acquireTokenSilent + apiTokenRequestScopes 取得。
 */
export const loginRequestScopes = ['openid', 'profile', 'email'];

/**
 * 呼叫後端 API 時要求的 scopes。
 * 預設為 issue #2 [2.1.1] 設定的 api://<API_CLIENT_ID>/access_as_user；
 * 透過 VITE_AZURE_API_SCOPE 注入。若未設則 fallback 到 User.Read（dev 試 SSO 用）。
 */
export const apiTokenRequestScopes: string[] = apiScope ? [apiScope] : ['User.Read'];

/**
 * 全域單例 PublicClientApplication。
 *
 * 注意：MSAL v3+ 強制要求先 `await msalInstance.initialize()` 再使用任何 API，
 * main.tsx 的 bootstrap 流程會處理初始化順序。
 */
export const msalInstance = new PublicClientApplication(msalConfig);
