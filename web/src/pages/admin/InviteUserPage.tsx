import { useState } from 'react';
import { Form, Input, Button, Card, Typography, Alert, message } from 'antd';
import { usersApi, type InviteUserRequest } from '../../api/users';

const { Title } = Typography;

interface InviteFormValues {
  email: string;
  displayName: string;
  roleId: string;
}

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export default function InviteUserPage() {
  const [form] = Form.useForm<InviteFormValues>();
  const [loading, setLoading] = useState(false);
  const [inviteRedeemUrl, setInviteRedeemUrl] = useState<string | null>(null);

  const handleSubmit = async (values: InviteFormValues) => {
    setLoading(true);
    setInviteRedeemUrl(null);
    try {
      const request: InviteUserRequest = {
        email: values.email,
        displayName: values.displayName,
        roleId: values.roleId,
      };
      const result = await usersApi.inviteUser(request);
      void message.success(`邀請已發送至 ${result.email}`);
      setInviteRedeemUrl(result.inviteRedeemUrl);
      form.resetFields();
    } catch {
      void message.error('邀請失敗，請確認您具有管理者權限，並核對輸入資料。');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ maxWidth: 600, margin: '40px auto', padding: '0 16px' }}>
      <Card>
        <Title level={3}>邀請成員</Title>
        <Form form={form} layout="vertical" onFinish={handleSubmit} autoComplete="off">
          <Form.Item
            label="電子郵件"
            name="email"
            rules={[
              { required: true, message: '請輸入電子郵件' },
              { type: 'email', message: '請輸入有效的電子郵件格式' },
            ]}
          >
            <Input placeholder="user@example.com" />
          </Form.Item>

          <Form.Item
            label="顯示名稱"
            name="displayName"
            rules={[{ required: true, message: '請輸入顯示名稱' }]}
          >
            <Input placeholder="張三" />
          </Form.Item>

          <Form.Item
            label="角色 ID"
            name="roleId"
            rules={[
              { required: true, message: '請輸入角色 ID' },
              { pattern: UUID_PATTERN, message: '請輸入有效的 UUID 格式' },
            ]}
          >
            <Input placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" />
          </Form.Item>

          <Form.Item>
            <Button type="primary" htmlType="submit" loading={loading} block>
              發送邀請
            </Button>
          </Form.Item>
        </Form>

        {inviteRedeemUrl && (
          <Alert
            type="success"
            message="邀請連結已產生"
            description={
              <a href={inviteRedeemUrl} target="_blank" rel="noopener noreferrer">
                {inviteRedeemUrl}
              </a>
            }
            showIcon
          />
        )}
      </Card>
    </div>
  );
}
