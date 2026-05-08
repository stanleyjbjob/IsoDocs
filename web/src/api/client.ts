import axios from 'axios';
import type { AxiosInstance, InternalAxiosRequestConfig, AxiosError } from 'axios';
import { InteractionRequiredAuthError } from '@azure/msal-browser';
import { msalInstance, apiTokenRequestScopes } from '../lib/msalConfig';

const API_PREFIX = import.meta.env.VITE_API_PREFIX ?? '/api';

/**
 * 取得當前要附帶到 API 請求上的 access token。
 * - 沒有 active account → null（caller 不附 Bearer，request 會被後端拒 401）
 * - acquireTokenSilent 成功 → 回傳 access token
 * - 需要互動 → 不在 interceptor 內主動 redirect（避免進入無窮重導），
 *   交給 caller / 401 handler 處理
 */
async function getAccessToken(): Promise<string | null> {
  const account = msalInstance.getActiveAccount() ?? msalInstance.getAllAccounts()[0];
  if (!account) {
    return null;
  }
  try {
    const result = await msalInstance.acquireTokenSilent({
      account,
      scopes: apiTokenRequestScopes,
    });
    return result.accessToken;
  } catch (err) {
    if (err instanceof InteractionRequiredAuthError) {
      return null;
    }
    // eslint-disable-next-line no-console
    console.warn('acquireTokenSilent 失敗，request 將不附 Bearer：', err);
    return null;
  }
}

/**
 * API client 全域設定：
 * - baseURL 預設為 /api（dev 透過 Vite proxy 轉發至後端）
 * - 自動附帶 Bearer Token（透過 MSAL acquireTokenSilent 取得）
 * - 401 自動導向 /login（不主動呼叫 logoutRedirect 以免無窮重導；MSAL session 由 logout() 顯式清除）
 * - timeout 30 秒
 */
export function createApiClient(): AxiosInstance {
  const instance = axios.create({
    baseURL: API_PREFIX,
    timeout: 30_000,
    headers: {
      'Content-Type': 'application/json',
    },
  });

  instance.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
    const token = await getAccessToken();
    if (token) {
      config.headers = config.headers ?? {};
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });

  instance.interceptors.response.use(
    (response) => response,
    (error: AxiosError) => {
      if (error.response?.status === 401) {
        if (typeof window !== 'undefined' && window.location.pathname !== '/login') {
          window.location.assign('/login');
        }
      }
      return Promise.reject(error);
    },
  );

  return instance;
}

export const apiClient = createApiClient();
