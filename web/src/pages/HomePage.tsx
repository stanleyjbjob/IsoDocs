import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Layout,
  Typography,
  Card,
  Space,
  Button,
  Table,
  Tag,
  Spin,
  Empty,
  Badge,
  Switch,
} from 'antd';
import { BellOutlined, FileTextOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../contexts/AuthContext';
import { dashboardApi, type TodoItem, type CaseSummary } from '../api/dashboard';

const { Header, Content } = Layout;
const { Title, Text } = Typography;

const STATUS_COLORS: Record<string, string> = {
  InProgress: 'processing',
  Pending: 'warning',
  Closed: 'success',
  Voided: 'error',
  Returned: 'volcano',
  Completed: 'success',
  Skipped: 'default',
};

const STATUS_LABELS: Record<string, string> = {
  InProgress: '進行中',
  Pending: '待處理',
  Closed: '已結案',
  Voided: '已作廢',
  Returned: '已退回',
  Completed: '已完成',
  Skipped: '已略過',
};

export default function HomePage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [adminView, setAdminView] = useState(false);

  const { data: todos = [], isLoading: loadingTodos } = useQuery({
    queryKey: ['me', 'todos'],
    queryFn: () => dashboardApi.getMyTodos(),
    retry: false,
    staleTime: 1000 * 60 * 5,
  });

  const { data: initiatedCases = [], isLoading: loadingInitiated } = useQuery({
    queryKey: ['me', 'initiated-cases'],
    queryFn: () => dashboardApi.getMyInitiatedCases(),
    retry: false,
    staleTime: 1000 * 60 * 5,
  });

  const {
    data: adminCases = [],
    isLoading: loadingAdmin,
    isError: adminDenied,
  } = useQuery({
    queryKey: ['admin', 'cases'],
    queryFn: () => dashboardApi.getAdminCases(),
    retry: false,
    staleTime: 1000 * 60 * 5,
  });

  const showAdminToggle = !loadingAdmin && !adminDenied;
  const isLoading = loadingTodos || loadingInitiated || loadingAdmin;

  const todoColumns: ColumnsType<TodoItem> = [
    {
      title: '案件編號',
      dataIndex: 'caseNumber',
      key: 'caseNumber',
      width: 160,
      render: (text: string, record: TodoItem) => (
        <Button type="link" style={{ padding: 0 }} onClick={() => navigate(`/cases/${record.caseId}`)}>
          {text}
        </Button>
      ),
    },
    { title: '案件名稱', dataIndex: 'caseTitle', key: 'caseTitle', ellipsis: true },
    { title: '節點', dataIndex: 'nodeName', key: 'nodeName', width: 140 },
    {
      title: '狀態',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (s: string) => (
        <Tag color={STATUS_COLORS[s] ?? 'default'}>{STATUS_LABELS[s] ?? s}</Tag>
      ),
    },
    {
      title: '預計完成',
      dataIndex: 'expectedAt',
      key: 'expectedAt',
      width: 120,
      render: (v: string | null) =>
        v ? new Date(v).toLocaleDateString('zh-TW') : '—',
    },
  ];

  const caseColumns: ColumnsType<CaseSummary> = [
    {
      title: '案件編號',
      dataIndex: 'caseNumber',
      key: 'caseNumber',
      width: 160,
      render: (text: string, record: CaseSummary) => (
        <Button type="link" style={{ padding: 0 }} onClick={() => navigate(`/cases/${record.id}`)}>
          {text}
        </Button>
      ),
    },
    { title: '案件名稱', dataIndex: 'title', key: 'title', ellipsis: true },
    {
      title: '狀態',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (s: string) => (
        <Tag color={STATUS_COLORS[s] ?? 'default'}>{STATUS_LABELS[s] ?? s}</Tag>
      ),
    },
    {
      title: '發起時間',
      dataIndex: 'initiatedAt',
      key: 'initiatedAt',
      width: 120,
      render: (v: string) => new Date(v).toLocaleDateString('zh-TW'),
    },
    {
      title: '預計完成',
      dataIndex: 'expectedCompletionAt',
      key: 'expectedCompletionAt',
      width: 120,
      render: (v: string | null) =>
        v ? new Date(v).toLocaleDateString('zh-TW') : '—',
    },
  ];

  const displayedCases = adminView ? adminCases : initiatedCases;

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Title level={4} style={{ color: '#fff', margin: 0 }}>
          IsoDocs
        </Title>
        <Space>
          {showAdminToggle && (
            <Space>
              <Text style={{ color: '#fff' }}>全公司視角</Text>
              <Switch
                checked={adminView}
                onChange={setAdminView}
                checkedChildren="管理者"
                unCheckedChildren="個人"
              />
            </Space>
          )}
          <Text style={{ color: '#fff' }}>{user?.displayName ?? '訪客'}</Text>
          <Button onClick={() => void logout()}>登出</Button>
        </Space>
      </Header>
      <Content style={{ padding: 24 }}>
        <Spin spinning={isLoading}>
          <Space direction="vertical" size="large" style={{ width: '100%' }}>
            {!adminView && (
              <Card
                title={
                  <Space>
                    <BellOutlined />
                    <span>我的待辦</span>
                    {todos.length > 0 && (
                      <Badge count={todos.length} overflowCount={99} />
                    )}
                  </Space>
                }
              >
                <Table<TodoItem>
                  dataSource={todos}
                  columns={todoColumns}
                  rowKey="caseNodeId"
                  pagination={{ pageSize: 10, hideOnSinglePage: true }}
                  locale={{ emptyText: <Empty description="目前沒有待辦事項" /> }}
                />
              </Card>
            )}

            <Card
              title={
                <Space>
                  <FileTextOutlined />
                  <span>{adminView ? '全公司案件' : '我發起的案件'}</span>
                  {displayedCases.length > 0 && (
                    <Badge count={displayedCases.length} overflowCount={999} />
                  )}
                </Space>
              }
            >
              <Table<CaseSummary>
                dataSource={displayedCases}
                columns={caseColumns}
                rowKey="id"
                pagination={{ pageSize: 10, hideOnSinglePage: true }}
                locale={{
                  emptyText: (
                    <Empty
                      description={adminView ? '目前沒有案件' : '尚未發起任何案件'}
                    />
                  ),
                }}
              />
            </Card>
          </Space>
        </Spin>
      </Content>
    </Layout>
  );
}
