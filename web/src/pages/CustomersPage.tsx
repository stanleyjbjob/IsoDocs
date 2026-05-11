import { useState, useEffect } from 'react';
import { Layout, Typography, Table, Button, Space, Tag, Modal, Form, Input, message } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { useAuth } from '../contexts/AuthContext';
import { customersApi } from '../api/customers';
import type { Customer, CreateCustomerRequest, UpdateCustomerRequest } from '../types/customer';

const { Header, Content } = Layout;
const { Title } = Typography;

export default function CustomersPage() {
  const { user, logout } = useAuth();
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [loading, setLoading] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<Customer | null>(null);
  const [form] = Form.useForm();

  const loadCustomers = async () => {
    setLoading(true);
    try {
      const data = await customersApi.list(true);
      setCustomers(data);
    } catch {
      void message.error('載入客戶失敗');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadCustomers();
  }, []);

  const openCreate = () => {
    setEditing(null);
    form.resetFields();
    setModalOpen(true);
  };

  const openEdit = (c: Customer) => {
    setEditing(c);
    form.setFieldsValue(c);
    setModalOpen(true);
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (editing) {
        await customersApi.update(editing.id, values as UpdateCustomerRequest);
        void message.success('更新成功');
      } else {
        await customersApi.create(values as CreateCustomerRequest);
        void message.success('建立成功');
      }
      setModalOpen(false);
      void loadCustomers();
    } catch {
      // validation error handled by form
    }
  };

  const toggleActive = async (c: Customer) => {
    try {
      if (c.isActive) {
        await customersApi.deactivate(c.id);
      } else {
        await customersApi.activate(c.id);
      }
      void loadCustomers();
    } catch {
      void message.error('操作失敗');
    }
  };

  const columns = [
    { title: '代碼', dataIndex: 'code', key: 'code', width: 120 },
    { title: '名稱', dataIndex: 'name', key: 'name' },
    { title: '聯絡人', dataIndex: 'contactPerson', key: 'contactPerson' },
    { title: 'Email', dataIndex: 'contactEmail', key: 'contactEmail' },
    { title: '電話', dataIndex: 'contactPhone', key: 'contactPhone' },
    {
      title: '狀態',
      key: 'isActive',
      render: (_: unknown, r: Customer) => (
        <Tag color={r.isActive ? 'green' : 'red'}>{r.isActive ? '啟用' : '停用'}</Tag>
      ),
    },
    {
      title: '操作',
      key: 'actions',
      render: (_: unknown, r: Customer) => (
        <Space>
          <Button size="small" onClick={() => openEdit(r)}>編輯</Button>
          <Button size="small" danger={r.isActive} onClick={() => void toggleActive(r)}>
            {r.isActive ? '停用' : '啟用'}
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Title level={4} style={{ color: '#fff', margin: 0 }}>IsoDocs — 客戶管理</Title>
        <Space>
          <span style={{ color: '#fff' }}>{user?.displayName ?? '訪客'}</span>
          <Button onClick={() => void logout()}>登出</Button>
        </Space>
      </Header>
      <Content style={{ padding: 24 }}>
        <Space style={{ marginBottom: 16 }}>
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>新增客戶</Button>
        </Space>
        <Table
          rowKey="id"
          loading={loading}
          dataSource={customers}
          columns={columns}
          pagination={{ pageSize: 20 }}
        />
      </Content>
      <Modal
        title={editing ? '編輯客戶' : '新增客戶'}
        open={modalOpen}
        onOk={() => void handleSubmit()}
        onCancel={() => setModalOpen(false)}
        destroyOnClose
      >
        <Form form={form} layout="vertical">
          {!editing && (
            <Form.Item name="code" label="代碼" rules={[{ required: true, message: '請輸入客戶代碼' }]}>
              <Input maxLength={64} />
            </Form.Item>
          )}
          <Form.Item name="name" label="名稱" rules={[{ required: true, message: '請輸入客戶名稱' }]}>
            <Input maxLength={256} />
          </Form.Item>
          <Form.Item name="contactPerson" label="聯絡人">
            <Input maxLength={128} />
          </Form.Item>
          <Form.Item name="contactEmail" label="Email" rules={[{ type: 'email', message: 'Email 格式不正確' }]}>
            <Input maxLength={256} />
          </Form.Item>
          <Form.Item name="contactPhone" label="電話">
            <Input maxLength={64} />
          </Form.Item>
          <Form.Item name="note" label="備註">
            <Input.TextArea maxLength={1024} rows={3} />
          </Form.Item>
        </Form>
      </Modal>
    </Layout>
  );
}
