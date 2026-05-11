/**
 * 開發模式假資料 + axios interceptor（自訂欄位）。
 *
 * 啟用方式：在 `web/.env.development` 設 `VITE_USE_MOCK_FIELDS=true`。
 * 後端 #7 [3.1.1] 落地後可關掉這個 flag，前端會自動改打真實 API。
 *
 * 攔截規則（僅針對 `/field-definitions*`）：
 * - GET   /field-definitions                                → MOCK_FIELDS
 * - GET   /field-definitions?includeInactive=true            → MOCK_FIELDS（含停用）
 * - POST  /field-definitions                                 → push 進 MOCK_FIELDS，version=1
 * - PUT   /field-definitions/{id}                            → 更新 MOCK_FIELDS[id]，version+=1，push 一筆 MOCK_VERSIONS
 * - GET   /field-definitions/{id}/versions                   → MOCK_VERSIONS.filter(fieldDefinitionId)
 *
 * **不會**攔截非 field-definitions 端點。
 */

import type { AxiosInstance } from 'axios';
import type {
  FieldDefinition,
  FieldDefinitionVersion,
} from './fieldDefinitions';

const NOW = () => new Date().toISOString();

const MOCK_FIELDS: FieldDefinition[] = [
  {
    id: 'field-priority',
    code: 'case.priority',
    label: '優先級',
    description: '案件處理的優先順序，影響首頁待辦排序。',
    fieldType: 'select',
    isRequired: true,
    config: {
      options: [
        { value: 'low', label: '低' },
        { value: 'medium', label: '中' },
        { value: 'high', label: '高' },
        { value: 'urgent', label: '緊急' },
      ],
    },
    version: 2,
    isActive: true,
    createdAt: '2026-04-01T00:00:00Z',
    updatedAt: '2026-04-20T10:30:00Z',
  },
  {
    id: 'field-customer-impact',
    code: 'case.customer_impact',
    label: '客戶影響說明',
    description: '此案件對客戶造成的影響描述（多行）。',
    fieldType: 'textarea',
    isRequired: false,
    config: { rows: 4, maxLength: 2000 },
    version: 1,
    isActive: true,
    createdAt: '2026-04-05T00:00:00Z',
    updatedAt: '2026-04-05T00:00:00Z',
  },
  {
    id: 'field-budget',
    code: 'case.estimated_budget',
    label: '預估預算',
    description: '預估完成此案件需要的預算（新台幣）。',
    fieldType: 'number',
    isRequired: false,
    config: { min: 0, precision: 0 },
    version: 1,
    isActive: true,
    createdAt: '2026-04-10T00:00:00Z',
    updatedAt: '2026-04-10T00:00:00Z',
  },
  {
    id: 'field-go-live-date',
    code: 'case.go_live_date',
    label: '預計上線日',
    fieldType: 'date',
    isRequired: false,
    version: 1,
    isActive: true,
    createdAt: '2026-04-12T00:00:00Z',
    updatedAt: '2026-04-12T00:00:00Z',
  },
  {
    id: 'field-needs-uat',
    code: 'case.needs_uat',
    label: '需要 UAT',
    description: '是否需要客戶進行使用者驗收測試。',
    fieldType: 'boolean',
    isRequired: false,
    version: 1,
    isActive: true,
    createdAt: '2026-04-15T00:00:00Z',
    updatedAt: '2026-04-15T00:00:00Z',
  },
  {
    id: 'field-tech-tags',
    code: 'case.tech_tags',
    label: '技術標籤',
    description: '此案件涉及的技術領域（可多選）。',
    fieldType: 'multiselect',
    isRequired: false,
    config: {
      options: [
        { value: 'frontend', label: '前端' },
        { value: 'backend', label: '後端' },
        { value: 'database', label: '資料庫' },
        { value: 'devops', label: 'DevOps' },
        { value: 'security', label: '資安' },
        { value: 'integration', label: '整合' },
      ],
      allowCustom: true,
    },
    version: 1,
    isActive: true,
    createdAt: '2026-04-18T00:00:00Z',
    updatedAt: '2026-04-18T00:00:00Z',
  },
  {
    id: 'field-deprecated-comment',
    code: 'case.legacy_note',
    label: '舊版備註欄位',
    description: '已停用：請改用「客戶影響說明」。',
    fieldType: 'text',
    isRequired: false,
    config: { maxLength: 500 },
    version: 3,
    isActive: false,
    createdAt: '2026-01-15T00:00:00Z',
    updatedAt: '2026-04-25T00:00:00Z',
  },
];

const MOCK_VERSIONS: FieldDefinitionVersion[] = [
  // priority v1（建立時）
  {
    id: 'fv-priority-1',
    fieldDefinitionId: 'field-priority',
    version: 1,
    snapshot: {
      id: 'field-priority',
      code: 'case.priority',
      label: '優先級',
      description: undefined,
      fieldType: 'select',
      isRequired: false,
      config: {
        options: [
          { value: 'low', label: '低' },
          { value: 'medium', label: '中' },
          { value: 'high', label: '高' },
        ],
      },
      isActive: true,
    },
    createdAt: '2026-04-01T00:00:00Z',
    createdBy: 'Alice Chen',
    changeNote: '初始建立',
  },
  // priority v2（加入「緊急」選項 + 改必填）
  {
    id: 'fv-priority-2',
    fieldDefinitionId: 'field-priority',
    version: 2,
    snapshot: {
      id: 'field-priority',
      code: 'case.priority',
      label: '優先級',
      description: '案件處理的優先順序，影響首頁待辦排序。',
      fieldType: 'select',
      isRequired: true,
      config: {
        options: [
          { value: 'low', label: '低' },
          { value: 'medium', label: '中' },
          { value: 'high', label: '高' },
          { value: 'urgent', label: '緊急' },
        ],
      },
      isActive: true,
    },
    createdAt: '2026-04-20T10:30:00Z',
    createdBy: 'Alice Chen',
    changeNote: '加入「緊急」選項，改為必填，加上欄位說明',
  },
  // legacy_note 三個版本
  {
    id: 'fv-legacy-1',
    fieldDefinitionId: 'field-deprecated-comment',
    version: 1,
    snapshot: {
      id: 'field-deprecated-comment',
      code: 'case.legacy_note',
      label: '舊版備註',
      fieldType: 'text',
      isRequired: false,
      config: { maxLength: 200 },
      isActive: true,
    },
    createdAt: '2026-01-15T00:00:00Z',
    createdBy: 'Alice Chen',
    changeNote: '初始建立',
  },
  {
    id: 'fv-legacy-2',
    fieldDefinitionId: 'field-deprecated-comment',
    version: 2,
    snapshot: {
      id: 'field-deprecated-comment',
      code: 'case.legacy_note',
      label: '舊版備註欄位',
      fieldType: 'text',
      isRequired: false,
      config: { maxLength: 500 },
      isActive: true,
    },
    createdAt: '2026-03-10T00:00:00Z',
    createdBy: 'Alice Chen',
    changeNote: '擴大字數上限至 500',
  },
  {
    id: 'fv-legacy-3',
    fieldDefinitionId: 'field-deprecated-comment',
    version: 3,
    snapshot: {
      id: 'field-deprecated-comment',
      code: 'case.legacy_note',
      label: '舊版備註欄位',
      description: '已停用：請改用「客戶影響說明」。',
      fieldType: 'text',
      isRequired: false,
      config: { maxLength: 500 },
      isActive: false,
    },
    createdAt: '2026-04-25T00:00:00Z',
    createdBy: 'Alice Chen',
    changeNote: '停用，導引使用者改用新欄位',
  },
];

// ---------- helpers ----------

function matchUrl(url: string | undefined, path: string): boolean {
  if (!url) return false;
  // 處理 query string
  const cleaned = url.split('?')[0];
  return cleaned === path || cleaned.endsWith(path);
}

function matchUrlPattern(url: string | undefined, pattern: RegExp): RegExpMatchArray | null {
  if (!url) return null;
  const cleaned = url.split('?')[0];
  return cleaned.match(pattern);
}

function makeId(prefix: string): string {
  return `${prefix}-${Math.random().toString(36).slice(2, 10)}`;
}

function fieldByIdOrThrow(id: string): FieldDefinition {
  const f = MOCK_FIELDS.find((x) => x.id === id);
  if (!f) throw new Error(`Mock: field-definition not found: ${id}`);
  return f;
}

function snapshotOf(field: FieldDefinition): FieldDefinitionVersion['snapshot'] {
  return {
    id: field.id,
    code: field.code,
    label: field.label,
    description: field.description,
    fieldType: field.fieldType,
    isRequired: field.isRequired,
    config: field.config,
    isActive: field.isActive,
  };
}

/**
 * 安裝 mock interceptor。在 main.tsx bootstrap 時依環境變數決定是否啟用。
 */
export function installMockFieldsInterceptor(client: AxiosInstance): void {
  client.interceptors.request.use(async (config) => {
    const url = config.url ?? '';
    const method = (config.method ?? 'get').toLowerCase();

    // GET /field-definitions（含 ?includeInactive=true）
    if (method === 'get' && matchUrl(url, '/field-definitions')) {
      const includeInactive =
        config.params?.includeInactive === true ||
        config.params?.includeInactive === 'true';
      const result = includeInactive
        ? MOCK_FIELDS
        : MOCK_FIELDS.filter((f) => f.isActive);
      return Promise.reject({
        __mock: true,
        config,
        status: 200,
        data: structuredClone(result),
      });
    }

    // GET /field-definitions/{id}/versions
    const versionsGet = matchUrlPattern(url, /\/field-definitions\/([^/]+)\/versions$/);
    if (method === 'get' && versionsGet) {
      const id = versionsGet[1];
      const list = MOCK_VERSIONS.filter((v) => v.fieldDefinitionId === id).sort(
        (a, b) => b.version - a.version,
      );
      return Promise.reject({ __mock: true, config, status: 200, data: structuredClone(list) });
    }

    // POST /field-definitions
    if (method === 'post' && matchUrl(url, '/field-definitions')) {
      const payload = config.data ? JSON.parse(config.data as string) : {};
      const newField: FieldDefinition = {
        id: makeId('field'),
        code: payload.code ?? 'unnamed',
        label: payload.label ?? '未命名欄位',
        description: payload.description,
        fieldType: payload.fieldType ?? 'text',
        isRequired: payload.isRequired ?? false,
        config: payload.config,
        version: 1,
        isActive: true,
        createdAt: NOW(),
        updatedAt: NOW(),
      };
      MOCK_FIELDS.push(newField);
      // 同時建立 v1 快照
      MOCK_VERSIONS.push({
        id: makeId('fv'),
        fieldDefinitionId: newField.id,
        version: 1,
        snapshot: snapshotOf(newField),
        createdAt: NOW(),
        createdBy: 'Mock User',
        changeNote: '初始建立',
      });
      return Promise.reject({
        __mock: true,
        config,
        status: 201,
        data: structuredClone(newField),
      });
    }

    // PUT /field-definitions/{id}
    const fieldPut = matchUrlPattern(url, /\/field-definitions\/([^/]+)$/);
    if (method === 'put' && fieldPut) {
      const id = fieldPut[1];
      const field = fieldByIdOrThrow(id);
      const payload = config.data ? JSON.parse(config.data as string) : {};
      // 不允許改 code / fieldType（守規定）
      if (payload.label !== undefined) field.label = payload.label;
      if (payload.description !== undefined) field.description = payload.description;
      if (payload.isRequired !== undefined) field.isRequired = payload.isRequired;
      if (payload.config !== undefined) field.config = payload.config;
      if (payload.isActive !== undefined) field.isActive = payload.isActive;
      field.version += 1;
      field.updatedAt = NOW();
      // 建立新版本快照
      MOCK_VERSIONS.push({
        id: makeId('fv'),
        fieldDefinitionId: field.id,
        version: field.version,
        snapshot: snapshotOf(field),
        createdAt: NOW(),
        createdBy: 'Mock User',
        changeNote: payload.changeNote,
      });
      return Promise.reject({
        __mock: true,
        config,
        status: 200,
        data: structuredClone(field),
      });
    }

    // 不命中 → 放行
    return config;
  });

  client.interceptors.response.use(
    (response) => response,
    (error) => {
      if (error && error.__mock) {
        return Promise.resolve({
          data: error.data,
          status: error.status,
          statusText: 'OK (mock)',
          headers: {},
          config: error.config,
        });
      }
      return Promise.reject(error);
    },
  );
}

export const __TEST_ONLY = { MOCK_FIELDS, MOCK_VERSIONS };
