import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { useMsal, useIsAuthenticated } from '@azure/msal-react';
import {
  InteractionRequiredAuthError,
  InteractionStatus,
  type AccountInfo,
} from '@azure/msal-browser';
import { apiTokenRequestScopes, loginRequestScopes } from '../lib/msalConfig';

/**
 * 使用者基本資訊（由 ID Token claims 解析；後續可改由後端 /api/me 同步覆寫）。
 */
export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
}

export interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  /**
   * 同步取得最近一次成功取得的 access token。供 axios request interceptor 使用。
   * 若尚未登入或還沒拿到 token，回傳 null。真正向 MSAL 取 token 是 acquireToken() 的工作。
   */
  getToken: () => string | null;
  /**
   * 異步取得 access token：先 acquireTokenSilent；catch InteractionRequiredAuthError
   * 再走 acquireTokenRedirect 互動式 fallback。
   */
  acquireToken: () => Promise<string | null>;
  login: () => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/**
 * 將 MSAL `AccountInfo` 轉成本系統的 `AuthUser`。
 * 守護式處理 idTokenClaims（其型別在 MSAL v3 是寬鬆的 unknown shape）。
 */
function accountToUser(account: AccountInfo | null | undefined): AuthUser | null {
  if (!account) return null;
  const claims = (account.idTokenClaims ?? {}) as Record<string, unknown>;
  const roles = Array.isArray(claims.roles) ? (claims.roles as string[]) : [];
  const oidClaim = typeof claims.oid === 'string' ? claims.oid : '';
  const preferredUsername =
    typeof claims.preferred_username === 'string' ? claims.preferred_username : '';
  const emailClaim = typeof claims.email === 'string' ? claims.email : '';
  const nameClaim = typeof claims.name === 'string' ? claims.name : '';
  return {
    id: oidClaim || account.localAccountId,
    email: preferredUsername || emailClaim || account.username,
    displayName: nameClaim || account.name || account.username,
    roles,
  };
}

/**
 * AuthProvider — issue #34 [2.1.2] MSAL 整合版。
 *
 * 必須包在 `<MsalProvider>` 之內（main.tsx 已處理）。
 *
 * 行為：
 * - 透過 useMsal / useIsAuthenticated 反映 MSAL 內部狀態
 * - 第一次偵測到登入後，主動 acquireTokenSilent 取一次 access token 暫存於 React state
 *   讓 API client 的 request interceptor 可以同步取用（雖然我們也提供 acquireToken async 版）
 * - login()  → loginRedirect（OIDC 標準）
 * - logout() → logoutRedirect，登出後導向 /login
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const { instance, accounts, inProgress } = useMsal();
  const isAuthenticated = useIsAuthenticated();
  const [token, setToken] = useState<string | null>(null);
  const [tokenAttempted, setTokenAttempted] = useState(false);

  const activeAccount = instance.getActiveAccount() ?? accounts[0] ?? null;

  // 確保 active account 與 accounts[0] 同步（首次登入完 redirect 回來時 useMsal 可能還沒設好）
  useEffect(() => {
    if (activeAccount && !instance.getActiveAccount()) {
      instance.setActiveAccount(activeAccount);
    }
  }, [activeAccount, instance]);

  const acquireToken = useCallback(async (): Promise<string | null> => {
    const account = instance.getActiveAccount() ?? accounts[0];
    if (!account) {
      return null;
    }
    try {
      const result = await instance.acquireTokenSilent({
        account,
        scopes: apiTokenRequestScopes,
      });
      setToken(result.accessToken);
      return result.accessToken;
    } catch (err) {
      if (err instanceof InteractionRequiredAuthError) {
        // 互動式 fallback：用 redirect 流程重新取 token
        await instance.acquireTokenRedirect({
          account,
          scopes: apiTokenRequestScopes,
        });
        return null;
      }
      // eslint-disable-next-line no-console
      console.error('acquireTokenSilent 失敗：', err);
      return null;
    }
  }, [instance, accounts]);

  // 第一次有 active account 時主動取一次 token，讓 API client 能立即用
  useEffect(() => {
    if (isAuthenticated && !tokenAttempted) {
      setTokenAttempted(true);
      void acquireToken();
    }
    if (!isAuthenticated && tokenAttempted) {
      setToken(null);
      setTokenAttempted(false);
    }
  }, [isAuthenticated, tokenAttempted, acquireToken]);

  const login = useCallback(async () => {
    await instance.loginRedirect({ scopes: loginRequestScopes });
  }, [instance]);

  const logout = useCallback(async () => {
    setToken(null);
    setTokenAttempted(false);
    const account = instance.getActiveAccount() ?? accounts[0] ?? undefined;
    await instance.logoutRedirect({
      account,
      postLogoutRedirectUri:
        typeof window !== 'undefined' ? window.location.origin + '/login' : undefined,
    });
  }, [instance, accounts]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user: accountToUser(activeAccount),
      isAuthenticated,
      isLoading:
        inProgress === InteractionStatus.Startup ||
        inProgress === InteractionStatus.HandleRedirect,
      getToken: () => token,
      acquireToken,
      login,
      logout,
    }),
    [activeAccount, isAuthenticated, inProgress, token, acquireToken, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth 必須在 <AuthProvider> 內使用');
  }
  return ctx;
}

/**
 * 為了相容 issue #4 [1.2] 留下的 API client 介面，導出一個 token storage key 常數。
 * MSAL 整合後 token 主要透過 `acquireTokenSilent` 取得而非 localStorage，但保留這個常數
 * 讓後續模組（例如手動清除 stale state 的工具）可以參照。
 */
export const __INTERNAL_TOKEN_STORAGE_KEY = 'isodocs.auth.token';
