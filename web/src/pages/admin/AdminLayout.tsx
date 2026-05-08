import { useMemo } from 'react';
import { Layout, Menu, Typography, Button, Space, Result, Spin } from 'antd';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import type { MenuProps } from 'antd';
import { useAuth } from '../../contexts/AuthContext';
import { useIsAdmin, usePermissionContext } from '../../api/permissionGate';

const { Header, Sider, Content } = Layout;
const { Title } = Typography;

/**
 * 管理者區 layout。
 *
 * - 對應驗收條件「操作權限受 RBAC 控管」：非 admin 進來會看到 403
 * - 左側 sidebar 提供【角色】【使用者】【自訂欄位】入口
 * - 在載入角色資料期間（需這份資料來決定 isAdmin）顯示 Spin
 */
export default function AdminLayout() {
  const { user, logout } = useAuth();
  const isAdmin = useIsAdmin();
  const { isLoading } = usePermissionContext();
  const location = useLocation();
  const navigate = useNavigate();

  const selectedKeys = useMemo(() => {
    if (location.pathname.startsWith('/admin/roles')) return ['roles'];
    if (location.pathname.startsWith('/admin/users')) return ['users'];
    if (location.pathname.startsWith('/admin/fields')) return ['fields'];
    return [];
  }, [location.pathname]);

  const menuItems: MenuProps['items'] = [
    { key: 'roles', label: <Link to="/admin/roles">角色與權限</Link> },
    { key: 'users', label: <Link to="/admin/users">使用者與指派</Link> },
    { key: 'fields', label: <Link to="/admin/fields">自訂欄位</Link> },
  ];

  if (isLoading) {
    return (
      <div
        style={{
          minHeight: '100vh',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <Spin size="large" tip="載入中…" />
      </div>
    );
  }

  if (!isAdmin) {
    return (
      <Result
        status="403"
        title="403"
        subTitle="您沒有進入這個頁面的權限。請聯絡系統管理者。"
        extra={
          <Button type="primary" onClick={() => navigate('/')}>
            返回首頁
          </Button>
        }
      />
    );
  }

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          paddingInline: 24,
        }}
      >
        <Space>
          <Title level={4} style={{ color: '#fff', margin: 0 }}>
            <Link to="/" style={{ color: '#fff' }}>
              IsoDocs
            </Link>
          </Title>
          <span style={{ color: 'rgba(255,255,255,0.65)' }}>· 管理者區</span>
        </Space>
        <Space>
          <span style={{ color: '#fff' }}>{user?.displayName ?? '訪客'}</span>
          <Button onClick={() => void logout()}>登出</Button>
        </Space>
      </Header>
      <Layout>
        <Sider width={220} theme="light" style={{ borderInlineEnd: '1px solid #f0f0f0' }}>
          <Menu mode="inline" selectedKeys={selectedKeys} items={menuItems} style={{ height: '100%', borderRight: 0 }} />
        </Sider>
        <Content style={{ padding: 24, background: '#fafafa' }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
