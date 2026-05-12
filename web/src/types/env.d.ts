/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string;
  readonly VITE_API_PREFIX: string;
  readonly VITE_AZURE_CLIENT_ID?: string;
  readonly VITE_AZURE_TENANT_ID?: string;
  readonly VITE_AZURE_REDIRECT_URI?: string;
  readonly VITE_AZURE_API_SCOPE?: string;
  /** issue #8 [2.2.2]：啟用 RBAC mock interceptor（'true' / 'false'） */
  readonly VITE_USE_MOCK_RBAC?: string;
  /** issue #11 [3.1.2]：啟用自訂欄位 mock interceptor（'true' / 'false'） */
  readonly VITE_USE_MOCK_FIELDS?: string;
  /** issue #13 [3.2.2]：啟用流程範本 mock interceptor（'true' / 'false'） */
  readonly VITE_USE_MOCK_TEMPLATES?: string;
  /** issue #21 [5.5]：啟用案件 mock interceptor（'true' / 'false'） */
  readonly VITE_USE_MOCK_CASES?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
