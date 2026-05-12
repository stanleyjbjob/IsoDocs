import { useState } from 'react';
import { Layout, Typography, Card, List, Badge, Button, Space, Tag, Spin } from 'antd';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import dayjs from 'dayjs';
import {
  listNotifications,
  markNotificationRead,
  markAllNotificationsRead,
} from '../api/notifications';

const { Header, Content } = Layout;
const { Title, Text } = Typography;

export default function NotificationsPage() {
  const [unreadOnly, setUnreadOnly] = useState(false);
  const queryClient = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['notifications', unreadOnly],
    queryFn: () => listNotifications(unreadOnly),
  });

  const markReadMutation = useMutation({
    mutationFn: (id: string) => markNotificationRead(id),
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  });

  const markAllReadMutation = useMutation({
    mutationFn: markAllNotificationsRead,
    onSuccess: () => void queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  });

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Space>
          <Link to="/" style={{ color: '#fff' }}>
            ← 返回首頁
          </Link>
          <Title level={4} style={{ color: '#fff', margin: 0 }}>
            通知中心
          </Title>
        </Space>
        <Badge count={data?.unreadCount ?? 0} overflowCount={99}>
          <span style={{ color: '#fff', fontSize: 14 }}>未讀</span>
        </Badge>
      </Header>

      <Content style={{ padding: 24 }}>
        <Card
          title={
            <Space>
              <Text strong>通知清單</Text>
              {(data?.unreadCount ?? 0) > 0 && (
                <Badge count={data?.unreadCount} overflowCount={99} />
              )}
            </Space>
          }
          extra={
            <Space>
              <Button
                size="small"
                type={unreadOnly ? 'primary' : 'default'}
                onClick={() => setUnreadOnly((v) => !v)}
              >
                {unreadOnly ? '顯示全部' : '只看未讀'}
              </Button>
              <Button
                size="small"
                onClick={() => markAllReadMutation.mutate()}
                loading={markAllReadMutation.isPending}
                disabled={(data?.unreadCount ?? 0) === 0}
              >
                全部標為已讀
              </Button>
            </Space>
          }
        >
          {isLoading ? (
            <div style={{ textAlign: 'center', padding: 40 }}>
              <Spin />
            </div>
          ) : (
            <List
              dataSource={data?.items ?? []}
              locale={{ emptyText: '沒有通知' }}
              renderItem={(item) => (
                <List.Item
                  style={{
                    backgroundColor: item.isRead ? 'transparent' : '#e6f7ff',
                    padding: '12px 16px',
                    borderRadius: 4,
                    marginBottom: 4,
                  }}
                  actions={
                    !item.isRead
                      ? [
                          <Button
                            key="read"
                            size="small"
                            onClick={() => markReadMutation.mutate(item.id)}
                            loading={markReadMutation.isPending}
                          >
                            標為已讀
                          </Button>,
                        ]
                      : []
                  }
                >
                  <List.Item.Meta
                    title={
                      <Space>
                        {!item.isRead && <Tag color="blue">未讀</Tag>}
                        <Text strong={!item.isRead}>{item.subject}</Text>
                      </Space>
                    }
                    description={
                      <Space direction="vertical" size={2}>
                        <Text type="secondary">{item.body}</Text>
                        <Text type="secondary" style={{ fontSize: 12 }}>
                          {dayjs(item.createdAt).format('YYYY/MM/DD HH:mm')}
                        </Text>
                      </Space>
                    }
                  />
                </List.Item>
              )}
            />
          )}
        </Card>
      </Content>
    </Layout>
  );
}
