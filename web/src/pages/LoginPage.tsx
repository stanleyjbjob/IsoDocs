import { Layout, Card, Typography, Button, Space, Alert } from 'antd';
import { useAuth } from '../contexts/AuthContext';
import { useNavigate } from 'react-router-dom';

const { Title, Paragraph } = Typography;

export default function LoginPage() {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();

  if (isAuthenticated) {
    navigate('/', { replace: true });
    return null;
  }

  const handleLogin = async () => {
    try {
      await login();
      navigate('/', { replace: true });
    } catch (err) {
      // 後續 issue #34 [2.1.2] 完成 MSAL 整合後會走真實流程
      // eslint-disable-next-line no-console
      console.warn('登入失敗：', err);
    }
  };

  return (
    <Layout style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
      <Card style={{ width: 420 }}>
        <Space direction="vertical" size="large" style={{ width: '100%' }}>
          <Title level={3} style={{ marginBottom: 0 }}>
            登入 IsoDocs
          </Title>
          <Paragraph type="secondary" style={{ marginBottom: 0 }}>
            請使用您的 Microsoft 公司帳號登入。
          </Paragraph>
          <Alert
            type="info"
            showIcon
            message="登入流程尚未整合"
            description="Azure AD / Entra ID OIDC 將於 issue #34 [2.1.2] 完成 MSAL 串接後啟用。"
          />
          <Button type="primary" block size="large" onClick={() => void handleLogin()}>
            使用 Microsoft 帳號登入
          </Button>
        </Space>
      </Card>
    </Layout>
  );
}
