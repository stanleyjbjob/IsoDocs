import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Layout, Typography, Table, Tag, Spin, Alert, Button, Modal, Input, message } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { getSignOffTrail, submitSignOff } from '../api/cases';
import type { SignOffEntry } from '../api/cases';

const { Header, Content } = Layout;
const { Title } = Typography;
const { TextArea } = Input;

export default function CaseSignOffTrailPage() {
  const { caseId } = useParams<{ caseId: string }>();
  const [trail, setTrail] = useState<SignOffEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [modalOpen, setModalOpen] = useState(false);
  const [comment, setComment] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [nodeId, setNodeId] = useState('');

  const fetchTrail = async () => {
    if (!caseId) return;
    setLoading(true);
    setError(null);
    try {
      const data = await getSignOffTrail(caseId);
      setTrail(data);
    } catch {
      setError('無法載入簽核軌跡，請稍後再試');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void fetchTrail();
  }, [caseId]);

  const handleSignOff = async () => {
    if (!caseId || !nodeId.trim()) {
      void message.warning('請輸入節點 ID');
      return;
    }
    setSubmitting(true);
    try {
      await submitSignOff(caseId, nodeId.trim(), comment.trim() || undefined);
      void message.success('簽核成功');
      setModalOpen(false);
      setComment('');
      setNodeId('');
      await fetchTrail();
    } catch {
      void message.error('簽核失敗，請稍後再試');
    } finally {
      setSubmitting(false);
    }
  };

  const columns: ColumnsType<SignOffEntry> = [
    {
      title: '簽核節點',
      dataIndex: 'nodeName',
      key: 'nodeName',
      render: (name: string | null) =>
        name ? <Tag color="blue">{name}</Tag> : <Tag>—</Tag>,
    },
    {
      title: '簽核人 ID',
      dataIndex: 'actorUserId',
      key: 'actorUserId',
    },
    {
      title: '簽核時間',
      dataIndex: 'actionAt',
      key: 'actionAt',
      render: (val: string) => new Date(val).toLocaleString('zh-TW'),
      defaultSortOrder: 'ascend',
      sorter: (a: SignOffEntry, b: SignOffEntry) =>
        new Date(a.actionAt).getTime() - new Date(b.actionAt).getTime(),
    },
    {
      title: '簽核意見',
      dataIndex: 'comment',
      key: 'comment',
      render: (val: string | null) => val ?? '（無）',
    },
  ];

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Title level={4} style={{ color: '#fff', margin: 0 }}>
          文件發行簽核軌跡
        </Title>
      </Header>
      <Content style={{ padding: 24 }}>
        {error && <Alert type="error" message={error} style={{ marginBottom: 16 }} />}
        <div style={{ marginBottom: 16, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Title level={5} style={{ margin: 0 }}>
            案件 {caseId}
          </Title>
          <Button type="primary" onClick={() => setModalOpen(true)}>
            提交簽核
          </Button>
        </div>
        {loading ? (
          <Spin size="large" />
        ) : (
          <Table<SignOffEntry>
            dataSource={trail}
            columns={columns}
            rowKey="id"
            pagination={false}
            locale={{ emptyText: '尚無簽核紀錄' }}
          />
        )}
        <Modal
          title="提交文件發行簽核"
          open={modalOpen}
          onOk={() => void handleSignOff()}
          onCancel={() => setModalOpen(false)}
          confirmLoading={submitting}
          okText="確認簽核"
          cancelText="取消"
        >
          <div style={{ marginBottom: 12 }}>
            <div style={{ marginBottom: 4 }}>節點 ID（CaseNodeId）</div>
            <Input
              value={nodeId}
              onChange={(e) => setNodeId(e.target.value)}
              placeholder="請輸入節點 UUID"
            />
          </div>
          <div>
            <div style={{ marginBottom: 4 }}>簽核意見（選填）</div>
            <TextArea
              rows={4}
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              placeholder="請輸入簽核意見（最多 2000 字）"
              maxLength={2000}
              showCount
            />
          </div>
        </Modal>
      </Content>
    </Layout>
  );
}
