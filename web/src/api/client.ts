import axios from 'axios';
import type { AxiosInstance, InternalAxiosRequestConfig, AxiosError } from 'axios';
import { __INTERNAL_TOKEN_STORAGE_KEY as TOKEN_KEY } from '../contexts/AuthContext';

const API_PREFIX = import.meta.env.VITE_API_PREFIX ?? '/api';

/**
 * API client 全域設定：
 * - baseURL 預設為 /api（dev 透過 Vite proxy 轉發至後端）
 * - 自動附帶 Bearer Token
 * - 401 自動清除登入狀態並導向 /login
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

  instance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem(TOKEN_KEY);
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
        // Token 過期或無效：清除狀態並導向登入頁
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem('isodocs.auth.user');
        if (window.location.pathname !== '/login') {
          window.location.assign('/login');
        }
      }
      return Promise.reject(error);
    },
  );

  return instance;
}

export const apiClient = createApiClient();
