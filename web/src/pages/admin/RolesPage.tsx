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
} from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import {
  listRoles,
  deactivateRole,
  activateRole,
  type Role,
} from '../../api/roles';
import { useHasPermission } from '../../api/permissionGate';
import RoleEditDrawer, { type RoleEditDrawerMode } from './RoleEditDrawer';

const { Title, Paragraph } = Typography;

export default function RolesPage() {
  const queryClient = useQueryClient();
  const canManage = useHasPermission('roles.manage');

  const [drawerOpen, setDrawerOpen] = useState(false);
  const [drawerMode, setDrawerMode] = useState<RoleEditDrawerMode>('create');
  const [editingRole, setEditingRole] = useState<Role | null>(null);

  const { data: roles, isLoading } = useQuery({
    queryKey: ['roles'],
    queryFn: listRoles,
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => deactivateRole(id),
    onSuccess: () => {
      message.success('角色已停用');
      // 達成驗收條件「權限異動即時生效」：同時 invalidate effective 計算使用的 cache
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
      void queryClient.invalidateQueries({ queryKey: ['roles', 'effective'] });
    },
    onError: (err) => {
      // eslint-disable-next-line no-console
      console.error(err);
      message.error('停用角色失敗');
    },
  });

  const activateMutation = useMutation({
    mutationFn: (id: string) => activateRole(id),
    onSuccess: () => {
      message.success('角色已啟用');
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
      void queryClient.invalidateQueries({ queryKey: ['roles', 'effective'] });
    },
    onError: (err) => {
      // eslint-disable-next-line no-console
      console.error(err);
      message.error('啟用角色失敗');
    },
  });

  const openCreate = () => {
    setDrawerMode('create');
    setEditingRole(null);
    setDrawerOpen(true);
  };

  const openEdit = (role: Role) => {
    setDrawerMode('edit');
    setEditingRole(role);
    setDrawerOpen(true);
  };

  const columns: ColumnsType<Role> = [
    {
      title: '角色名稱',
      dataIndex: 'name',
      key: 'name',
      width: 180,
      render: (name: string, role) => (
        <Space direction="vertical" size={0}>
          <span style={{ fontWeight: 500 }}>{name}</span>
          {!role.isActive && <Tag color="default">已停用</Tag>}
        </Space>
      ),
    },
    {
      title: '說明',
      dataIndex: 'description',
      key: 'description',
      ellipsis: true,
    },
    {
      title: '權限數',
      dataIndex: 'permissions',
      key: 'permissions',
      width: 100,
      align: 'right',
      render: (perms: string[]) => <Tag>{perms.length}</Tag>,
    },
    {
      title: '最後修改',
      dataIndex: 'updatedAt',
      key: 'updatedAt',
      width: 160,
      render: (v: string) => (v ? dayjs(v).format('YYYY-MM-DD HH:mm') : '-'),
    },
    {
      title: '動作',
      key: 'actions',
      width: 200,
      render: (_v, role) => (
        <Space size={4}>
          <Tooltip title={canManage ? '' : '需「角色管理」權限'}>
            <Button type="link" size="small" disabled={!canManage} onClick={() => openEdit(role)}>
              編輯
            </Button>
          </Tooltip>
          {role.isActive ? (
            <Popconfirm
              title="確定要停用這個角色嗎？"
              description="指派了這個角色的使用者會失去其權限。"
              onConfirm={() => deactivateMutation.mutate(role.id)}
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
              onClick={() => activateMutation.mutate(role.id)}
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
            角色與權限
          </Title>
          <Paragraph type="secondary" style={{ margin: 0 }}>
            管理者可自訂角色、設定權限，所有變動隨即生效
          </Paragraph>
        </Space>
      }
      extra={
        <Tooltip title={canManage ? '' : '需「角色管理」權限'}>
          <Button type="primary" disabled={!canManage} onClick={openCreate}>
            建立角色
          </Button>
        </Tooltip>
      }
    >
      <Table<Role>
        rowKey="id"
        columns={columns}
        dataSource={roles ?? []}
        loading={isLoading}
        pagination={false}
        locale={{ emptyText: <Empty description="尚未建立任何角色" /> }}
      />
      <RoleEditDrawer
        open={drawerOpen}
        mode={drawerMode}
        role={editingRole}
        onClose={() => setDrawerOpen(false)}
        onSaved={() => setDrawerOpen(false)}
      />
    </Card>
  );
}
