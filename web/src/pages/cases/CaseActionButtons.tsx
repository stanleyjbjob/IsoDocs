import { useEffect, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { App, Button, DatePicker, Form, Input, Modal, Select, Space } from 'antd';
import dayjs from 'dayjs';
import { casesApi } from '../../api/cases';
import type { CaseDetail } from '../../api/cases';
import { workflowTemplatesApi } from '../../api/workflowTemplates';
import { useHasPermission } from '../../api/permissionGate';
import { useAuth } from '../../contexts/AuthContext';

interface Props {
  caseDetail: CaseDetail;
  onChanged: () => void;
}

/**
 * 案件動作按鈕區（issue #21 [5.5] 驗收條件 3 「接單/指派/核准/退回/作廢」 × 「修改預計完成時間」）。
 * 按鈕顯示條件同時受 RBAC 與案件內部狀態限制（驗收條件 5）。
 */
export default function CaseActionButtons({ caseDetail: c, onChanged }: Props) {
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const { user } = useAuth();
  const myUserId = user?.id ?? null;

  const canManage = useHasPermission('cases.manage');
  const canVoid = useHasPermission('cases.void');
  const canSpawn = useHasPermission('cases.spawn_child');
  const canReopen = useHasPermission('cases.reopen');

  const [assignOpen, setAssignOpen] = useState(false);
  const [rejectOpen, setRejectOpen] = useState(false);
  const [voidOpen, setVoidOpen] = useState(false);
  const [spawnOpen, setSpawnOpen] = useState(false);
  const [reopenOpen, setReopenOpen] = useState(false);
  const [updateExpectedOpen, setUpdateExpectedOpen] = useState(false);

  const refresh = () => {
    queryClient.invalidateQueries({ queryKey: ['cases'] });
    onChanged();
  };

  const acceptMutation = useMutation({
    mutationFn: () => casesApi.accept(c.id, {}),
    onSuccess: refresh,
  });
  const replyCloseMutation = useMutation({
    mutationFn: () => casesApi.replyClose(c.id, {}),
    onSuccess: refresh,
  });
  const approveMutation = useMutation({
    mutationFn: () => casesApi.approve(c.id, {}),
    onSuccess: refresh,
  });

  const isInProgress = c.status === 'in_progress';
  const currentNode = c.nodes.find((n) => n.status === 'in_progress');
  const iAmCurrentAssignee = currentNode?.assigneeUserId === myUserId;
  const currentIsApprove = currentNode?.nodeType === 'approve';
  const currentIsHandle = currentNode?.nodeType === 'handle';

  return (
    <Space wrap>
      {/* 接單：有被指派並且是自己 + 處理節點 + 進行中 */}
      {isInProgress && currentIsHandle && iAmCurrentAssignee && (
        <Button
          type="primary"
          loading={acceptMutation.isPending}
          onClick={() => {
            acceptMutation.mutate(undefined, {
              onSuccess: () => message.success('已接單'),
            });
          }}
        >
          接單
        </Button>
      )}

      {/* 回覆結案 (路徑 A)：處理節點 + 是承辦人 */}
      {isInProgress && currentIsHandle && iAmCurrentAssignee && (
        <Button
          loading={replyCloseMutation.isPending}
          onClick={() => {
            replyCloseMutation.mutate(undefined, {
              onSuccess: () => message.success('已回覆結案'),
            });
          }}
        >
          回覆結案
        </Button>
      )}

      {/* 核准：核准節點 + 是承辦人 */}
      {isInProgress && currentIsApprove && iAmCurrentAssignee && (
        <Button
          type="primary"
          loading={approveMutation.isPending}
          onClick={() => {
            approveMutation.mutate(undefined, {
              onSuccess: () => message.success('已核准'),
            });
          }}
        >
          核准
        </Button>
      )}

      {/* 退回 */}
      {isInProgress && iAmCurrentAssignee && (
        <Button danger onClick={() => setRejectOpen(true)}>
          退回至前一處理節點
        </Button>
      )}

      {/* 指派 (管理者) */}
      {isInProgress && canManage && (
        <Button onClick={() => setAssignOpen(true)}>指派</Button>
      )}

      {/* 修改預計完成時間 (issue #20) */}
      {isInProgress && (iAmCurrentAssignee || c.initiatorUserId === myUserId || canManage) && (
        <Button onClick={() => setUpdateExpectedOpen(true)}>修改預計完成時間</Button>
      )}

      {/* 衍生子流程 (issue #17) */}
      {isInProgress && canSpawn && (
        <Button onClick={() => setSpawnOpen(true)}>衍生子流程</Button>
      )}

      {/* 作廢 (issue #18) */}
      {isInProgress && canVoid && (
        <Button danger onClick={() => setVoidOpen(true)}>作廢</Button>
      )}

      {/* 重開新案 (issue #32 [5.3.4]) */}
      {c.status === 'completed' && canReopen && (
        <Button type="dashed" onClick={() => setReopenOpen(true)}>重開新案</Button>
      )}

      <AssignModal open={assignOpen} caseId={c.id} onClose={() => setAssignOpen(false)} onChanged={refresh} />
      <CommentModal
        open={rejectOpen}
        title="退回至前一處理節點"
        confirmText="確認退回"
        danger
        onClose={() => setRejectOpen(false)}
        onConfirm={async (comment) => {
          await casesApi.reject(c.id, { comment });
          refresh();
          message.success('已退回');
        }}
      />
      <CommentModal
        open={voidOpen}
        title="作廢案件"
        confirmText="確認作廢"
        danger
        warning="主案作廢會連鎖作廢所有未完成的子流程 (issue #18)。本動作不可逆。"
        onClose={() => setVoidOpen(false)}
        onConfirm={async (comment) => {
          await casesApi.voidCase(c.id, { comment });
          refresh();
          message.success('已作廢');
        }}
      />
      <SpawnChildModal open={spawnOpen} caseId={c.id} onClose={() => setSpawnOpen(false)} onChanged={refresh} />
      <ReopenModal open={reopenOpen} caseId={c.id} parentTitle={c.title} onClose={() => setReopenOpen(false)} onChanged={refresh} />
      <UpdateExpectedModal
        open={updateExpectedOpen}
        caseId={c.id}
        currentExpected={c.expectedCompletionAt}
        onClose={() => setUpdateExpectedOpen(false)}
        onChanged={refresh}
      />
    </Space>
  );
}

// ----- Modals -------------------------------------------------------------

function AssignModal({
  open,
  caseId,
  onClose,
  onChanged,
}: {
  open: boolean;
  caseId: string;
  onClose: () => void;
  onChanged: () => void;
}) {
  const [form] = Form.useForm<{ userId: string; comment?: string }>();
  const { message } = App.useApp();
  const mutation = useMutation({
    mutationFn: (v: { userId: string; comment?: string }) => casesApi.assign(caseId, v),
    onSuccess: () => {
      message.success('已指派');
      onChanged();
      onClose();
      form.resetFields();
    },
  });
  return (
    <Modal
      open={open}
      title="指派承辦人"
      okText="確認指派"
      cancelText="取消"
      onCancel={onClose}
      onOk={async () => {
        const v = await form.validateFields();
        mutation.mutate(v);
      }}
      confirmLoading={mutation.isPending}
    >
      <Form form={form} layout="vertical">
        <Form.Item label="使用者 ID" name="userId" rules={[{ required: true }]}>
          <Input placeholder="u-xxx；後續會改為使用者 Select" />
        </Form.Item>
        <Form.Item label="註記" name="comment">
          <Input.TextArea rows={3} />
        </Form.Item>
      </Form>
    </Modal>
  );
}

function CommentModal({
  open,
  title,
  confirmText,
  danger,
  warning,
  onClose,
  onConfirm,
}: {
  open: boolean;
  title: string;
  confirmText: string;
  danger?: boolean;
  warning?: string;
  onClose: () => void;
  onConfirm: (comment: string) => Promise<void>;
}) {
  const [form] = Form.useForm<{ comment?: string }>();
  const [submitting, setSubmitting] = useState(false);
  return (
    <Modal
      open={open}
      title={title}
      okText={confirmText}
      okButtonProps={{ danger }}
      cancelText="取消"
      onCancel={onClose}
      onOk={async () => {
        const v = await form.validateFields();
        setSubmitting(true);
        try {
          await onConfirm(v.comment ?? '');
          form.resetFields();
          onClose();
        } finally {
          setSubmitting(false);
        }
      }}
      confirmLoading={submitting}
    >
      {warning && <div style={{ color: '#a8071a', marginBottom: 12 }}>{warning}</div>}
      <Form form={form} layout="vertical">
        <Form.Item label="註記 / 原因" name="comment" rules={[{ max: 500 }]}>
          <Input.TextArea rows={4} />
        </Form.Item>
      </Form>
    </Modal>
  );
}

function SpawnChildModal({
  open,
  caseId,
  onClose,
  onChanged,
}: {
  open: boolean;
  caseId: string;
  onClose: () => void;
  onChanged: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm<{ templateId: string; title: string; description?: string }>();
  const queryClient = useQueryClient();
  const cachedTemplates = queryClient.getQueryData<{
    items: Array<{ id: string; name: string; code: string }>;
  }>(['workflow-templates', 'list']);
  const fetchTemplates = useMutation({
    mutationFn: () => workflowTemplatesApi.list(),
    onSuccess: (data) => queryClient.setQueryData(['workflow-templates', 'list'], data),
  });
  // 用 useEffect 取代 antd Modal afterOpenChange（在較舊的 antd v5 不一定存在）
  useEffect(() => {
    if (open && !cachedTemplates && !fetchTemplates.isPending) {
      fetchTemplates.mutate();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  const mutation = useMutation({
    mutationFn: (v: { templateId: string; title: string; description?: string }) =>
      casesApi.spawnChild(caseId, v),
    onSuccess: () => {
      message.success('已衍生子流程');
      onChanged();
      onClose();
      form.resetFields();
    },
  });

  return (
    <Modal
      open={open}
      title="衍生子流程"
      okText="建立子流程"
      cancelText="取消"
      onCancel={onClose}
      onOk={async () => {
        const v = await form.validateFields();
        mutation.mutate(v);
      }}
      confirmLoading={mutation.isPending}
      width={560}
    >
      <Form form={form} layout="vertical">
        <Form.Item label="子流程範本" name="templateId" rules={[{ required: true }]}>
          <Select
            placeholder="選擇子流程要使用的範本"
            options={(cachedTemplates?.items ?? []).map((t) => ({ value: t.id, label: t.name }))}
            loading={fetchTemplates.isPending}
          />
        </Form.Item>
        <Form.Item label="子流程標題" name="title" rules={[{ required: true }]}>
          <Input />
        </Form.Item>
        <Form.Item label="描述" name="description">
          <Input.TextArea rows={3} />
        </Form.Item>
        <div style={{ color: '#999', fontSize: 12 }}>
          依 issue #19 [5.3.2]，可繼承主案指定欄位；本 MVP 未提供指定選項 UI，後續補齊。
        </div>
      </Form>
    </Modal>
  );
}

function ReopenModal({
  open,
  caseId,
  parentTitle,
  onClose,
  onChanged,
}: {
  open: boolean;
  caseId: string;
  parentTitle: string;
  onClose: () => void;
  onChanged: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm<{ title: string; description?: string }>();
  const mutation = useMutation({
    mutationFn: (v: { title: string; description?: string }) => casesApi.reopen(caseId, v),
    onSuccess: () => {
      message.success('已重開新案');
      onChanged();
      onClose();
      form.resetFields();
    },
  });
  return (
    <Modal
      open={open}
      title="結案後重開新案 (issue #32 [5.3.4])"
      okText="重開新案"
      cancelText="取消"
      onCancel={onClose}
      onOk={async () => {
        const v = await form.validateFields();
        mutation.mutate(v);
      }}
      confirmLoading={mutation.isPending}
      width={560}
    >
      <div style={{ marginBottom: 12, color: '#999' }}>原案：{parentTitle}</div>
      <Form form={form} layout="vertical" initialValues={{ title: `重開：${parentTitle}` }}>
        <Form.Item label="新案標題" name="title" rules={[{ required: true }]}>
          <Input />
        </Form.Item>
        <Form.Item label="重開原因 / 描述" name="description">
          <Input.TextArea rows={3} />
        </Form.Item>
      </Form>
    </Modal>
  );
}

function UpdateExpectedModal({
  open,
  caseId,
  currentExpected,
  onClose,
  onChanged,
}: {
  open: boolean;
  caseId: string;
  currentExpected: string | null;
  onClose: () => void;
  onChanged: () => void;
}) {
  const { message } = App.useApp();
  const [form] = Form.useForm<{ expectedCompletionAt: dayjs.Dayjs; comment?: string }>();
  const mutation = useMutation({
    mutationFn: (v: { expectedCompletionAt: string; comment?: string }) =>
      casesApi.updateExpectedCompletion(caseId, v),
    onSuccess: () => {
      message.success('已修改預計完成時間');
      onChanged();
      onClose();
    },
  });
  return (
    <Modal
      open={open}
      title="修改預計完成時間"
      okText="確認修改"
      cancelText="取消"
      onCancel={onClose}
      onOk={async () => {
        const v = await form.validateFields();
        mutation.mutate({
          expectedCompletionAt: v.expectedCompletionAt.toISOString(),
          comment: v.comment,
        });
      }}
      confirmLoading={mutation.isPending}
      width={520}
    >
      <div style={{ marginBottom: 12, color: '#999', fontSize: 12 }}>
        OriginalExpectedAt 以首次設定為準、不被覆寫；本次修改會寫入當前節點的 ModifiedExpectedAt (issue #20)。
      </div>
      <Form
        form={form}
        layout="vertical"
        initialValues={{
          expectedCompletionAt: currentExpected ? dayjs(currentExpected) : undefined,
        }}
      >
        <Form.Item label="新預計完成時間" name="expectedCompletionAt" rules={[{ required: true }]}>
          <DatePicker showTime style={{ width: '100%' }} />
        </Form.Item>
        <Form.Item label="修改原因" name="comment" rules={[{ max: 500 }]}>
          <Input.TextArea rows={3} placeholder="記錄為何修改，方便後續查閱" />
        </Form.Item>
      </Form>
    </Modal>
  );
}
