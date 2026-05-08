/**
 * 開發模式假資料 + axios interceptor（流程範本）。
 *
 * 啟用方式：在 `web/.env.development` 設 `VITE_USE_MOCK_TEMPLATES=true`。
 * 後端 #12 [3.2.1] 落地後可關掉這個 flag，前端會自動改打真實 API。
 *
 * 攔截規則（僅針對 `/workflow-templates*`）：
 * - GET   /workflow-templates                                → MOCK_TEMPLATES
 * - GET   /workflow-templates?includeInactive=true            → MOCK_TEMPLATES（含停用）
 * - GET   /workflow-templates/{id}                            → MOCK_TEMPLATES.find(id)
 * - POST  /workflow-templates                                 → push 進 MOCK_TEMPLATES（draft）
 * - PUT   /workflow-templates/{id}                            → 更新 MOCK_TEMPLATES[id]（不 bump version）
 * - PUT   /workflow-templates/{id}/publish                    → version+=1, publishedAt=NOW, push MOCK_VERSIONS
 * - GET   /workflow-templates/{id}/versions                   → MOCK_VERSIONS.filter(templateId)
 */

import type { AxiosInstance } from 'axios';
import type {
  WorkflowTemplate,
  WorkflowTemplateVersion,
  WorkflowNode,
} from './workflowTemplates';

const NOW = () => new Date().toISOString();

function makeId(prefix: string): string {
  return `${prefix}-${Math.random().toString(36).slice(2, 10)}`;
}

const WORK_REQUEST_NODES: WorkflowNode[] = [
  { nodeOrder: 1, nodeKey: 'start', label: '案件發起', nodeType: 'start' },
  {
    nodeOrder: 2,
    nodeKey: 'pm-confirm',
    label: 'PM 確認需求',
    nodeType: 'handle',
    requiredRoleId: 'role-pm',
    description: '確認需求合理性與優先順序',
    expectedHours: 4,
  },
  {
    nodeOrder: 3,
    nodeKey: 'engineer-handle',
    label: '工程師處理',
    nodeType: 'handle',
    requiredRoleId: 'role-engineer',
    description: '依需求進行實作',
    expectedHours: 24,
  },
  {
    nodeOrder: 4,
    nodeKey: 'pm-approve',
    label: 'PM 核准結案',
    nodeType: 'approve',
    requiredRoleId: 'role-pm',
    description: '驗收結果並結案',
    expectedHours: 2,
  },
  { nodeOrder: 5, nodeKey: 'end', label: '結案', nodeType: 'end' },
];

const SPEC_CHANGE_NODES: WorkflowNode[] = [
  { nodeOrder: 1, nodeKey: 'start', label: '規格變更發起', nodeType: 'start' },
  {
    nodeOrder: 2,
    nodeKey: 'consultant-review',
    label: '顧問審查',
    nodeType: 'handle',
    requiredRoleId: 'role-consultant',
    description: '評估變更影響範圍',
  },
  {
    nodeOrder: 3,
    nodeKey: 'pm-approve',
    label: 'PM 核准',
    nodeType: 'approve',
    requiredRoleId: 'role-pm',
  },
  {
    nodeOrder: 4,
    nodeKey: 'team-notify',
    label: '通知開發團隊',
    nodeType: 'notify',
    description: '副知所有相關工程師',
  },
  { nodeOrder: 5, nodeKey: 'end', label: '結案', nodeType: 'end' },
];

const FEEDBACK_NODES: WorkflowNode[] = [
  { nodeOrder: 1, nodeKey: 'start', label: '客戶反饋發起', nodeType: 'start' },
  {
    nodeOrder: 2,
    nodeKey: 'cs-handle',
    label: '客服初判',
    nodeType: 'handle',
    requiredRoleId: 'role-customer-service',
  },
  { nodeOrder: 3, nodeKey: 'end', label: '結案', nodeType: 'end' },
];

const MOCK_TEMPLATES: WorkflowTemplate[] = [
  {
    id: 'tpl-work-request',
    code: 'work_request',
    name: '工作需求單',
    description: '主流程：案件發起、PM 確認、工程師處理、PM 核准結案。',
    version: 3,
    nodes: WORK_REQUEST_NODES,
    publishedAt: '2026-04-22T09:00:00Z',
    isActive: true,
    hasDraftChanges: false,
    createdAt: '2026-03-01T00:00:00Z',
    updatedAt: '2026-04-22T09:00:00Z',
  },
  {
    id: 'tpl-spec-change',
    code: 'spec_change',
    name: '規格變更',
    description: '子流程：由工作需求單衍生，需顧問審查 + PM 核准。',
    version: 1,
    nodes: SPEC_CHANGE_NODES,
    publishedAt: '2026-03-15T10:00:00Z',
    isActive: true,
    hasDraftChanges: true,
    createdAt: '2026-03-15T00:00:00Z',
    updatedAt: '2026-05-01T10:30:00Z',
  },
  {
    id: 'tpl-feedback',
    code: 'customer_feedback',
    name: '客戶反饋（草稿）',
    description: '客服初判流程，尚未發行。',
    version: 0,
    nodes: FEEDBACK_NODES,
    publishedAt: null,
    isActive: true,
    hasDraftChanges: true,
    createdAt: '2026-05-05T00:00:00Z',
    updatedAt: '2026-05-05T00:00:00Z',
  },
];

const MOCK_VERSIONS: WorkflowTemplateVersion[] = [
  {
    id: 'tv-wr-1',
    templateId: 'tpl-work-request',
    version: 1,
    snapshot: {
      id: 'tpl-work-request',
      code: 'work_request',
      name: '工作需求單',
      description: '初版：發起 → 工程師 → 結束（無 PM 確認）',
      nodes: [
        { nodeOrder: 1, nodeKey: 'start', label: '案件發起', nodeType: 'start' },
        {
          nodeOrder: 2,
          nodeKey: 'engineer',
          label: '工程師處理',
          nodeType: 'handle',
          requiredRoleId: 'role-engineer',
        },
        { nodeOrder: 3, nodeKey: 'end', label: '結案', nodeType: 'end' },
      ],
      isActive: true,
      version: 1,
    },
    publishedAt: '2026-03-01T08:00:00Z',
    publishedBy: 'Alice Chen',
    changeNote: '初始發行',
  },
  {
    id: 'tv-wr-2',
    templateId: 'tpl-work-request',
    version: 2,
    snapshot: {
      id: 'tpl-work-request',
      code: 'work_request',
      name: '工作需求單',
      description: '加入 PM 確認節點',
      nodes: [
        { nodeOrder: 1, nodeKey: 'start', label: '案件發起', nodeType: 'start' },
        {
          nodeOrder: 2,
          nodeKey: 'pm-confirm',
          label: 'PM 確認需求',
          nodeType: 'handle',
          requiredRoleId: 'role-pm',
        },
        {
          nodeOrder: 3,
          nodeKey: 'engineer-handle',
          label: '工程師處理',
          nodeType: 'handle',
          requiredRoleId: 'role-engineer',
        },
        { nodeOrder: 4, nodeKey: 'end', label: '結案', nodeType: 'end' },
      ],
      isActive: true,
      version: 2,
    },
    publishedAt: '2026-04-05T10:00:00Z',
    publishedBy: 'Alice Chen',
    changeNote: '加入 PM 確認需求節點，避免直接進工程師處理',
  },
  {
    id: 'tv-wr-3',
    templateId: 'tpl-work-request',
    version: 3,
    snapshot: {
      id: 'tpl-work-request',
      code: 'work_request',
      name: '工作需求單',
      description: '加入 PM 核准結案節點，符合驗收條件',
      nodes: WORK_REQUEST_NODES,
      isActive: true,
      version: 3,
    },
    publishedAt: '2026-04-22T09:00:00Z',
    publishedBy: 'Alice Chen',
    changeNote: '加入 PM 核准結案節點，PM 核准後才結案',
  },
  {
    id: 'tv-sc-1',
    templateId: 'tpl-spec-change',
    version: 1,
    snapshot: {
      id: 'tpl-spec-change',
      code: 'spec_change',
      name: '規格變更',
      nodes: SPEC_CHANGE_NODES,
      isActive: true,
      version: 1,
    },
    publishedAt: '2026-03-15T10:00:00Z',
    publishedBy: 'Alice Chen',
    changeNote: '初始發行',
  },
];

// ---------- helpers ----------

function matchUrl(url: string | undefined, path: string): boolean {
  if (!url) return false;
  const cleaned = url.split('?')[0];
  return cleaned === path || cleaned.endsWith(path);
}

function matchUrlPattern(url: string | undefined, pattern: RegExp): RegExpMatchArray | null {
  if (!url) return null;
  const cleaned = url.split('?')[0];
  return cleaned.match(pattern);
}

function templateByIdOrThrow(id: string): WorkflowTemplate {
  const t = MOCK_TEMPLATES.find((x) => x.id === id);
  if (!t) throw new Error(`Mock: workflow-template not found: ${id}`);
  return t;
}

function snapshotOf(template: WorkflowTemplate): WorkflowTemplateVersion['snapshot'] {
  return {
    id: template.id,
    code: template.code,
    name: template.name,
    description: template.description,
    nodes: structuredClone(template.nodes),
    isActive: template.isActive,
    version: template.version,
  };
}

/**
 * 安裝 mock interceptor。在 main.tsx bootstrap 時依環境變數決定是否啟用。
 */
export function installMockTemplatesInterceptor(client: AxiosInstance): void {
  client.interceptors.request.use(async (config) => {
    const url = config.url ?? '';
    const method = (config.method ?? 'get').toLowerCase();

    // GET /workflow-templates（含 ?includeInactive）
    if (method === 'get' && matchUrl(url, '/workflow-templates')) {
      const includeInactive =
        config.params?.includeInactive === true ||
        config.params?.includeInactive === 'true';
      const result = includeInactive
        ? MOCK_TEMPLATES
        : MOCK_TEMPLATES.filter((t) => t.isActive);
      return Promise.reject({
        __mock: true,
        config,
        status: 200,
        data: structuredClone(result),
      });
    }

    // GET /workflow-templates/{id}/versions
    const versionsGet = matchUrlPattern(url, /\/workflow-templates\/([^/]+)\/versions$/);
    if (method === 'get' && versionsGet) {
      const id = versionsGet[1];
      const list = MOCK_VERSIONS.filter((v) => v.templateId === id).sort(
        (a, b) => b.version - a.version,
      );
      return Promise.reject({ __mock: true, config, status: 200, data: structuredClone(list) });
    }

    // GET /workflow-templates/{id}
    const tplGet = matchUrlPattern(url, /\/workflow-templates\/([^/]+)$/);
    if (method === 'get' && tplGet) {
      const id = tplGet[1];
      const t = MOCK_TEMPLATES.find((x) => x.id === id);
      if (!t) {
        return Promise.reject({ __mock: true, config, status: 404, data: { message: 'Not found' } });
      }
      return Promise.reject({ __mock: true, config, status: 200, data: structuredClone(t) });
    }

    // POST /workflow-templates
    if (method === 'post' && matchUrl(url, '/workflow-templates')) {
      const payload = config.data ? JSON.parse(config.data as string) : {};
      const newTpl: WorkflowTemplate = {
        id: makeId('tpl'),
        code: payload.code ?? 'unnamed',
        name: payload.name ?? '未命名範本',
        description: payload.description,
        version: 0,
        nodes: payload.nodes ?? [],
        publishedAt: null,
        isActive: true,
        hasDraftChanges: true,
        createdAt: NOW(),
        updatedAt: NOW(),
      };
      MOCK_TEMPLATES.push(newTpl);
      return Promise.reject({
        __mock: true,
        config,
        status: 201,
        data: structuredClone(newTpl),
      });
    }

    // PUT /workflow-templates/{id}/publish
    const publishPut = matchUrlPattern(url, /\/workflow-templates\/([^/]+)\/publish$/);
    if (method === 'put' && publishPut) {
      const id = publishPut[1];
      const tpl = templateByIdOrThrow(id);
      const payload = config.data ? JSON.parse(config.data as string) : {};
      tpl.version += 1;
      tpl.publishedAt = NOW();
      tpl.hasDraftChanges = false;
      tpl.updatedAt = NOW();
      MOCK_VERSIONS.push({
        id: makeId('tv'),
        templateId: tpl.id,
        version: tpl.version,
        snapshot: snapshotOf(tpl),
        publishedAt: tpl.publishedAt!,
        publishedBy: 'Mock User',
        changeNote: payload.changeNote,
      });
      return Promise.reject({
        __mock: true,
        config,
        status: 200,
        data: structuredClone(tpl),
      });
    }

    // PUT /workflow-templates/{id}（不 bump version）
    const tplPut = matchUrlPattern(url, /\/workflow-templates\/([^/]+)$/);
    if (method === 'put' && tplPut) {
      const id = tplPut[1];
      const tpl = templateByIdOrThrow(id);
      const payload = config.data ? JSON.parse(config.data as string) : {};
      if (payload.name !== undefined) tpl.name = payload.name;
      if (payload.description !== undefined) tpl.description = payload.description;
      if (payload.nodes !== undefined) {
        tpl.nodes = payload.nodes;
        tpl.hasDraftChanges = true;
      }
      if (payload.isActive !== undefined) tpl.isActive = payload.isActive;
      tpl.updatedAt = NOW();
      return Promise.reject({
        __mock: true,
        config,
        status: 200,
        data: structuredClone(tpl),
      });
    }

    return config;
  });

  client.interceptors.response.use(
    (response) => response,
    (error) => {
      if (error && error.__mock) {
        if (error.status >= 400) {
          return Promise.reject({
            response: { status: error.status, data: error.data },
            config: error.config,
            message: 'Mock error',
          });
        }
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

export const __TEST_ONLY = { MOCK_TEMPLATES, MOCK_VERSIONS };
