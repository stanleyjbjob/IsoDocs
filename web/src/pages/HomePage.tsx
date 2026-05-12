import { Layout, Typography, Card, Space, Button, Badge } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../contexts/AuthContext';
import { listNotifications } from '../api/notifications';

const { Header, Content } = Layout;
const { Title, Paragraph } = Typography;

export default function HomePage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const { data: notifData } = useQuery({
    queryKey: ['notifications', false],
    queryFn: () => listNotifications(false),
    refetchInterval: 60_000,
  });

  const unreadCount = notifData?.unreadCount ?? 0;

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Title level={4} style={{ color: '#fff', margin: 0 }}>
          IsoDocs
        </Title>
        <Space>
          <Badge count={unreadCount} overflowCount={99}>
            <Button onClick={() => navigate('/notifications')}>🔔 通知</Button>
          </Badge>
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
