import { Layout, Typography, Card, Space, Button, Alert, Col, Row } from 'antd';
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
        <Card style={{ marginBottom: 16 }}>
          <Title level={3}>歡迎使用 IsoDocs</Title>
          <Paragraph>
            工作需求單管理系統：支持發起、處理、簽核、作廢、重開與衍生子流程。
          </Paragraph>
        </Card>
        <Row gutter={[16, 16]}>
          <Col xs={24} md={12}>
            <Card title="工作需求單" extra={<Link to="/cases">查看全部</Link>}>
              <Paragraph>查看你發起或承辦的案件、追蹤狀態，或發起新案件。</Paragraph>
              <Space>
                <Link to="/cases">
                  <Button>我的案件</Button>
                </Link>
                <Link to="/cases/new">
                  <Button type="primary">發起新案件</Button>
                </Link>
              </Space>
            </Card>
          </Col>
          {isAdmin && (
            <Col xs={24} md={12}>
              <Alert
                type="info"
                showIcon
                message="您是系統管理者"
                description={
                  <Space direction="vertical">
                    <span>可進入管理者區設定角色、自訂欄位、流程範本與指派使用者。</span>
                    <Space>
                      <Link to="/admin/roles">
                        <Button type="primary" size="small">
                          角色與權限
                        </Button>
                      </Link>
                      <Link to="/admin/users">
                        <Button size="small">使用者與指派</Button>
                      </Link>
                      <Link to="/admin/fields">
                        <Button size="small">自訂欄位</Button>
                      </Link>
                      <Link to="/admin/workflow-templates">
                        <Button size="small">流程範本</Button>
                      </Link>
                    </Space>
                  </Space>
                }
              />
            </Col>
          )}
        </Row>
      </Content>
    </Layout>
  );
}
