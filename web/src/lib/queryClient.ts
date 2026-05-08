import { QueryClient } from '@tanstack/react-query';

/**
 * 全域 QueryClient 設定。
 * - staleTime 預設 30 秒，降低不必要的 refetch
 * - retry 預設 1 次，避免後端短暫抖動造成 UX 不佳
 * - 401 錯誤交由 axios 攔截器處理（重定向至 /login）
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      gcTime: 5 * 60_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: 0,
    },
  },
});
