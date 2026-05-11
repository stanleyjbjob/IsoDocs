import { Layout, Typography, Card, Space, Button } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { useIsAdmin } from '../api/permissionGate';

const { Header, Content } = Layout;
const { Title, Paragraph } = Typography;

export default function HomePage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const isAdmin = useIsAdmin();

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Title level={4} style={{ color: '#fff', margin: 0 }}>
          IsoDocs
        </Title>
        <Space>
          <span style={{ color: '#fff' }}>{user?.displayName ?? '訪客'}</span>
          <Button onClick={() => void logout()}>登出</Button>
        </Space>
      </Header>
      <Content style={{ padding: 24 }}>
        <Card style={{ marginBottom: 16 }}>
          <Title level={3}>歡迎使用 IsoDocs</Title>
          <Paragraph>
            這是 React 18 + Vite + TypeScript 前端骨架。後續任務會逐步補上案件清單、流程範本、自訂欄位等模組。
          </Paragraph>
        </Card>
        {isAdmin && (
          <Card>
            <Title level={5} style={{ marginTop: 0 }}>管理者快速進入</Title>
            <Button
              type="primary"
              onClick={() => navigate('/admin/roles')}
            >
              進入管理者後台
            </Button>
          </Card>
        )}
      </Content>
    </Layout>
  );
}
