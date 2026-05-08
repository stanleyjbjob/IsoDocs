import React from 'react';
import ReactDOM from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';
import { ConfigProvider, App as AntdApp } from 'antd';
import zhTW from 'antd/locale/zh_TW';
import { MsalProvider } from '@azure/msal-react';
import { EventType } from '@azure/msal-browser';
import 'dayjs/locale/zh-tw';
import dayjs from 'dayjs';

import App from './App';
import { queryClient } from './lib/queryClient';
import { AuthProvider } from './contexts/AuthContext';
import { msalInstance } from './lib/msalConfig';
import { apiClient } from './api/client';
import { installMockRbacInterceptor } from './api/mockRoles';
import { installMockFieldsInterceptor } from './api/mockFieldDefinitions';
import { installMockTemplatesInterceptor } from './api/mockWorkflowTemplates';
import { installMockCasesInterceptor } from './api/mockCases';

dayjs.locale('zh-tw');

const rootEl = document.getElementById('root');
if (!rootEl) {
  throw new Error('Root element #root not found');
}

async function bootstrap(target: HTMLElement) {
  // 條件啟用 mock RBAC interceptor（issue #8 [2.2.2]），讓前端在 #6 後端未落地前可獨立開發
  if (import.meta.env.VITE_USE_MOCK_RBAC === 'true') {
    installMockRbacInterceptor(apiClient);
    // eslint-disable-next-line no-console
    console.info('[IsoDocs] Mock RBAC interceptor enabled (VITE_USE_MOCK_RBAC=true)');
  }

  // 條件啟用 mock field-definitions interceptor（issue #11 [3.1.2]），讓前端在 #7 未落地前可獨立開發
  if (import.meta.env.VITE_USE_MOCK_FIELDS === 'true') {
    installMockFieldsInterceptor(apiClient);
    // eslint-disable-next-line no-console
    console.info('[IsoDocs] Mock field-definitions interceptor enabled (VITE_USE_MOCK_FIELDS=true)');
  }

  // 條件啟用 mock workflow-templates interceptor（issue #13 [3.2.2]），讓前端在 #12 未落地前可獨立開發
  if (import.meta.env.VITE_USE_MOCK_TEMPLATES === 'true') {
    installMockTemplatesInterceptor(apiClient);
    // eslint-disable-next-line no-console
    console.info('[IsoDocs] Mock workflow-templates interceptor enabled (VITE_USE_MOCK_TEMPLATES=true)');
  }

  // 條件啟用 mock cases interceptor（issue #21 [5.5]），讓前端在 #9/#15/#17/#18/#20 未落地前可獨立開發
  if (import.meta.env.VITE_USE_MOCK_CASES === 'true') {
    installMockCasesInterceptor(apiClient);
  }

  // MSAL v3+ 強制要求先 initialize() 再使用任何 API
  await msalInstance.initialize();

  // 處理 redirect 回來的 response（會 set active account）
  const response = await msalInstance.handleRedirectPromise();
  if (response?.account) {
    msalInstance.setActiveAccount(response.account);
  } else if (!msalInstance.getActiveAccount()) {
    const accounts = msalInstance.getAllAccounts();
    if (accounts.length > 0) {
      msalInstance.setActiveAccount(accounts[0]);
    }
  }

  // 訂閱 LOGIN_SUCCESS / ACQUIRE_TOKEN_SUCCESS：保持 active account 同步
  msalInstance.addEventCallback((event) => {
    if (
      (event.eventType === EventType.LOGIN_SUCCESS ||
        event.eventType === EventType.ACQUIRE_TOKEN_SUCCESS) &&
      event.payload != null &&
      typeof event.payload === 'object' &&
      'account' in event.payload &&
      event.payload.account != null
    ) {
      const payload = event.payload as { account: import('@azure/msal-browser').AccountInfo };
      msalInstance.setActiveAccount(payload.account);
    }
  });

  ReactDOM.createRoot(target).render(
    <React.StrictMode>
      <ConfigProvider
        locale={zhTW}
        theme={{
          token: {
            colorPrimary: '#1677ff',
            borderRadius: 6,
          },
        }}
      >
        <AntdApp>
          <QueryClientProvider client={queryClient}>
            <MsalProvider instance={msalInstance}>
              <AuthProvider>
                <BrowserRouter>
                  <App />
                </BrowserRouter>
              </AuthProvider>
            </MsalProvider>
            {import.meta.env.DEV && <ReactQueryDevtools initialIsOpen={false} />}
          </QueryClientProvider>
        </AntdApp>
      </ConfigProvider>
    </React.StrictMode>,
  );
}

bootstrap(rootEl).catch((err) => {
  // eslint-disable-next-line no-console
  console.error('App bootstrap failed:', err);
  rootEl.innerHTML =
    '<pre style="padding:24px;color:#a8071a">App 啟動失敗，請檢查 Azure AD 設定或重新整理頁面。</pre>';
});
