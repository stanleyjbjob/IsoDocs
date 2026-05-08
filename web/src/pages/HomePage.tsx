import { Layout, Typography, Card, Space, Button } from 'antd';
import { useAuth } from '../contexts/AuthContext';

const { Header, Content } = Layout;
const { Title, Paragraph } = Typography;

export default function HomePage() {
  const { user, logout } = useAuth();

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
        <Card>
          <Title level={3}>歡迎使用 IsoDocs</Title>
          <Paragraph>
            這是 React 18 + Vite + TypeScript 前端骨架。後續任務會逐步補上案件清單、流程範本、自訂欄位等模組。
          </Paragraph>
        </Card>
      </Content>
    </Layout>
  );
}
