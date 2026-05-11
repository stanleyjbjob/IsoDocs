import { useState } from 'react';
import { Table, Button, Tag, Popconfirm, Space, message } from 'antd';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { rolesApi, type Role } from '../../api/roles';
import { useHasPermission } from '../../api/permissionGate';
import RoleEditDrawer from './RoleEditDrawer';

export default function RolesPage() {
  const [editRole, setEditRole] = useState<Role | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const queryClient = useQueryClient();
  const canManage = useHasPermission('roles.manage');

  const { data: roles = [], isLoading } = useQuery({
    queryKey: ['roles'],
    queryFn: rolesApi.list,
  });

  const deactivateMutation = useMutation({
    mutationFn: (id: string) => rolesApi.deactivate(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
      void queryClient.invalidateQueries({ queryKey: ['roles', 'effective'] });
      void message.success('角色已停用');
    },
  });

  const activateMutation = useMutation({
    mutationFn: (id: string) => rolesApi.activate(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
      void queryClient.invalidateQueries({ queryKey: ['roles', 'effective'] });
      void message.success('角色已啟用');
    },
  });

  const columns: ColumnsType<Role> = [
    { title: '角色名稱', dataIndex: 'name', key: 'name', width: 140 },
    { title: '描述', dataIndex: 'description', key: 'description' },
    {
      title: '權限數',
      key: 'permCount',
      width: 80,
      render: (_: unknown, r: Role) => r.permissions.length,
    },
    {
      title: '狀態',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 80,
      render: (isActive: boolean) => (
        <Tag color={isActive ? 'green' : 'default'}>{isActive ? '啟用' : '停用'}</Tag>
      ),
    },
    {
      title: '更新時間',
      dataIndex: 'updatedAt',
      key: 'updatedAt',
      width: 120,
      render: (v: string) => new Date(v).toLocaleDateString('zh-TW'),
    },
    {
      title: '操作',
      key: 'actions',
      width: 160,
      render: (_: unknown, r: Role) =>
        canManage ? (
          <Space>
            <Button
              size="small"
              onClick={() => {
                setEditRole(r);
                setDrawerOpen(true);
              }}
            >
              編輯
            </Button>
            {r.isActive ? (
              <Popconfirm
                title="確定停用此角色？"
                onConfirm={() => deactivateMutation.mutate(r.id)}
              >
                <Button size="small" danger>
                  停用
                </Button>
              </Popconfirm>
            ) : (
              <Button size="small" onClick={() => activateMutation.mutate(r.id)}>
                啟用
              </Button>
            )}
          </Space>
        ) : null,
    },
  ];

  return (
    <div style={{ background: '#fff', padding: 24, borderRadius: 8 }}>
      <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h2 style={{ margin: 0 }}>角色管理</h2>
        {canManage && (
          <Button
            type="primary"
            onClick={() => {
              setEditRole(null);
              setDrawerOpen(true);
            }}
          >
            新增角色
          </Button>
        )}
      </div>
      <Table rowKey="id" columns={columns} dataSource={roles} loading={isLoading} />
      <RoleEditDrawer
        open={drawerOpen}
        role={editRole}
        onClose={() => setDrawerOpen(false)}
      />
    </div>
  );
}
