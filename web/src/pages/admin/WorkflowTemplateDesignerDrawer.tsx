import { useEffect, useMemo, useState } from 'react';
import {
  Alert,
  Button,
  Drawer,
  Form,
  Input,
  InputNumber,
  Popconfirm,
  Select,
  Space,
  Table,
  Tag,
  Tooltip,
  Typography,
  message,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { ArrowDownOutlined, ArrowUpOutlined, DeleteOutlined, HolderOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  createWorkflowTemplate,
  updateWorkflowTemplate,
  type WorkflowNode,
  type WorkflowTemplate,
} from '../../api/workflowTemplates';
import { listRoles, type Role } from '../../api/roles';
import {
  NODE_TYPES,
  getNodeTypeMeta,
  validateNodes,
  type WorkflowNodeType,
} from '../../lib/workflowNodeTypes';
import WorkflowTemplatePublishConfirmModal from './WorkflowTemplatePublishConfirmModal';

const { Title, Paragraph } = Typography;

interface DraftNode extends WorkflowNode {
  /** UI 內部用的連續性 ID（刷新不變） */
  uiId: string;
}

function newUiId(): string {
  return `n-${Math.random().toString(36).slice(2, 10)}`;
}

function toDraftNodes(nodes: WorkflowNode[]): DraftNode[] {
  return nodes
    .slice()
    .sort((a, b) => a.nodeOrder - b.nodeOrder)
    .map((n) => ({ ...n, uiId: newUiId() }));
}

function renumber(nodes: DraftNode[]): DraftNode[] {
  return nodes.map((n, i) => ({ ...n, nodeOrder: i + 1 }));
}

/** 把 DraftNode 轉回 API 可接受的 WorkflowNode（明確拋掉 uiId，避免 noUnusedLocals 旗標）。 */
function toApiNode(n: DraftNode): WorkflowNode {
  return {
    nodeOrder: n.nodeOrder,
    nodeKey: n.nodeKey,
    label: n.label,
    nodeType: n.nodeType,
    requiredRoleId: n.requiredRoleId,
    description: n.description,
    expectedHours: n.expectedHours,
  };
}

export interface WorkflowTemplateDesignerDrawerProps {
  open: boolean;
  mode: 'create' | 'edit';
  template?: WorkflowTemplate;
  onClose: () => void;
  onSaved: () => void;
}

export default function WorkflowTemplateDesignerDrawer({
  open,
  mode,
  template,
  onClose,
  onSaved,
}: WorkflowTemplateDesignerDrawerProps) {
  const queryClient = useQueryClient();
  const [form] = Form.useForm<{
    code: string;
    name: string;
    description?: string;
  }>();
  const [nodes, setNodes] = useState<DraftNode[]>([]);
  const [draggingUiId, setDraggingUiId] = useState<string | null>(null);
  const [publishModalOpen, setPublishModalOpen] = useState(false);

  // 取角色清單（給 requiredRoleId Select 用）
  const { data: roles = [] } = useQuery({
    queryKey: ['roles'],
    queryFn: () => listRoles(),
    enabled: open,
  });

  // 入口 / 退出時重置 form
  useEffect(() => {
    if (!open) return;
    if (mode === 'edit' && template) {
      form.setFieldsValue({
        code: template.code,
        name: template.name,
        description: template.description,
      });
      setNodes(toDraftNodes(template.nodes));
    } else {
      form.resetFields();
      setNodes([
        { uiId: newUiId(), nodeOrder: 1, nodeKey: 'start', label: '起始', nodeType: 'start' },
        { uiId: newUiId(), nodeOrder: 2, nodeKey: 'end', label: '結束', nodeType: 'end' },
      ]);
    }
  }, [open, mode, template, form]);

  const validationIssues = useMemo(() => validateNodes(nodes), [nodes]);

  const moveUp = (uiId: string) => {
    setNodes((prev) => {
      const idx = prev.findIndex((n) => n.uiId === uiId);
      if (idx <= 0) return prev;
      const next = prev.slice();
      [next[idx - 1], next[idx]] = [next[idx], next[idx - 1]];
      return renumber(next);
    });
  };

  const moveDown = (uiId: string) => {
    setNodes((prev) => {
      const idx = prev.findIndex((n) => n.uiId === uiId);
      if (idx === -1 || idx >= prev.length - 1) return prev;
      const next = prev.slice();
      [next[idx], next[idx + 1]] = [next[idx + 1], next[idx]];
      return renumber(next);
    });
  };

  const removeNode = (uiId: string) => {
    setNodes((prev) => renumber(prev.filter((n) => n.uiId !== uiId)));
  };

  const addNode = () => {
    setNodes((prev) => {
      // 插入在倒數第 2 個位置，避免插到 start/end 之外
      const insertIdx = Math.max(prev.length - 1, 1);
      const next = prev.slice();
      next.splice(insertIdx, 0, {
        uiId: newUiId(),
        nodeOrder: 0,
        nodeKey: `node-${Math.random().toString(36).slice(2, 6)}`,
        label: '新節點',
        nodeType: 'handle',
      });
      return renumber(next);
    });
  };

  const patchNode = (uiId: string, patch: Partial<WorkflowNode>) => {
    setNodes((prev) =>
      prev.map((n) => (n.uiId === uiId ? { ...n, ...patch } : n)),
    );
  };

  // HTML5 native drag-drop
  const onDragStart = (uiId: string) => {
    setDraggingUiId(uiId);
  };
  const onDragOver = (e: React.DragEvent<HTMLTableRowElement>) => {
    e.preventDefault();
  };
  const onDrop = (targetUiId: string) => {
    setNodes((prev) => {
      if (!draggingUiId || draggingUiId === targetUiId) return prev;
      const fromIdx = prev.findIndex((n) => n.uiId === draggingUiId);
      const toIdx = prev.findIndex((n) => n.uiId === targetUiId);
      if (fromIdx === -1 || toIdx === -1) return prev;
      const next = prev.slice();
      const [moved] = next.splice(fromIdx, 1);
      next.splice(toIdx, 0, moved);
      return renumber(next);
    });
    setDraggingUiId(null);
  };

  const saveDraftMutation = useMutation({
    mutationFn: async () => {
      const values = await form.validateFields();
      const payloadNodes: WorkflowNode[] = nodes.map(toApiNode);
      if (mode === 'create') {
        return createWorkflowTemplate({
          code: values.code,
          name: values.name,
          description: values.description,
          nodes: payloadNodes,
        });
      }
      return updateWorkflowTemplate(template!.id, {
        name: values.name,
        description: values.description,
        nodes: payloadNodes,
      });
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['workflow-templates'] });
      void message.success(mode === 'create' ? '已建立草稿' : '已儲存草稿');
      onSaved();
    },
    onError: (err) => {
      void message.error(`儲存失敗：${err instanceof Error ? err.message : '未知錯誤'}`);
    },
  });

  const onPublishClicked = async () => {
    if (mode === 'create') {
      void message.warning('請先「儲存草稿」建立範本，再發行首個版本');
      return;
    }
    if (validationIssues.length > 0) {
      void message.warning('節點設定仍有驗證問題，請先修正再發行');
      return;
    }
    // 先存草稿再開發行 modal（避免未儲存的變更不進新版本）
    try {
      await form.validateFields();
      await saveDraftMutation.mutateAsync();
      setPublishModalOpen(true);
    } catch {
      void message.error('請先修正表單錯誤');
    }
  };

  const columns: ColumnsType<DraftNode> = [
    {
      title: '',
      key: 'drag',
      width: 32,
      render: () => (
        <Tooltip title="拖曳以重新排序">
          <HolderOutlined style={{ cursor: 'grab', color: 'rgba(0,0,0,0.45)' }} />
        </Tooltip>
      ),
    },
    {
      title: '#',
      dataIndex: 'nodeOrder',
      key: 'nodeOrder',
      width: 50,
      align: 'center',
      render: (n: number) => <Tag>{n}</Tag>,
    },
    {
      title: 'nodeKey',
      key: 'nodeKey',
      width: 160,
      render: (_: unknown, record) => (
        <Input
          size="small"
          value={record.nodeKey}
          onChange={(e) => patchNode(record.uiId, { nodeKey: e.target.value })}
          placeholder="e.g. pm-confirm"
        />
      ),
    },
    {
      title: '顯示名稱',
      key: 'label',
      render: (_: unknown, record) => (
        <Input
          size="small"
          value={record.label}
          onChange={(e) => patchNode(record.uiId, { label: e.target.value })}
          placeholder="例如：PM 確認需求"
        />
      ),
    },
    {
      title: '類型',
      key: 'nodeType',
      width: 130,
      render: (_: unknown, record) => (
        <Select<WorkflowNodeType>
          size="small"
          value={record.nodeType}
          style={{ width: '100%' }}
          onChange={(v) => patchNode(record.uiId, { nodeType: v })}
          options={NODE_TYPES.map((t) => ({
            value: t.type,
            label: <Tag color={t.color}>{t.label}</Tag>,
          }))}
        />
      ),
    },
    {
      title: '必要角色',
      key: 'requiredRoleId',
      width: 180,
      render: (_: unknown, record) => {
        const meta = getNodeTypeMeta(record.nodeType);
        const isDisabled = !meta || (meta.type === 'start' || meta.type === 'end');
        return (
          <Select<string | undefined>
            size="small"
            value={record.requiredRoleId}
            allowClear
            disabled={isDisabled}
            placeholder={isDisabled ? '不需指派' : '請選擇角色'}
            style={{ width: '100%' }}
            onChange={(v) => patchNode(record.uiId, { requiredRoleId: v })}
            options={roles
              .filter((r: Role) => r.isActive)
              .map((r: Role) => ({ value: r.id, label: r.name }))}
          />
        );
      },
    },
    {
      title: '預計時間(時)',
      key: 'expectedHours',
      width: 110,
      render: (_: unknown, record) => (
        <InputNumber<number>
          size="small"
          min={0}
          step={1}
          value={record.expectedHours}
          onChange={(v) => patchNode(record.uiId, { expectedHours: v ?? undefined })}
          style={{ width: '100%' }}
        />
      ),
    },
    {
      title: '操作',
      key: 'actions',
      width: 130,
      render: (_: unknown, record, idx) => (
        <Space size={0}>
          <Tooltip title="上移">
            <Button
              size="small"
              type="text"
              icon={<ArrowUpOutlined />}
              disabled={idx === 0}
              onClick={() => moveUp(record.uiId)}
            />
          </Tooltip>
          <Tooltip title="下移">
            <Button
              size="small"
              type="text"
              icon={<ArrowDownOutlined />}
              disabled={idx === nodes.length - 1}
              onClick={() => moveDown(record.uiId)}
            />
          </Tooltip>
          <Popconfirm
            title="刪除此節點？"
            okText="刪除"
            cancelText="取消"
            onConfirm={() => removeNode(record.uiId)}
          >
            <Button size="small" type="text" danger icon={<DeleteOutlined />} />
          </Popconfirm>
        </Space>
      ),
    },
  ];

  return (
    <>
      <Drawer
        open={open}
        onClose={onClose}
        title={mode === 'create' ? '建立新範本' : `設計範本：${template?.name ?? ''}`}
        width={960}
        destroyOnClose
        extra={
          <Space>
            <Button onClick={onClose}>取消</Button>
            <Button
              type="default"
              loading={saveDraftMutation.isPending}
              onClick={() => saveDraftMutation.mutate()}
            >
              儲存草稿
            </Button>
            <Button
              type="primary"
              disabled={validationIssues.length > 0 || mode === 'create'}
              onClick={onPublishClicked}
            >
              儲存並發行新版本
            </Button>
          </Space>
        }
      >
        <Form form={form} layout="vertical">
          <Title level={5} style={{ marginTop: 0 }}>基本資訊</Title>
          <Form.Item
            label="範本 code"
            name="code"
            rules={[
              { required: true, message: '請輸入 code' },
              { pattern: /^[a-z][a-z0-9_]*$/, message: '只能含小寫、數字、底線，且以字母開頭' },
            ]}
            tooltip="Machine-readable 識別碼，一旦建立不可變更"
          >
            <Input disabled={mode === 'edit'} placeholder="e.g. work_request" />
          </Form.Item>
          <Form.Item
            label="顯示名稱"
            name="name"
            rules={[{ required: true, message: '請輸入名稱' }]}
          >
            <Input maxLength={100} placeholder="例如：工作需求單" />
          </Form.Item>
          <Form.Item label="說明" name="description">
            <Input.TextArea maxLength={500} rows={2} placeholder="這個範本適用的情境說明" />
          </Form.Item>
        </Form>

        <div style={{ marginTop: 24 }}>
          <Space style={{ marginBottom: 12 }} align="baseline">
            <Title level={5} style={{ margin: 0 }}>節點設計</Title>
            <Paragraph type="secondary" style={{ margin: 0 }}>
              拖曳右側圖示或使用上下移動按鈕以調整順序。類型為 handle / approve 時需指派必要角色。
            </Paragraph>
          </Space>

          {validationIssues.length > 0 && (
            <Alert
              type="warning"
              showIcon
              style={{ marginBottom: 12 }}
              message="節點設定需要修正"
              description={
                <ul style={{ paddingInlineStart: 18, marginBottom: 0 }}>
                  {validationIssues.map((iss, i) => (
                    <li key={i}>
                      {iss.nodeOrder !== null && `(#${iss.nodeOrder}) `}
                      {iss.message}
                    </li>
                  ))}
                </ul>
              }
            />
          )}

          <Table<DraftNode>
            rowKey="uiId"
            dataSource={nodes}
            columns={columns}
            pagination={false}
            size="small"
            onRow={(record) => ({
              draggable: true,
              onDragStart: () => onDragStart(record.uiId),
              onDragOver,
              onDrop: () => onDrop(record.uiId),
              style: {
                cursor: 'grab',
                opacity: draggingUiId === record.uiId ? 0.4 : 1,
              },
            })}
          />

          <div style={{ marginTop: 12 }}>
            <Button onClick={addNode}>+ 新增節點</Button>
          </div>
        </div>
      </Drawer>

      {publishModalOpen && template && (
        <WorkflowTemplatePublishConfirmModal
          open
          template={template}
          onClose={() => setPublishModalOpen(false)}
          onPublished={() => {
            setPublishModalOpen(false);
            void queryClient.invalidateQueries({ queryKey: ['workflow-templates'] });
            onSaved();
          }}
        />
      )}
    </>
  );
}
