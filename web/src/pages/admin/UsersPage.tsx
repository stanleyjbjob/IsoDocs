import { useState } from 'react';
import { Table, Button, Tag, Space } from 'antd';
import { useQuery } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { rolesApi, type UserSummary } from '../../api/roles';
import { useHasPermission } from '../../api/permissionGate';
import UserRolesAssignDrawer from './UserRolesAssignDrawer';

export default function UsersPage() {
  const [selectedUser, setSelectedUser] = useState<UserSummary | null>(null);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const canAssign = useHasPermission('users.assign_roles');

  const { data: users = [], isLoading } = useQuery({
    queryKey: ['users'],
    queryFn: rolesApi.listUsers,
  });

  const { data: roles = [] } = useQuery({
    queryKey: ['roles'],
    queryFn: rolesApi.list,
  });

  const columns: ColumnsType<UserSummary> = [
    { title: '顯示名稱', dataIndex: 'displayName', key: 'displayName', width: 140 },
    { title: 'Email', dataIndex: 'email', key: 'email' },
    { title: '部門', dataIndex: 'department', key: 'department', width: 120 },
    { title: '職稱', dataIndex: 'jobTitle', key: 'jobTitle', width: 140 },
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
      title: '操作',
      key: 'actions',
      width: 120,
      render: (_: unknown, u: UserSummary) =>
        canAssign ? (
          <Space>
            <Button
              size="small"
              onClick={() => {
                setSelectedUser(u);
                setDrawerOpen(true);
              }}
            >
              指派角色
            </Button>
          </Space>
        ) : null,
    },
  ];

  return (
    <div style={{ background: '#fff', padding: 24, borderRadius: 8 }}>
      <div style={{ marginBottom: 16 }}>
        <h2 style={{ margin: 0 }}>使用者管理</h2>
      </div>
      <Table rowKey="id" columns={columns} dataSource={users} loading={isLoading} />
      {selectedUser && (
        <UserRolesAssignDrawer
          open={drawerOpen}
          user={selectedUser}
          roles={roles}
          onClose={() => setDrawerOpen(false)}
        />
      )}
    </div>
  );
}
