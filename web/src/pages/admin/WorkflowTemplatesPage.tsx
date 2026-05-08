import { useState } from 'react';
import { Button, Card, Space, Switch, Table, Tag, Typography, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import {
  listWorkflowTemplates,
  deactivateWorkflowTemplate,
  activateWorkflowTemplate,
  type WorkflowTemplate,
} from '../../api/workflowTemplates';
import WorkflowTemplateDesignerDrawer from './WorkflowTemplateDesignerDrawer';
import WorkflowTemplateVersionsModal from './WorkflowTemplateVersionsModal';
import { useHasPermission } from '../../api/permissionGate';

dayjs.extend(relativeTime);

const { Title, Paragraph } = Typography;

/**
 * 流程範本管理頁 (issue #13 [3.2.2])。
 *
 * 驗收條件對應：
 * - 範本清單頁面 → 本頁
 * - 節點拖曳排序介面 → WorkflowTemplateDesignerDrawer
 * - 節點類型與必要角色設定 → WorkflowTemplateDesignerDrawer
 * - 發行新版本確認流程 → WorkflowTemplatePublishConfirmModal（由 designer drawer 觸發）
 * - 顯示版本歷史與 PublishedAt → WorkflowTemplateVersionsModal
 */
export default function WorkflowTemplatesPage() {
  const queryClient = useQueryClient();
  const canManage = useHasPermission('templates.manage');
  const [includeInactive, setIncludeInactive] = useState(true);
  const [designerState, setDesignerState] = useState<
    | { mode: 'create' }
    | { mode: 'edit'; template: WorkflowTemplate }
    | null
  >(null);
  const [versionsTemplate, setVersionsTemplate] = useState<WorkflowTemplate | null>(null);

  const { data: templates = [], isLoading } = useQuery({
    queryKey: ['workflow-templates', { includeInactive }],
    queryFn: () => listWorkflowTemplates(includeInactive),
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => deactivateWorkflowTemplate(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['workflow-templates'] });
      void message.success('已停用範本');
    },
    onError: (err) => {
      void message.error(`停用失敗：${err instanceof Error ? err.message : '未知錯誤'}`);
    },
  });

  const activateMutation = useMutation({
    mutationFn: (id: string) => activateWorkflowTemplate(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['workflow-templates'] });
      void message.success('已重新啟用範本');
    },
    onError: (err) => {
      void message.error(`啟用失敗：${err instanceof Error ? err.message : '未知錯誤'}`);
    },
  });

  const columns: ColumnsType<WorkflowTemplate> = [
    {
      title: '名稱',
      dataIndex: 'name',
      key: 'name',
      render: (name: string, record) => (
        <div>
          <div style={{ fontWeight: 500 }}>{name}</div>
          <div style={{ fontSize: 12, color: 'rgba(0,0,0,0.45)' }}>
            <code>{record.code}</code>
          </div>
        </div>
      ),
    },
    {
      title: '說明',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: '節點數',
      dataIndex: 'nodes',
      key: 'nodes',
      width: 80,
      align: 'center',
      render: (nodes: WorkflowTemplate['nodes']) => nodes.length,
    },
    {
      title: '版本',
      key: 'version',
      width: 110,
      render: (_, record) => {
        if (record.version === 0) {
          return <Tag color="default">草稿</Tag>;
        }
        return (
          <Space size={4}>
            <Tag color="blue">v{record.version}</Tag>
            {record.hasDraftChanges && <Tag color="orange">有未發行變更</Tag>}
          </Space>
        );
      },
    },
    {
      title: '上次發行',
      dataIndex: 'publishedAt',
      key: 'publishedAt',
      width: 180,
      render: (publishedAt: string | null) => {
        if (!publishedAt) {
          return <span style={{ color: 'rgba(0,0,0,0.45)' }}>尚未發行</span>;
        }
        return (
          <div>
            <div>{dayjs(publishedAt).format('YYYY-MM-DD HH:mm')}</div>
            <div style={{ fontSize: 12, color: 'rgba(0,0,0,0.45)' }}>
              {dayjs(publishedAt).fromNow()}
            </div>
          </div>
        );
      },
    },
    {
      title: '狀態',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 90,
      render: (isActive: boolean) =>
        isActive ? <Tag color="green">啟用中</Tag> : <Tag color="default">已停用</Tag>,
    },
    {
      title: '操作',
      key: 'actions',
      width: 280,
      render: (_, record) => (
        <Space size={4}>
          <Button
            size="small"
            type="link"
            disabled={!canManage}
            onClick={() => setDesignerState({ mode: 'edit', template: record })}
          >
            設計
          </Button>
          <Button size="small" type="link" onClick={() => setVersionsTemplate(record)}>
            版本歷史
          </Button>
          {record.isActive ? (
            <Button
              size="small"
              type="link"
              danger
              disabled={!canManage}
              loading={deactivateMutation.isPending}
              onClick={() => deactivateMutation.mutate(record.id)}
            >
              停用
            </Button>
          ) : (
            <Button
              size="small"
              type="link"
              disabled={!canManage}
              loading={activateMutation.isPending}
              onClick={() => activateMutation.mutate(record.id)}
            >
              啟用
            </Button>
          )}
        </Space>
      ),
    },
  ];

  return (
    <Card
      title={
        <Space direction="vertical" size={0}>
          <Title level={4} style={{ margin: 0 }}>
            流程範本
          </Title>
          <Paragraph type="secondary" style={{ margin: 0 }}>
            管理案件可使用的流程範本。範本異動僅套用新案件，進行中案件沿用建立時的 TemplateVersion。
          </Paragraph>
        </Space>
      }
      extra={
        <Space>
          <Switch
            checked={includeInactive}
            onChange={setIncludeInactive}
            checkedChildren="含已停用"
            unCheckedChildren="僅啟用中"
          />
          <Button
            type="primary"
            disabled={!canManage}
            onClick={() => setDesignerState({ mode: 'create' })}
          >
            建立新範本
          </Button>
        </Space>
      }
    >
      <Table
        rowKey="id"
        loading={isLoading}
        columns={columns}
        dataSource={templates}
        pagination={false}
        size="middle"
      />

      {designerState && (
        <WorkflowTemplateDesignerDrawer
          open
          mode={designerState.mode}
          template={designerState.mode === 'edit' ? designerState.template : undefined}
          onClose={() => setDesignerState(null)}
          onSaved={() => {
            setDesignerState(null);
            void queryClient.invalidateQueries({ queryKey: ['workflow-templates'] });
          }}
        />
      )}

      {versionsTemplate && (
        <WorkflowTemplateVersionsModal
          open
          template={versionsTemplate}
          onClose={() => setVersionsTemplate(null)}
        />
      )}
    </Card>
  );
}
