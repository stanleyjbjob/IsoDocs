import { useState } from 'react';
import { Card, Table, Tag, Button, Typography, Space, Tooltip, Empty } from 'antd';
import { useQuery } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { listUsers, type UserSummary } from '../../api/roles';
import { useHasPermission } from '../../api/permissionGate';
import UserRolesAssignDrawer from './UserRolesAssignDrawer';

const { Title, Paragraph } = Typography;

export default function UsersPage() {
  const canAssign = useHasPermission('users.assign_roles');
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<UserSummary | null>(null);

  const { data: users, isLoading } = useQuery({
    queryKey: ['users'],
    queryFn: listUsers,
  });

  const openAssign = (user: UserSummary) => {
    setEditingUser(user);
    setDrawerOpen(true);
  };

  const columns: ColumnsType<UserSummary> = [
    {
      title: '使用者',
      key: 'user',
      render: (_v, user) => (
        <Space direction="vertical" size={0}>
          <span style={{ fontWeight: 500 }}>{user.displayName}</span>
          <span style={{ color: 'rgba(0,0,0,0.45)', fontSize: 12 }}>{user.email}</span>
        </Space>
      ),
      width: 240,
    },
    {
      title: '部門／職稱',
      key: 'department',
      render: (_v, user) => (
        <Space direction="vertical" size={0}>
          <span>{user.department ?? '-'}</span>
          <span style={{ color: 'rgba(0,0,0,0.45)', fontSize: 12 }}>{user.jobTitle ?? '-'}</span>
        </Space>
      ),
      width: 200,
    },
    {
      title: '狀態',
      key: 'isActive',
      width: 80,
      render: (_v, user) =>
        user.isActive ? <Tag color="green">啟用</Tag> : <Tag>停用</Tag>,
    },
    {
      title: '已指派角色',
      key: 'roles',
      render: (_v, user) => {
        if (user.roles.length === 0) return <span style={{ color: 'rgba(0,0,0,0.45)' }}>尚未指派</span>;
        return (
          <Space size={[4, 4]} wrap>
            {user.roles.map((r) => (
              <Tag key={r.roleId} color="blue">
                {r.roleName ?? r.roleId}
              </Tag>
            ))}
          </Space>
        );
      },
    },
    {
      title: '動作',
      key: 'actions',
      width: 140,
      render: (_v, user) => (
        <Tooltip title={canAssign ? '' : '需「使用者角色指派」權限'}>
          <Button type="link" disabled={!canAssign} onClick={() => openAssign(user)}>
            指派角色
          </Button>
        </Tooltip>
      ),
    },
  ];

  return (
    <Card
      title={
        <Space direction="vertical" size={0}>
          <Title level={4} style={{ margin: 0 }}>
            使用者與指派
          </Title>
          <Paragraph type="secondary" style={{ margin: 0 }}>
            一個使用者可同時擁有多個角色，並可設定生效期限
          </Paragraph>
        </Space>
      }
    >
      <Table<UserSummary>
        rowKey="id"
        columns={columns}
        dataSource={users ?? []}
        loading={isLoading}
        pagination={false}
        locale={{ emptyText: <Empty description="尚無使用者" /> }}
      />
      <UserRolesAssignDrawer
        open={drawerOpen}
        user={editingUser}
        onClose={() => setDrawerOpen(false)}
        onSaved={() => setDrawerOpen(false)}
      />
    </Card>
  );
}
