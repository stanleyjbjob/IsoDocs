import { useQuery } from '@tanstack/react-query';
import { App, Card, Col, Descriptions, Row, Space, Steps, Table, Tabs, Tag, Timeline, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { Link, useParams } from 'react-router-dom';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import { casesApi, ACTION_TYPE_LABEL, CASE_STATUS_META, NODE_STATUS_META } from '../../api/cases';
import type { CaseDetail, CaseAction, CaseFieldValue, CaseNodeProgress, CaseRelationItem } from '../../api/cases';
import { fieldDefinitionsApi } from '../../api/fieldDefinitions';
import { DynamicFieldRenderer } from '../../components/DynamicFieldRenderer';
import type { DynamicField } from '../../components/DynamicFieldRenderer';
import CaseActionButtons from './CaseActionButtons';

dayjs.extend(relativeTime);

export default function CaseDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { message } = App.useApp();
  const { data: caseDetail, isLoading, refetch } = useQuery({
    queryKey: ['cases', id],
    queryFn: () => casesApi.get(id!),
    enabled: !!id,
  });
  const { data: fieldDefs } = useQuery({
    queryKey: ['field-definitions', { active: true }],
    queryFn: () => fieldDefinitionsApi.list({ activeOnly: true }),
  });

  if (isLoading || !caseDetail) {
    return <div style={{ padding: 24 }}>載入中…</div>;
  }

  const c = caseDetail;
  const meta = CASE_STATUS_META[c.status];
  const currentStepIndex = c.nodes.findIndex((n) => n.status === 'in_progress');

  const fieldDefMap = new Map((fieldDefs?.items ?? []).map((f) => [f.id, f] as const));

  return (
    <div style={{ padding: 24, maxWidth: 1200, margin: '0 auto' }}>
      <Space style={{ width: '100%', justifyContent: 'space-between', marginBottom: 12 }}>
        <div>
          <Typography.Title level={3} style={{ margin: 0 }}>
            <Tag color={meta.color}>{meta.label}</Tag>
            <span style={{ marginRight: 12 }}>{c.caseNumber}</span>
            <span>{c.title}</span>
          </Typography.Title>
          <Space size={12} style={{ marginTop: 4, color: '#666' }} wrap>
            <span>範本：{c.templateName} v{c.templateVersion}</span>
            <span>發起人：{c.initiatorName}</span>
            <span>發起時間：{dayjs(c.initiatedAt).format('YYYY-MM-DD HH:mm')}</span>
            {c.customVersion && <span>自訂版號：{c.customVersion}</span>}
            {c.customerName && <span>客戶：{c.customerName}</span>}
          </Space>
        </div>
        <CaseActionButtons
          caseDetail={c}
          onChanged={() => {
            refetch();
            message.success('動作完成');
          }}
        />
      </Space>

      <Card size="small" style={{ marginBottom: 16 }}>
        <Steps
          current={currentStepIndex >= 0 ? currentStepIndex : c.nodes.length}
          status={c.status === 'voided' ? 'error' : c.status === 'completed' ? 'finish' : 'process'}
          items={c.nodes.map((n) => ({
            title: n.label,
            description: (
              <Space direction="vertical" size={0}>
                <span>{NODE_STATUS_META[n.status].label}</span>
                <span style={{ color: '#999', fontSize: 12 }}>{n.assigneeName ?? '未分派'}</span>
              </Space>
            ),
          }))}
        />
      </Card>

      <Tabs
        defaultActiveKey="basic"
        items={[
          {
            key: 'basic',
            label: '基本資訊',
            children: <BasicTab caseDetail={c} />,
          },
          {
            key: 'fields',
            label: '動態欄位',
            children: <FieldsTab fields={c.fields} fieldDefMap={fieldDefMap} />,
          },
          {
            key: 'nodes',
            label: '節點進度',
            children: <NodesTab nodes={c.nodes} />,
          },
          {
            key: 'actions',
            label: '軌跡',
            children: <ActionsTab actions={c.actions} />,
          },
          {
            key: 'relations',
            label: `關聯案件 (${c.relations.length})`,
            children: <RelationsTab relations={c.relations} />,
          },
        ]}
      />
    </div>
  );
}

function BasicTab({ caseDetail }: { caseDetail: CaseDetail }) {
  const c = caseDetail;
  return (
    <Card>
      <Descriptions column={2} bordered size="small">
        <Descriptions.Item label="描述" span={2}>
          {c.description ?? <span style={{ color: '#999' }}>—</span>}
        </Descriptions.Item>
        <Descriptions.Item label="原始預計完成時間 (Original)">
          {c.originalExpectedAt ? dayjs(c.originalExpectedAt).format('YYYY-MM-DD HH:mm') : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="當前預計完成時間">
          {c.expectedCompletionAt ? dayjs(c.expectedCompletionAt).format('YYYY-MM-DD HH:mm') : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="結案時間">
          {c.completedAt ? dayjs(c.completedAt).format('YYYY-MM-DD HH:mm') : '—'}
        </Descriptions.Item>
        <Descriptions.Item label="作廢時間">
          {c.voidedAt ? dayjs(c.voidedAt).format('YYYY-MM-DD HH:mm') : '—'}
        </Descriptions.Item>
      </Descriptions>
      {c.nodes.some((n) => n.modifiedExpectedAt) && (
        <Card size="small" type="inner" title="預計完成時間修改軌跡" style={{ marginTop: 16 }}>
          <Timeline
            items={[
              ...(c.originalExpectedAt
                ? [
                    {
                      color: 'gray',
                      children: (
                        <Space direction="vertical" size={0}>
                          <span>原始設定（發起時）</span>
                          <strong>{dayjs(c.originalExpectedAt).format('YYYY-MM-DD HH:mm')}</strong>
                        </Space>
                      ),
                    },
                  ]
                : []),
              ...c.nodes
                .filter((n) => n.modifiedExpectedAt)
                .map((n) => ({
                  color: 'blue',
                  children: (
                    <Space direction="vertical" size={0}>
                      <span>「{n.label}」節點修改</span>
                      <strong>{dayjs(n.modifiedExpectedAt!).format('YYYY-MM-DD HH:mm')}</strong>
                    </Space>
                  ),
                })),
            ]}
          />
        </Card>
      )}
    </Card>
  );
}

function FieldsTab({
  fields,
  fieldDefMap,
}: {
  fields: CaseFieldValue[];
  fieldDefMap: Map<string, { config?: DynamicField['config'] }>;
}) {
  if (fields.length === 0) {
    return <Card>本案未填寫動態欄位。</Card>;
  }
  return (
    <Card>
      <Descriptions column={1} bordered size="small">
        {fields.map((f) => {
          const def = fieldDefMap.get(f.fieldDefinitionId);
          const dynamicField: DynamicField = {
            fieldDefinitionId: f.fieldDefinitionId,
            code: f.code,
            label: f.label,
            fieldType: f.fieldType,
            required: f.required,
            config: def?.config,
          };
          return (
            <Descriptions.Item key={f.fieldDefinitionId} label={f.label}>
              <DynamicFieldRenderer field={dynamicField} value={f.value} readonly />
            </Descriptions.Item>
          );
        })}
      </Descriptions>
    </Card>
  );
}

function NodesTab({ nodes }: { nodes: CaseNodeProgress[] }) {
  const columns: ColumnsType<CaseNodeProgress> = [
    { title: '序', dataIndex: 'nodeKey', key: 'nodeKey', width: 120, render: (v) => <Tag>{v}</Tag> },
    { title: '節點名稱', dataIndex: 'label', key: 'label', width: 160 },
    { title: '類型', dataIndex: 'nodeType', key: 'nodeType', width: 100 },
    {
      title: '狀態',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (v: CaseNodeProgress['status']) => <Tag color={NODE_STATUS_META[v].color}>{NODE_STATUS_META[v].label}</Tag>,
    },
    { title: '承辦人', dataIndex: 'assigneeName', key: 'assigneeName', width: 120 },
    { title: '必要角色', dataIndex: 'requiredRoleName', key: 'requiredRoleName', width: 120 },
    {
      title: '進入時間',
      dataIndex: 'enteredAt',
      key: 'enteredAt',
      width: 150,
      render: (v: string | null) => (v ? dayjs(v).format('YYYY-MM-DD HH:mm') : '—'),
    },
    {
      title: '完成時間',
      dataIndex: 'completedAt',
      key: 'completedAt',
      width: 150,
      render: (v: string | null) => (v ? dayjs(v).format('YYYY-MM-DD HH:mm') : '—'),
    },
    { title: '註記', dataIndex: 'comment', key: 'comment', ellipsis: true },
  ];
  return <Table<CaseNodeProgress> rowKey="nodeId" columns={columns} dataSource={nodes} pagination={false} />;
}

function ActionsTab({ actions }: { actions: CaseAction[] }) {
  if (actions.length === 0) return <Card>本案尚無動作軌跡。</Card>;
  return (
    <Card>
      <Timeline
        items={[...actions].reverse().map((a) => ({
          color: a.actionType === 'void' ? 'red' : a.actionType === 'reject' ? 'orange' : a.actionType === 'approve' ? 'green' : 'blue',
          children: (
            <Space direction="vertical" size={0}>
              <Space>
                <Tag>{ACTION_TYPE_LABEL[a.actionType] ?? a.actionType}</Tag>
                <span>{a.actorName}</span>
                <span style={{ color: '#999' }}>{dayjs(a.actionAt).format('YYYY-MM-DD HH:mm')}</span>
              </Space>
              {a.comment && <div>{a.comment}</div>}
            </Space>
          ),
        }))}
      />
    </Card>
  );
}

function RelationsTab({ relations }: { relations: CaseRelationItem[] }) {
  if (relations.length === 0) return <Card>本案無關聯案件。</Card>;
  return (
    <Card>
      <Row gutter={[16, 16]}>
        {relations.map((r) => (
          <Col span={12} key={`${r.caseId}-${r.relationType}`}>
            <Card size="small" type="inner" title={
              <Space>
                <Tag color={r.relationType === 'spawn' ? 'blue' : 'purple'}>
                  {r.relationType === 'spawn' ? '子流程' : '重開'}
                </Tag>
                <Tag>{r.iAmChild ? '來自 (parent)' : '走向 (child)'}</Tag>
                <Tag color={CASE_STATUS_META[r.status].color}>{CASE_STATUS_META[r.status].label}</Tag>
              </Space>
            }>
              <Link to={`/cases/${r.caseId}`}>
                <strong>{r.caseNumber}</strong>   {r.title}
              </Link>
            </Card>
          </Col>
        ))}
      </Row>
    </Card>
  );
}
