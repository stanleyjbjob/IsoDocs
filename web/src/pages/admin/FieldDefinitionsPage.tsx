import { useState } from 'react';
import {
  Card,
  Table,
  Button,
  Space,
  Tag,
  Typography,
  Popconfirm,
  message,
  Tooltip,
  Empty,
  Switch,
} from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import {
  listFieldDefinitions,
  deactivateFieldDefinition,
  activateFieldDefinition,
  type FieldDefinition,
} from '../../api/fieldDefinitions';
import { findFieldType } from '../../lib/fieldTypes';
import { useHasPermission } from '../../api/permissionGate';
import FieldDefinitionEditDrawer, {
  type FieldDefinitionEditDrawerMode,
} from './FieldDefinitionEditDrawer';
import FieldDefinitionVersionsModal from './FieldDefinitionVersionsModal';

const { Title, Paragraph } = Typography;

/**
 * 自訂欄位管理頁面（issue #11 [3.1.2]）。
 *
 * 驗收條件對照：
 * - 欄位清單頁面 → 本元件
 * - 欄位新增/編輯表單 → `FieldDefinitionEditDrawer`
 * - 支援多種欄位類型選擇 → drawer 內 select 列舉 `FIELD_TYPES` catalog
 * - 顯示欄位版本歷史 → `FieldDefinitionVersionsModal`
 * - 異動時顯示警告說明 → drawer edit mode 顯示警告 Alert
 * - 操作權限受 RBAC 控管 → `useHasPermission('fields.manage')`
 */
export default function FieldDefinitionsPage() {
  const queryClient = useQueryClient();
  const canManage = useHasPermission('fields.manage');

  const [includeInactive, setIncludeInactive] = useState(true);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [drawerMode, setDrawerMode] =
    useState<FieldDefinitionEditDrawerMode>('create');
  const [editingField, setEditingField] = useState<FieldDefinition | null>(null);
  const [versionsField, setVersionsField] = useState<FieldDefinition | null>(null);

  const { data: fields, isLoading } = useQuery({
    queryKey: ['field-definitions', { includeInactive }],
    queryFn: () => listFieldDefinitions(includeInactive),
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => deactivateFieldDefinition(id, '透過清單頁停用'),
    onSuccess: () => {
      message.success('欄位已停用，新案件將不再使用');
      void queryClient.invalidateQueries({ queryKey: ['field-definitions'] });
    },
    onError: (err) => {
      // eslint-disable-next-line no-console
      console.error(err);
      message.error('停用欄位失敗');
    },
  });

  const activateMutation = useMutation({
    mutationFn: (id: string) => activateFieldDefinition(id, '透過清單頁啟用'),
    onSuccess: () => {
      message.success('欄位已啟用');
      void queryClient.invalidateQueries({ queryKey: ['field-definitions'] });
    },
    onError: (err) => {
      // eslint-disable-next-line no-console
      console.error(err);
      message.error('啟用欄位失敗');
    },
  });

  const openCreate = () => {
    setDrawerMode('create');
    setEditingField(null);
    setDrawerOpen(true);
  };

  const openEdit = (field: FieldDefinition) => {
    setDrawerMode('edit');
    setEditingField(field);
    setDrawerOpen(true);
  };

  const columns: ColumnsType<FieldDefinition> = [
    {
      title: '欄位名稱',
      dataIndex: 'label',
      key: 'label',
      width: 200,
      render: (label: string, field) => (
        <Space direction="vertical" size={0}>
          <span style={{ fontWeight: 500 }}>{label}</span>
          {!field.isActive && <Tag color="default">已停用</Tag>}
        </Space>
      ),
    },
    {
      title: '識別碼',
      dataIndex: 'code',
      key: 'code',
      width: 220,
      render: (code: string) => <code style={{ fontSize: 12 }}>{code}</code>,
    },
    {
      title: '類型',
      dataIndex: 'fieldType',
      key: 'fieldType',
      width: 110,
      render: (t: string) => {
        const def = findFieldType(t);
        return <Tag color="blue">{def?.label ?? t}</Tag>;
      },
    },
    {
      title: '必填',
      dataIndex: 'isRequired',
      key: 'isRequired',
      width: 70,
      align: 'center',
      render: (v: boolean) => (v ? <Tag color="red">必填</Tag> : <Tag>選填</Tag>),
    },
    {
      title: '版本',
      dataIndex: 'version',
      key: 'version',
      width: 80,
      align: 'right',
      render: (v: number, field) => (
        <Button
          type="link"
          size="small"
          onClick={() => setVersionsField(field)}
          style={{ padding: 0 }}
        >
          v{v}
        </Button>
      ),
    },
    {
      title: '最後修改',
      dataIndex: 'updatedAt',
      key: 'updatedAt',
      width: 160,
      render: (v: string) => (v ? dayjs(v).format('YYYY-MM-DD HH:mm') : '-'),
    },
    {
      title: '說明',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: '動作',
      key: 'actions',
      width: 240,
      fixed: 'right',
      render: (_v, field) => (
        <Space size={4}>
          <Tooltip title={canManage ? '' : '需「自訂欄位管理」權限'}>
            <Button
              type="link"
              size="small"
              disabled={!canManage}
              onClick={() => openEdit(field)}
            >
              編輯
            </Button>
          </Tooltip>
          <Button type="link" size="small" onClick={() => setVersionsField(field)}>
            版本歷史
          </Button>
          {field.isActive ? (
            <Popconfirm
              title="確定要停用這個欄位嗎？"
              description="進行中與歷史案件不受影響，但新案件將不再使用此欄位。"
              onConfirm={() => deactivateMutation.mutate(field.id)}
              okButtonProps={{ loading: deactivateMutation.isPending }}
              disabled={!canManage}
            >
              <Button type="link" size="small" danger disabled={!canManage}>
                停用
              </Button>
            </Popconfirm>
          ) : (
            <Button
              type="link"
              size="small"
              disabled={!canManage}
              loading={activateMutation.isPending}
              onClick={() => activateMutation.mutate(field.id)}
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
            自訂欄位
          </Title>
          <Paragraph type="secondary" style={{ margin: 0 }}>
            管理者可建立、編輯、停用案件自訂欄位。修改會自動建立新版本快照，不影響既有案件紀錄。
          </Paragraph>
        </Space>
      }
      extra={
        <Space>
          <Switch
            checkedChildren="含已停用"
            unCheckedChildren="僅顯示啟用中"
            checked={includeInactive}
            onChange={setIncludeInactive}
          />
          <Tooltip title={canManage ? '' : '需「自訂欄位管理」權限'}>
            <Button type="primary" disabled={!canManage} onClick={openCreate}>
              建立欄位
            </Button>
          </Tooltip>
        </Space>
      }
    >
      <Table<FieldDefinition>
        rowKey="id"
        columns={columns}
        dataSource={fields ?? []}
        loading={isLoading}
        pagination={false}
        scroll={{ x: 1200 }}
        locale={{ emptyText: <Empty description="尚未建立任何自訂欄位" /> }}
      />
      <FieldDefinitionEditDrawer
        open={drawerOpen}
        mode={drawerMode}
        field={editingField}
        onClose={() => setDrawerOpen(false)}
        onSaved={() => setDrawerOpen(false)}
      />
      <FieldDefinitionVersionsModal
        open={versionsField !== null}
        field={versionsField}
        onClose={() => setVersionsField(null)}
      />
    </Card>
  );
}
