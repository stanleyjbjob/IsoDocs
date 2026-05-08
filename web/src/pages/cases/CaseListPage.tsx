import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Table, Tag, Switch, Select, Space, Button, Typography, Card } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { Link, useNavigate } from 'react-router-dom';
import dayjs from 'dayjs';
import { casesApi, CASE_STATUS_META } from '../../api/cases';
import type { CaseStatus, CaseSummary } from '../../api/cases';

export default function CaseListPage() {
  const [mineOnly, setMineOnly] = useState(false);
  const [status, setStatus] = useState<CaseStatus | undefined>(undefined);
  const navigate = useNavigate();

  const { data, isLoading } = useQuery({
    queryKey: ['cases', { mineOnly, status }],
    queryFn: () => casesApi.list({ mineOnly, status }),
  });

  const columns: ColumnsType<CaseSummary> = [
    {
      title: '案號',
      dataIndex: 'caseNumber',
      key: 'caseNumber',
      width: 160,
      render: (v: string, record) => <Link to={`/cases/${record.id}`}>{v}</Link>,
    },
    {
      title: '標題',
      dataIndex: 'title',
      key: 'title',
      ellipsis: true,
      render: (v: string, record) => <Link to={`/cases/${record.id}`}>{v}</Link>,
    },
    {
      title: '流程範本',
      dataIndex: 'templateName',
      key: 'templateName',
      width: 140,
      render: (v: string, r) => (
        <Space size={4}>
          <span>{v}</span>
          <Tag>v{r.templateVersion}</Tag>
        </Space>
      ),
    },
    {
      title: '狀態',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (v: CaseStatus) => {
        const meta = CASE_STATUS_META[v];
        return <Tag color={meta.color}>{meta.label}</Tag>;
      },
    },
    {
      title: '發起人',
      dataIndex: 'initiatorName',
      key: 'initiatorName',
      width: 120,
    },
    {
      title: '當前節點 / 承辦人',
      key: 'currentNode',
      width: 200,
      render: (_: unknown, r) =>
        r.currentNodeKey ? (
          <Space size={4}>
            <Tag>{r.currentNodeKey}</Tag>
            <span>{r.currentAssigneeName ?? '未分派'}</span>
          </Space>
        ) : (
          <span style={{ color: '#999' }}>—</span>
        ),
    },
    {
      title: '發起時間',
      dataIndex: 'initiatedAt',
      key: 'initiatedAt',
      width: 140,
      render: (v: string) => dayjs(v).format('YYYY-MM-DD HH:mm'),
    },
    {
      title: '預計完成',
      dataIndex: 'expectedCompletionAt',
      key: 'expectedCompletionAt',
      width: 140,
      render: (v: string | null) => (v ? dayjs(v).format('YYYY-MM-DD') : '—'),
    },
  ];

  return (
    <div style={{ padding: 24 }}>
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          工作需求單
        </Typography.Title>
        <Button type="primary" onClick={() => navigate('/cases/new')}>
          發起新案件
        </Button>
      </Space>
      <Card size="small" style={{ marginBottom: 16 }}>
        <Space size={16} wrap>
          <Space>
            <span>只看與我相關：</span>
            <Switch checked={mineOnly} onChange={setMineOnly} />
          </Space>
          <Space>
            <span>狀態：</span>
            <Select<CaseStatus | undefined>
              style={{ width: 140 }}
              allowClear
              placeholder="全部"
              value={status}
              onChange={setStatus}
              options={[
                { value: 'in_progress', label: '進行中' },
                { value: 'completed', label: '已結案' },
                { value: 'voided', label: '已作廢' },
              ]}
            />
          </Space>
        </Space>
      </Card>
      <Table<CaseSummary>
        rowKey="id"
        columns={columns}
        dataSource={data?.items ?? []}
        loading={isLoading}
        pagination={{ pageSize: 20, showTotal: (t) => `共 ${t} 筆` }}
      />
    </div>
  );
}
