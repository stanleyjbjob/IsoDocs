import { createContext, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';

/**
 * 使用者基本資訊（由後端 /api/me 回傳，目前先用最小子集）。
 */
export interface AuthUser {
  id: string;
  email: string;
  displayName: string;
  isSystemAdmin?: boolean;
  roles: string[];
}

export interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  /**
   * 取得目前的存取 Token。供 API client 攔截器同步呼叫。
   * 目前回傳 localStorage 暂存値；issue #34 [2.1.2] 會改由 MSAL acquireTokenSilent 取得。
   */
  getToken: () => string | null;
  login: () => Promise<void>;
  logout: () => Promise<void>;
}

const TOKEN_STORAGE_KEY = 'isodocs.auth.token';
const USER_STORAGE_KEY = 'isodocs.auth.user';

const AuthContext = createContext<AuthContextValue | null>(null);

/**
 * AuthProvider 是 MSAL 整合前的暫時實作：
 * - getToken 直接讀 localStorage（後續會改為 MSAL acquireTokenSilent）
 * - login/logout 為 stub，待 issue #34 接入真實流程
 * 設計上保留完整介面，讓 API client / 受保護路由不需在後續整合時改動。
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    try {
      const raw = localStorage.getItem(USER_STORAGE_KEY);
      if (raw) {
        setUser(JSON.parse(raw) as AuthUser);
      }
    } catch {
      // 忽略：壞資料時視為未登入
    } finally {
      setIsLoading(false);
    }
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isLoading,
      getToken: () => localStorage.getItem(TOKEN_STORAGE_KEY),
      login: async () => {
        // TODO(issue #34 [2.1.2])：以 MSAL.loginPopup / loginRedirect 取代
        throw new Error('登入流程尚未整合，將於 issue #34 [2.1.2] 完成');
      },
      logout: async () => {
        localStorage.removeItem(TOKEN_STORAGE_KEY);
        localStorage.removeItem(USER_STORAGE_KEY);
        setUser(null);
      },
    }),
    [user, isLoading],
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

export const __INTERNAL_TOKEN_STORAGE_KEY = TOKEN_STORAGE_KEY;
