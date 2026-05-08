import { Layout, Typography, Card, Space, Button, Alert } from 'antd';
import { Link } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { useIsAdmin } from '../api/permissionGate';

const { Header, Content } = Layout;
const { Title, Paragraph } = Typography;

export default function HomePage() {
  const { user, logout } = useAuth();
  const isAdmin = useIsAdmin();

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Title level={4} style={{ color: '#fff', margin: 0 }}>
          IsoDocs
        </Title>
        <Space>
          <span style={{ color: '#fff' }}>{user?.displayName ?? '訪客'}</span>
          {isAdmin && (
            <Link to="/admin/roles">
              <Button>進入管理者區</Button>
            </Link>
          )}
          <Button onClick={() => void logout()}>登出</Button>
        </Space>
      </Header>
      <Content style={{ padding: 24 }}>
        <Card>
          <Title level={3}>歡迎使用 IsoDocs</Title>
          <Paragraph>
            這是 React 18 + Vite + TypeScript 前端骨架。後續任務會逐步補上案件清單、流程範本、自訂欄位等模組。
          </Paragraph>
          {isAdmin && (
            <Alert
              type="info"
              showIcon
              message="您是系統管理者"
              description={
                <Space direction="vertical">
                  <span>可進入管理者區設定角色與指派使用者。</span>
                  <Space>
                    <Link to="/admin/roles">
                      <Button type="primary" size="small">
                        角色與權限
                      </Button>
                    </Link>
                    <Link to="/admin/users">
                      <Button size="small">使用者與指派</Button>
                    </Link>
                  </Space>
                </Space>
              }
            />
          )}
        </Card>
      </Content>
    </Layout>
  );
}
