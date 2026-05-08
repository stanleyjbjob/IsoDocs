import { Layout, Card, Typography, Button, Space, Alert } from 'antd';
import { useAuth } from '../contexts/AuthContext';
import { useNavigate } from 'react-router-dom';
import { isMsalConfigured } from '../lib/msalConfig';

const { Title, Paragraph } = Typography;

export default function LoginPage() {
  const { isAuthenticated, isLoading, login } = useAuth();
  const navigate = useNavigate();

  if (isAuthenticated) {
    navigate('/', { replace: true });
    return null;
  }

  const handleLogin = async () => {
    try {
      await login();
      // login() 觸發 loginRedirect 後頁面會重導，此處 navigate 通常跑不到（保留以防萬一）
      navigate('/', { replace: true });
    } catch (err) {
      // eslint-disable-next-line no-console
      console.error('登入失敗：', err);
    }
  };

  return (
    <Layout
      style={{
        minHeight: '100vh',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      <Card style={{ width: 420 }}>
        <Space direction="vertical" size="large" style={{ width: '100%' }}>
          <Title level={3} style={{ marginBottom: 0 }}>
            登入 IsoDocs
          </Title>
          <Paragraph type="secondary" style={{ marginBottom: 0 }}>
            請使用您的 Microsoft 公司帳號登入。
          </Paragraph>
          {isMsalConfigured ? (
            <Alert
              type="info"
              showIcon
              message="使用 Microsoft 帳號登入"
              description="點擊下方按鈕後將導向 Microsoft 登入頁面，登入完成後自動回到 IsoDocs。"
            />
          ) : (
            <Alert
              type="warning"
              showIcon
              message="Azure AD 尚未設定"
              description="請設定 VITE_AZURE_CLIENT_ID 與 VITE_AZURE_TENANT_ID 環境變數，並重新啟動 dev server。詳見 .env.example。"
            />
          )}
          <Button
            type="primary"
            block
            size="large"
            loading={isLoading}
            disabled={!isMsalConfigured}
            onClick={() => void handleLogin()}
          >
            使用 Microsoft 帳號登入
          </Button>
        </Space>
      </Card>
    </Layout>
  );
}
