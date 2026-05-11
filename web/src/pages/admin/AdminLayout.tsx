import { Layout, Menu, Result, Spin } from 'antd';
import { Outlet, useNavigate, useLocation } from 'react-router-dom';
import { useIsAdmin, useEffectivePermissions } from '../../api/permissionGate';

const { Sider, Header, Content } = Layout;

export default function AdminLayout() {
  const navigate = useNavigate();
  const location = useLocation();
  const isAdmin = useIsAdmin();
  const { isLoading } = useEffectivePermissions();

  if (isLoading) {
    return (
      <div style={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
        <Spin size="large" />
      </div>
    );
  }

  if (!isAdmin) {
    return (
      <Result
        status="403"
        title="403"
        subTitle="您沒有存取管理者頁面的權限"
      />
    );
  }

  const menuItems = [
    { key: '/admin/roles', label: '角色管理' },
    { key: '/admin/users', label: '使用者管理' },
  ];

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider width={200} style={{ background: '#fff' }}>
        <div style={{ padding: '16px', fontWeight: 'bold', fontSize: 16 }}>管理者設定</div>
        <Menu
          mode="inline"
          selectedKeys={[location.pathname]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
          style={{ height: 'calc(100% - 56px)', borderRight: 0 }}
        />
      </Sider>
      <Layout>
        <Header style={{ background: '#fff', padding: '0 24px', borderBottom: '1px solid #f0f0f0' }}>
          <span style={{ fontWeight: 600 }}>管理者後台</span>
        </Header>
        <Content style={{ padding: 24, background: '#f5f5f5', minHeight: 'calc(100vh - 64px)' }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
