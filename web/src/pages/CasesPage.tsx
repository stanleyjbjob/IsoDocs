import { useState, useMemo, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Button,
  Card,
  Col,
  DatePicker,
  Form,
  Input,
  Row,
  Select,
  Space,
  Table,
  Tag,
  Typography,
} from 'antd';
import { ReloadOutlined, SearchOutlined } from '@ant-design/icons';
import type { TablePaginationConfig } from 'antd';
import type { SorterResult } from 'antd/es/table/interface';
import dayjs from 'dayjs';
import { listCases, searchCases } from '../api/cases';
import type { CaseSummaryDto, CaseStatusValue, ListCasesParams } from '../types/case';
import { CaseStatusMap } from '../types/case';

const { RangePicker } = DatePicker;

const STATUS_OPTIONS = [
  { value: 1, label: '進行中' },
  { value: 2, label: '已結案' },
  { value: 3, label: '已作廢' },
];

const STATUS_TAG_COLOR: Record<number, string> = {
  1: 'processing',
  2: 'success',
  3: 'error',
};

function Highlighted({ text, keyword }: { text: string; keyword: string }) {
  if (!keyword) return <>{text}</>;
  const escaped = keyword.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const parts = text.split(new RegExp(`(${escaped})`, 'gi'));
  return (
    <>
      {parts.map((part, i) =>
        part.toLowerCase() === keyword.toLowerCase() ? (
          <mark key={i} style={{ background: '#ffe58f', padding: 0 }}>
            {part}
          </mark>
        ) : (
          part
        ),
      )}
    </>
  );
}

interface FilterValues {
  keyword?: string;
  status?: number;
  caseNumberPrefix?: string;
  dateRange?: [dayjs.Dayjs | null, dayjs.Dayjs | null] | null;
}

export default function CasesPage() {
  const [form] = Form.useForm<FilterValues>();
  const [params, setParams] = useState<ListCasesParams>({ page: 1, pageSize: 20 });
  const [activeKeyword, setActiveKeyword] = useState('');

  const isSearch = activeKeyword.trim().length > 0;

  const { data, isFetching } = useQuery({
    queryKey: ['cases', isSearch, params, activeKeyword],
    queryFn: () =>
      isSearch
        ? searchCases({ ...params, keyword: activeKeyword })
        : listCases(params),
    placeholderData: (prev) => prev,
  });

  const applyFilters = useCallback(() => {
    const v = form.getFieldsValue();
    setActiveKeyword(v.keyword?.trim() ?? '');
    setParams({
      page: 1,
      pageSize: params.pageSize ?? 20,
      status: v.status,
      caseNumberPrefix: v.caseNumberPrefix?.trim() || undefined,
      initiatedFrom: v.dateRange?.[0]?.startOf('day').toISOString(),
      initiatedTo: v.dateRange?.[1]?.endOf('day').toISOString(),
    });
  }, [form, params.pageSize]);

  const reset = useCallback(() => {
    form.resetFields();
    setActiveKeyword('');
    setParams({ page: 1, pageSize: 20 });
  }, [form]);

  const onTableChange = useCallback(
    (
      pagination: TablePaginationConfig,
      _filters: unknown,
      sorter: SorterResult<CaseSummaryDto> | SorterResult<CaseSummaryDto>[],
    ) => {
      const s = Array.isArray(sorter) ? sorter[0] : sorter;
      setParams((p) => ({
        ...p,
        page: pagination.current ?? 1,
        pageSize: pagination.pageSize ?? 20,
        sortBy: s.column ? (s.field as string) : undefined,
        sortDescending: s.order === 'descend',
      }));
    },
    [],
  );

  const columns = useMemo(
    () => [
      {
        title: '案件號',
        dataIndex: 'caseNumber',
        key: 'caseNumber',
        sorter: true,
        render: (v: string) => <Highlighted text={v} keyword={activeKeyword} />,
      },
      {
        title: '標題',
        dataIndex: 'title',
        key: 'title',
        sorter: true,
        render: (v: string) => <Highlighted text={v} keyword={activeKeyword} />,
      },
      {
        title: '狀態',
        dataIndex: 'status',
        key: 'status',
        sorter: true,
        render: (v: CaseStatusValue) => (
          <Tag color={STATUS_TAG_COLOR[v]}>{CaseStatusMap[v] ?? String(v)}</Tag>
        ),
      },
      {
        title: '文件類型',
        dataIndex: 'documentTypeName',
        render: (v: string | null) => v ?? '-',
      },
      {
        title: '客戶',
        dataIndex: 'customerName',
        render: (v: string | null) => v ?? '-',
      },
      {
        title: '發起時間',
        dataIndex: 'initiatedAt',
        key: 'initiatedAt',
        sorter: true,
        render: (v: string) => dayjs(v).format('YYYY-MM-DD HH:mm'),
      },
      {
        title: '預計完成',
        dataIndex: 'expectedCompletionAt',
        key: 'expectedcompletionat',
        sorter: true,
        render: (v: string | null) => (v ? dayjs(v).format('YYYY-MM-DD') : '-'),
      },
      {
        title: '自訂版號',
        dataIndex: 'customVersionNumber',
        render: (v: string | null) =>
          v ? <Highlighted text={v} keyword={activeKeyword} /> : '-',
      },
    ],
    [activeKeyword],
  );

  return (
    <div style={{ padding: 24 }}>
      <Typography.Title level={3} style={{ marginBottom: 16 }}>
        案件清單
      </Typography.Title>
      <Card style={{ marginBottom: 16 }}>
        <Form form={form} onFinish={applyFilters}>
          <Row gutter={[8, 8]} align="middle">
            <Col xs={24} sm={12} lg={6}>
              <Form.Item name="keyword" noStyle>
                <Input placeholder="關鍵字（案件號 / 標題 / 版號）" allowClear />
              </Form.Item>
            </Col>
            <Col xs={24} sm={6} lg={3}>
              <Form.Item name="status" noStyle>
                <Select
                  placeholder="狀態"
                  allowClear
                  options={STATUS_OPTIONS}
                  style={{ width: '100%' }}
                />
              </Form.Item>
            </Col>
            <Col xs={24} sm={6} lg={3}>
              <Form.Item name="caseNumberPrefix" noStyle>
                <Input placeholder="案件號前綴" allowClear />
              </Form.Item>
            </Col>
            <Col xs={24} sm={12} lg={6}>
              <Form.Item name="dateRange" noStyle>
                <RangePicker
                  placeholder={['發起日期起', '發起日期訖']}
                  style={{ width: '100%' }}
                />
              </Form.Item>
            </Col>
            <Col>
              <Space>
                <Button type="primary" icon={<SearchOutlined />} htmlType="submit">
                  查詢
                </Button>
                <Button icon={<ReloadOutlined />} onClick={reset}>
                  重置
                </Button>
              </Space>
            </Col>
          </Row>
        </Form>
      </Card>
      <Table<CaseSummaryDto>
        rowKey="id"
        loading={isFetching}
        columns={columns}
        dataSource={data?.items ?? []}
        onChange={onTableChange}
        pagination={{
          current: params.page ?? 1,
          pageSize: params.pageSize ?? 20,
          total: data?.totalCount ?? 0,
          showSizeChanger: true,
          showTotal: (total) => `共 ${total} 筆`,
          pageSizeOptions: [10, 20, 50, 100],
        }}
        scroll={{ x: 'max-content' }}
      />
    </div>
  );
}
