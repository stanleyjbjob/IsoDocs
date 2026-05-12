import type { AxiosInstance, InternalAxiosRequestConfig } from 'axios';
import type {
  CaseDetail,
  CaseSummary,
  CaseAction,
  CaseNodeProgress,
  CaseFieldValue,
  CaseRelationItem,
  CaseCreatePayload,
  CaseAssignPayload,
  CaseActionPayload,
  SpawnChildPayload,
  ReopenPayload,
  UpdateExpectedCompletionPayload,
} from './cases';

let currentUser = { id: 'u-alice', name: 'Alice (示範)' };

export function setMockCasesCurrentUser(id: string, name: string) {
  currentUser = { id, name };
}

function nextCaseNumber(typeCode: string): string {
  const yy = String(new Date().getFullYear()).slice(-2);
  caseSequence += 1;
  const seq = String(caseSequence).padStart(4, '0');
  return `ITCT-${typeCode}-${yy}${seq}`;
}

let caseSequence = 80;
let actionSeq = 0;

function nextActionId() {
  actionSeq += 1;
  return `act-${actionSeq.toString().padStart(4, '0')}`;
}

function now() {
  return new Date().toISOString();
}

function daysAgo(d: number) {
  return new Date(Date.now() - d * 86400000).toISOString();
}

function daysFromNow(d: number) {
  return new Date(Date.now() + d * 86400000).toISOString();
}

function makeNode(
  key: string,
  label: string,
  type: CaseNodeProgress['nodeType'],
  status: CaseNodeProgress['status'],
  assignee: { id: string | null; name: string | null },
  enteredAt: string | null,
  completedAt: string | null,
  expectedHours: number | null = null,
  comment: string | null = null,
): CaseNodeProgress {
  return {
    nodeId: `n-${key}-${Math.random().toString(36).slice(2, 7)}`,
    nodeKey: key,
    label,
    nodeType: type,
    requiredRoleId: type === 'approve' ? 'role-pm' : type === 'handle' ? 'role-engineer' : null,
    requiredRoleName: type === 'approve' ? '專案經理' : type === 'handle' ? '工程師' : null,
    assigneeUserId: assignee.id,
    assigneeName: assignee.name,
    status,
    expectedHours,
    enteredAt,
    completedAt,
    modifiedExpectedAt: null,
    comment,
  };
}

function makeField(code: string, label: string, fieldType: string, value: unknown, required = false): CaseFieldValue {
  return {
    fieldDefinitionId: `fd-${code}`,
    fieldVersionId: `fv-${code}-1`,
    code,
    label,
    fieldType,
    value,
    required,
  };
}

function baseCase(partial: Partial<CaseDetail> & { id: string; title: string }): CaseDetail {
  return {
    id: partial.id,
    caseNumber: partial.caseNumber ?? nextCaseNumber('F01'),
    title: partial.title,
    status: 'in_progress',
    templateId: 't-work-request',
    templateCode: 'work-request',
    templateName: '工作需求單',
    templateVersion: 3,
    documentTypeCode: 'F01',
    customerId: null,
    customerName: null,
    initiatorUserId: 'u-alice',
    initiatorName: 'Alice (示範)',
    initiatedAt: daysAgo(5),
    originalExpectedAt: daysFromNow(7),
    expectedCompletionAt: daysFromNow(7),
    completedAt: null,
    voidedAt: null,
    customVersion: null,
    currentNodeKey: null,
    currentAssigneeName: null,
    currentAssigneeUserId: null,
    description: '示範用案件描述',
    fields: [],
    nodes: [],
    actions: [],
    relations: [],
    currentNodeId: null,
    ...partial,
  };
}

function recomputeCurrent(c: CaseDetail) {
  const cur = c.nodes.find((n) => n.status === 'in_progress');
  c.currentNodeId = cur?.nodeId ?? null;
  c.currentNodeKey = cur?.nodeKey ?? null;
  c.currentAssigneeName = cur?.assigneeName ?? null;
  c.currentAssigneeUserId = cur?.assigneeUserId ?? null;
}

function toSummary(c: CaseDetail): CaseSummary {
  return {
    id: c.id,
    caseNumber: c.caseNumber,
    title: c.title,
    status: c.status,
    templateId: c.templateId,
    templateCode: c.templateCode,
    templateName: c.templateName,
    templateVersion: c.templateVersion,
    documentTypeCode: c.documentTypeCode,
    customerId: c.customerId,
    customerName: c.customerName,
    initiatorUserId: c.initiatorUserId,
    initiatorName: c.initiatorName,
    initiatedAt: c.initiatedAt,
    originalExpectedAt: c.originalExpectedAt,
    expectedCompletionAt: c.expectedCompletionAt,
    completedAt: c.completedAt,
    voidedAt: c.voidedAt,
    customVersion: c.customVersion,
    currentNodeKey: c.currentNodeKey,
    currentAssigneeName: c.currentAssigneeName,
    currentAssigneeUserId: c.currentAssigneeUserId,
  };
}

function logAction(
  c: CaseDetail,
  type: CaseAction['actionType'],
  comment: string | null = null,
  metadata?: Record<string, unknown>,
) {
  c.actions.push({
    id: nextActionId(),
    caseId: c.id,
    caseNodeId: c.currentNodeId,
    actionType: type,
    actorUserId: currentUser.id,
    actorName: currentUser.name,
    actionAt: now(),
    comment,
    metadata,
  });
}

const SEED_CASES: CaseDetail[] = [];

(function seed() {
  const c1 = baseCase({
    id: 'c-1',
    title: '會議室預約系統需修正時區顯示',
    description: '報修者反映預約時間在跨時區同步時會錯一小時。',
    initiatedAt: daysAgo(3),
    originalExpectedAt: daysFromNow(5),
    expectedCompletionAt: daysFromNow(5),
  });
  c1.fields = [
    makeField('priority', '優先順序', 'select', 'high', true),
    makeField('description', '需求描述', 'textarea', '使用 dayjs 處理 UTC 偏移時誤算 +0', true),
    makeField('expected_hours', '預計工時', 'number', 8, false),
    makeField('test_required', '需要測試', 'boolean', true, false),
  ];
  c1.nodes = [
    makeNode('init', '發起', 'start', 'completed', { id: 'u-alice', name: 'Alice (示範)' }, daysAgo(3), daysAgo(3)),
    makeNode('triage', '分派', 'handle', 'completed', { id: 'u-alice', name: 'Alice (示範)' }, daysAgo(3), daysAgo(2)),
    makeNode('handle', '處理', 'handle', 'in_progress', { id: 'u-bob', name: 'Bob' }, daysAgo(2), null, 8),
    makeNode('approve', '核准結案', 'approve', 'pending', { id: null, name: null }, null, null),
    makeNode('end', '結案', 'end', 'pending', { id: null, name: null }, null, null),
  ];
  c1.actions = [
    {
      id: nextActionId(),
      caseId: c1.id,
      caseNodeId: c1.nodes[0].nodeId,
      actionType: 'create',
      actorUserId: 'u-alice',
      actorName: 'Alice (示範)',
      actionAt: daysAgo(3),
      comment: '發起案件',
    },
    {
      id: nextActionId(),
      caseId: c1.id,
      caseNodeId: c1.nodes[1].nodeId,
      actionType: 'assign',
      actorUserId: 'u-alice',
      actorName: 'Alice (示範)',
      actionAt: daysAgo(2),
      comment: '分派給 Bob',
      metadata: { assigneeUserId: 'u-bob' },
    },
    {
      id: nextActionId(),
      caseId: c1.id,
      caseNodeId: c1.nodes[2].nodeId,
      actionType: 'accept',
      actorUserId: 'u-bob',
      actorName: 'Bob',
      actionAt: daysAgo(2),
      comment: '已接單',
    },
  ];
  recomputeCurrent(c1);
  SEED_CASES.push(c1);

  const c2 = baseCase({
    id: 'c-2',
    title: '客戶 ABC 規格書修訂 V1.2',
    description: '依客戶會議結論修訂規格書版本。',
    customerId: 'cust-abc',
    customerName: '客戶 ABC',
    customVersion: 'SPEC-ABC-2026.05-V1.2',
    initiatedAt: daysAgo(1),
    originalExpectedAt: daysFromNow(10),
    expectedCompletionAt: daysFromNow(10),
  });
  c2.fields = [
    makeField('priority', '優先順序', 'select', 'medium', true),
    makeField('description', '需求描述', 'textarea', '依 5/3 客戶會議結論調整 §3.2 介面定義。', true),
  ];
  c2.nodes = [
    makeNode('init', '發起', 'start', 'completed', { id: 'u-alice', name: 'Alice (示範)' }, daysAgo(1), daysAgo(1)),
    makeNode('handle', '處理', 'handle', 'in_progress', { id: 'u-alice', name: 'Alice (示範)' }, daysAgo(1), null, 16),
    makeNode('end', '結案', 'end', 'pending', { id: null, name: null }, null, null),
  ];
  c2.actions = [
    {
      id: nextActionId(),
      caseId: c2.id,
      caseNodeId: c2.nodes[0].nodeId,
      actionType: 'create',
      actorUserId: 'u-alice',
      actorName: 'Alice (示範)',
      actionAt: daysAgo(1),
      comment: '發起案件',
    },
  ];
  recomputeCurrent(c2);
  SEED_CASES.push(c2);

  const c3 = baseCase({
    id: 'c-3',
    title: '舊版報表頁面退場規劃',
    status: 'completed',
    initiatedAt: daysAgo(20),
    originalExpectedAt: daysAgo(5),
    expectedCompletionAt: daysAgo(5),
    completedAt: daysAgo(4),
  });
  c3.fields = [makeField('priority', '優先順序', 'select', 'low', true)];
  c3.nodes = [
    makeNode('init', '發起', 'start', 'completed', { id: 'u-alice', name: 'Alice (示範)' }, daysAgo(20), daysAgo(20)),
    makeNode('handle', '處理', 'handle', 'completed', { id: 'u-bob', name: 'Bob' }, daysAgo(20), daysAgo(6)),
    makeNode('approve', '核准結案', 'approve', 'completed', { id: 'u-charlie', name: 'Charlie (PM)' }, daysAgo(6), daysAgo(4)),
    makeNode('end', '結案', 'end', 'completed', { id: null, name: null }, daysAgo(4), daysAgo(4)),
  ];
  recomputeCurrent(c3);
  SEED_CASES.push(c3);

  const c4 = baseCase({
    id: 'c-4',
    title: '臨時調整：開會延後處理',
    status: 'voided',
    initiatedAt: daysAgo(15),
    voidedAt: daysAgo(13),
  });
  c4.fields = [];
  c4.nodes = [
    makeNode('init', '發起', 'start', 'completed', { id: 'u-alice', name: 'Alice (示範)' }, daysAgo(15), daysAgo(15)),
    makeNode('handle', '處理', 'handle', 'skipped', { id: null, name: null }, null, null),
  ];
  c4.actions = [
    {
      id: nextActionId(),
      caseId: c4.id,
      caseNodeId: c4.nodes[0].nodeId,
      actionType: 'void',
      actorUserId: 'u-alice',
      actorName: 'Alice (示範)',
      actionAt: daysAgo(13),
      comment: '會議結論不需此案，作廢處理',
    },
  ];
  recomputeCurrent(c4);
  SEED_CASES.push(c4);

  const c5 = baseCase({
    id: 'c-5',
    title: '規格變更：跨時區處理規則文件補充',
    templateId: 't-spec-change',
    templateCode: 'spec-change',
    templateName: '規格變更',
    templateVersion: 1,
    documentTypeCode: 'F03',
    initiatedAt: daysAgo(2),
  });
  c5.fields = [
    makeField('change_reason', '變更原因', 'textarea', 'C-1 修正連帶需更新規格說明。', true),
    makeField('priority', '優先順序', 'select', 'high', true),
  ];
  c5.nodes = [
    makeNode('init', '發起', 'start', 'completed', { id: 'u-alice', name: 'Alice (示範)' }, daysAgo(2), daysAgo(2)),
    makeNode('handle', '處理', 'handle', 'in_progress', { id: 'u-charlie', name: 'Charlie (PM)' }, daysAgo(2), null),
  ];

  const c1Ref = SEED_CASES[0];
  c5.relations = [
    {
      caseId: c1Ref.id,
      caseNumber: c1Ref.caseNumber,
      title: c1Ref.title,
      status: c1Ref.status,
      relationType: 'spawn',
      iAmChild: true,
    },
  ];
  recomputeCurrent(c5);
  SEED_CASES.push(c5);

  c1Ref.relations.push({
    caseId: c5.id,
    caseNumber: c5.caseNumber,
    title: c5.title,
    status: c5.status,
    relationType: 'spawn',
    iAmChild: false,
  });
})();

const CASES_BY_ID = new Map<string, CaseDetail>(SEED_CASES.map((c) => [c.id, c]));

function findById(id: string): CaseDetail | undefined {
  return CASES_BY_ID.get(id);
}

function matchPath(url: string, pattern: RegExp): RegExpMatchArray | null {
  const path = url.split('?')[0];
  return path.match(pattern);
}

function advanceToNextNode(c: CaseDetail) {
  const idx = c.nodes.findIndex((n) => n.status === 'in_progress');
  if (idx < 0) return;
  c.nodes[idx].status = 'completed';
  c.nodes[idx].completedAt = now();
  for (let i = idx + 1; i < c.nodes.length; i += 1) {
    if (c.nodes[i].status === 'pending') {
      if (c.nodes[i].nodeType === 'end') {
        c.nodes[i].status = 'completed';
        c.nodes[i].enteredAt = now();
        c.nodes[i].completedAt = now();
        c.status = 'completed';
        c.completedAt = now();
      } else {
        c.nodes[i].status = 'in_progress';
        c.nodes[i].enteredAt = now();
      }
      break;
    }
  }
  recomputeCurrent(c);
}

function rejectToPrev(c: CaseDetail, comment: string | null) {
  const idx = c.nodes.findIndex((n) => n.status === 'in_progress');
  if (idx < 0) return;
  for (let i = idx - 1; i >= 0; i -= 1) {
    if (c.nodes[i].nodeType === 'handle' && c.nodes[i].status === 'completed') {
      c.nodes[idx].status = 'rejected';
      c.nodes[idx].comment = comment;
      c.nodes[i].status = 'in_progress';
      c.nodes[i].completedAt = null;
      recomputeCurrent(c);
      return;
    }
  }
}

export function installMockCasesInterceptor(client: AxiosInstance): void {
  client.interceptors.request.use((config: InternalAxiosRequestConfig) => {
    const url = config.url ?? '';
    if (!url.startsWith('/cases')) return config;

    config.adapter = async () => {
      const respond = (status: number, data: unknown) => ({
        data,
        status,
        statusText: 'OK',
        headers: {},
        config,
        request: {},
      });

      if (config.method === 'get' && /^\/cases(\?.*)?$/.test(url)) {
        const params = (config.params ?? {}) as Record<string, unknown>;
        let items = [...CASES_BY_ID.values()];
        if (params.status) items = items.filter((c) => c.status === params.status);
        if (params.mineOnly === true || params.mineOnly === 'true') {
          items = items.filter(
            (c) => c.initiatorUserId === currentUser.id || c.currentAssigneeUserId === currentUser.id,
          );
        }
        if (params.templateCode) items = items.filter((c) => c.templateCode === params.templateCode);
        if (params.customerId) items = items.filter((c) => c.customerId === params.customerId);
        const summaries = items.map(toSummary).sort((a, b) => b.initiatedAt.localeCompare(a.initiatedAt));
        return respond(200, { items: summaries, totalCount: summaries.length });
      }

      let m = matchPath(url, /^\/cases\/([^/]+)$/);
      if (m && config.method === 'get') {
        const c = findById(m[1]);
        if (!c) return respond(404, { code: 'NOT_FOUND' });
        return respond(200, c);
      }

      if (config.method === 'post' && /^\/cases$/.test(url)) {
        const payload = config.data as CaseCreatePayload;
        const newId = `c-${Date.now()}`;
        const newCase = baseCase({
          id: newId,
          title: payload.title,
          description: payload.description ?? null,
          customerId: payload.customerId ?? null,
          expectedCompletionAt: payload.expectedCompletionAt ?? null,
          originalExpectedAt: payload.expectedCompletionAt ?? null,
          customVersion: payload.customVersion ?? null,
          documentTypeCode: payload.documentTypeCode,
          templateId: payload.templateId,
          initiatorUserId: currentUser.id,
          initiatorName: currentUser.name,
          initiatedAt: now(),
        });
        newCase.fields = (payload.fieldValues ?? []).map((fv) => makeField(fv.fieldDefinitionId, fv.fieldDefinitionId, 'text', fv.value));
        const initialAssignee = payload.initialAssigneeUserId
          ? { id: payload.initialAssigneeUserId, name: '指定承辦人' }
          : { id: currentUser.id, name: currentUser.name };
        newCase.nodes = [
          makeNode('init', '發起', 'start', 'completed', { id: currentUser.id, name: currentUser.name }, now(), now()),
          makeNode('handle', '處理', 'handle', 'in_progress', initialAssignee, now(), null),
          makeNode('approve', '核准結案', 'approve', 'pending', { id: null, name: null }, null, null),
          makeNode('end', '結案', 'end', 'pending', { id: null, name: null }, null, null),
        ];
        logAction(newCase, 'create', '發起案件');
        recomputeCurrent(newCase);
        CASES_BY_ID.set(newId, newCase);
        return respond(201, newCase);
      }

      m = matchPath(url, /^\/cases\/([^/]+)\/assign$/);
      if (m && config.method === 'put') {
        const c = findById(m[1]);
        if (!c) return respond(404, { code: 'NOT_FOUND' });
        const payload = config.data as CaseAssignPayload;
        const cur = c.nodes.find((n) => n.status === 'in_progress');
        if (cur) {
          cur.assigneeUserId = payload.userId;
          cur.assigneeName = `指定承辦人 ${payload.userId}`;
          recomputeCurrent(c);
          logAction(c, 'assign', payload.comment ?? null, { assigneeUserId: payload.userId });
        }
        return respond(200, c);
      }

      m = matchPath(url, /^\/cases\/([^/]+)\/actions\/([a-z-]+)$/);
      if (m && config.method === 'post') {
        const c = findById(m[1]);
        if (!c) return respond(404, { code: 'NOT_FOUND' });
        const action = m[2];
        const payload = (config.data ?? {}) as CaseActionPayload & SpawnChildPayload & ReopenPayload;
        const comment = payload.comment ?? null;

        if (action === 'accept') {
          logAction(c, 'accept', comment);
          return respond(200, c);
        }
        if (action === 'reply-close' || action === 'approve') {
          advanceToNextNode(c);
          logAction(c, action === 'reply-close' ? 'reply_close' : 'approve', comment);
          return respond(200, c);
        }
        if (action === 'reject') {
          rejectToPrev(c, comment);
          logAction(c, 'reject', comment);
          return respond(200, c);
        }
        if (action === 'void') {
          c.status = 'voided';
          c.voidedAt = now();
          c.nodes.forEach((n) => {
            if (n.status === 'in_progress' || n.status === 'pending') n.status = 'skipped';
          });
          recomputeCurrent(c);
          logAction(c, 'void', comment);
          c.relations
            .filter((r) => r.relationType === 'spawn' && !r.iAmChild)
            .forEach((r) => {
              const child = findById(r.caseId);
              if (child && child.status === 'in_progress') {
                child.status = 'voided';
                child.voidedAt = now();
                child.nodes.forEach((n) => {
                  if (n.status === 'in_progress' || n.status === 'pending') n.status = 'skipped';
                });
                recomputeCurrent(child);
                logAction(child, 'void', '主案作廢連鎖作廢');
              }
            });
          return respond(200, c);
        }
        if (action === 'spawn-child') {
          const newId = `c-${Date.now()}`;
          const child = baseCase({
            id: newId,
            title: payload.title,
            description: payload.description ?? null,
            templateId: payload.templateId,
            templateCode: 'spec-change',
            templateName: '規格變更',
            templateVersion: 1,
            documentTypeCode: 'F03',
            initiatorUserId: currentUser.id,
            initiatorName: currentUser.name,
            initiatedAt: now(),
          });
          child.nodes = [
            makeNode('init', '發起', 'start', 'completed', { id: currentUser.id, name: currentUser.name }, now(), now()),
            makeNode('handle', '處理', 'handle', 'in_progress', { id: currentUser.id, name: currentUser.name }, now(), null),
            makeNode('end', '結案', 'end', 'pending', { id: null, name: null }, null, null),
          ];
          child.relations.push({
            caseId: c.id,
            caseNumber: c.caseNumber,
            title: c.title,
            status: c.status,
            relationType: 'spawn',
            iAmChild: true,
          });
          c.relations.push({
            caseId: child.id,
            caseNumber: child.caseNumber,
            title: child.title,
            status: child.status,
            relationType: 'spawn',
            iAmChild: false,
          });
          logAction(child, 'create', '由 ' + c.caseNumber + ' 衍生');
          logAction(c, 'spawn_child', comment ?? '衍生子流程', { childId: newId });
          recomputeCurrent(child);
          CASES_BY_ID.set(newId, child);
          return respond(201, child);
        }
        if (action === 'reopen') {
          if (c.status !== 'completed') return respond(400, { code: 'INVALID_STATE' });
          const newId = `c-${Date.now()}`;
          const reopened = baseCase({
            id: newId,
            title: payload.title,
            description: payload.description ?? null,
            templateId: payload.templateId ?? c.templateId,
            templateCode: c.templateCode,
            templateName: c.templateName,
            templateVersion: c.templateVersion,
            documentTypeCode: c.documentTypeCode,
            initiatorUserId: currentUser.id,
            initiatorName: currentUser.name,
            initiatedAt: now(),
          });
          reopened.nodes = [
            makeNode('init', '發起', 'start', 'completed', { id: currentUser.id, name: currentUser.name }, now(), now()),
            makeNode('handle', '處理', 'handle', 'in_progress', { id: currentUser.id, name: currentUser.name }, now(), null),
            makeNode('end', '結案', 'end', 'pending', { id: null, name: null }, null, null),
          ];
          reopened.relations.push({
            caseId: c.id,
            caseNumber: c.caseNumber,
            title: c.title,
            status: c.status,
            relationType: 'reopen',
            iAmChild: true,
          });
          c.relations.push({
            caseId: reopened.id,
            caseNumber: reopened.caseNumber,
            title: reopened.title,
            status: reopened.status,
            relationType: 'reopen',
            iAmChild: false,
          });
          logAction(reopened, 'reopen', '由 ' + c.caseNumber + ' 結案後重開');
          recomputeCurrent(reopened);
          CASES_BY_ID.set(newId, reopened);
          return respond(201, reopened);
        }
        return respond(400, { code: 'UNKNOWN_ACTION', action });
      }

      m = matchPath(url, /^\/cases\/([^/]+)\/expected-completion$/);
      if (m && config.method === 'put') {
        const c = findById(m[1]);
        if (!c) return respond(404, { code: 'NOT_FOUND' });
        const payload = config.data as UpdateExpectedCompletionPayload;
        c.expectedCompletionAt = payload.expectedCompletionAt;
        const cur = c.nodes.find((n) => n.status === 'in_progress');
        if (cur) cur.modifiedExpectedAt = payload.expectedCompletionAt;
        logAction(c, 'expected_completion_change', payload.comment ?? null, {
          newExpectedAt: payload.expectedCompletionAt,
        });
        return respond(200, c);
      }

      return respond(404, { code: 'MOCK_NOT_HANDLED', url, method: config.method });
    };
    return config;
  });
  // eslint-disable-next-line no-console
  console.info('[IsoDocs] Mock cases interceptor enabled (VITE_USE_MOCK_CASES=true)');
}
