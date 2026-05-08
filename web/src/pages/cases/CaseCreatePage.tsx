import { useEffect, useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { App, Alert, Button, Card, Col, DatePicker, Form, Input, Row, Select, Space, Typography } from 'antd';
import { useNavigate } from 'react-router-dom';
import dayjs from 'dayjs';
import { casesApi } from '../../api/cases';
import type { CaseCreatePayload } from '../../api/cases';
import { workflowTemplatesApi } from '../../api/workflowTemplates';
import { fieldDefinitionsApi } from '../../api/fieldDefinitions';
import { DynamicFieldRenderer } from '../../components/DynamicFieldRenderer';
import type { DynamicField } from '../../components/DynamicFieldRenderer';

interface FormShape {
  templateId: string;
  documentTypeCode: string;
  title: string;
  description?: string;
  customerId?: string;
  expectedCompletionAt?: dayjs.Dayjs;
  customVersion?: string;
  initialAssigneeUserId?: string;
  fieldValues?: Record<string, unknown>;
}

export default function CaseCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { message } = App.useApp();
  const [form] = Form.useForm<FormShape>();
  const [selectedTemplateId, setSelectedTemplateId] = useState<string | undefined>();

  const { data: templates } = useQuery({
    queryKey: ['workflow-templates', 'list'],
    queryFn: () => workflowTemplatesApi.list(),
  });

  const { data: fieldDefs } = useQuery({
    queryKey: ['field-definitions', 'list'],
    queryFn: () => fieldDefinitionsApi.list(),
  });

  const selectedTemplate = useMemo(
    () => templates?.items.find((t) => t.id === selectedTemplateId),
    [templates, selectedTemplateId],
  );

  // 選定範本後自動帶入 documentTypeCode
  useEffect(() => {
    if (selectedTemplate) {
      form.setFieldsValue({ documentTypeCode: defaultDocTypeCode(selectedTemplate.code) });
    }
  }, [selectedTemplate, form]);

  const createMutation = useMutation({
    mutationFn: (payload: CaseCreatePayload) => casesApi.create(payload),
    onSuccess: (res) => {
      message.success(`案件 ${res.caseNumber} 已發起`);
      queryClient.invalidateQueries({ queryKey: ['cases'] });
      navigate(`/cases/${res.id}`);
    },
    onError: () => message.error('發起失敗'),
  });

  const onFinish = async () => {
    const v = await form.validateFields();
    const fieldValues = Object.entries(v.fieldValues ?? {}).map(([fieldDefinitionId, value]) => ({
      fieldDefinitionId,
      value: dayjs.isDayjs(value) ? value.toISOString() : value,
    }));
    createMutation.mutate({
      templateId: v.templateId,
      documentTypeCode: v.documentTypeCode,
      title: v.title,
      description: v.description,
      customerId: v.customerId ?? null,
      expectedCompletionAt: v.expectedCompletionAt ? v.expectedCompletionAt.toISOString() : null,
      customVersion: v.customVersion ?? null,
      initialAssigneeUserId: v.initialAssigneeUserId ?? null,
      fieldValues,
    });
  };

  // 打包成 DynamicField (容忍 fieldDefinitions 的型別可能未包含 helpText)
  const dynamicFields: DynamicField[] = useMemo(() => {
    return (fieldDefs?.items ?? []).map((f) => {
      const anyF = f as unknown as {
        id: string;
        code: string;
        label: string;
        fieldType: string;
        required?: boolean;
        helpText?: string;
        config?: DynamicField['config'];
      };
      return {
        fieldDefinitionId: anyF.id,
        code: anyF.code,
        label: anyF.label,
        fieldType: anyF.fieldType,
        required: anyF.required ?? false,
        helpText: anyF.helpText,
        config: anyF.config,
      };
    });
  }, [fieldDefs]);

  return (
    <div style={{ padding: 24, maxWidth: 960, margin: '0 auto' }}>
      <Space style={{ marginBottom: 16, width: '100%', justifyContent: 'space-between' }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          發起新案件
        </Typography.Title>
        <Button onClick={() => navigate(-1)}>返回</Button>
      </Space>

      <Form<FormShape>
        form={form}
        layout="vertical"
        initialValues={{ documentTypeCode: 'F01' }}
      >
        <Card size="small" title="基本資訊" style={{ marginBottom: 16 }}>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item
                label="流程範本"
                name="templateId"
                rules={[{ required: true, message: '請選擇流程範本' }]}
              >
                <Select
                  placeholder="選擇要使用的流程範本"
                  options={(templates?.items ?? []).map((t) => ({
                    value: t.id,
                    label: `${t.name} (v${t.version})`,
                  }))}
                  onChange={(v) => setSelectedTemplateId(v)}
                />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item
                label="文件類型"
                name="documentTypeCode"
                rules={[{ required: true, message: '請輸入文件類型代碼' }]}
                tooltip="依類型取號，例：F01、F03。預設由範本推斷"
              >
                <Input maxLength={6} placeholder="F01" />
              </Form.Item>
            </Col>
            <Col span={6}>
              <Form.Item label="自訂版號" name="customVersion" tooltip="可選，issue #33 [5.4.2]">
                <Input placeholder="例：SPEC-2026.05-V1" allowClear />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item
            label="案件標題"
            name="title"
            rules={[{ required: true, message: '請輸入標題' }, { max: 200 }]}
          >
            <Input placeholder="簡要描述需求" />
          </Form.Item>
          <Form.Item label="詳細描述" name="description">
            <Input.TextArea rows={3} placeholder="詳細背景 / 需求 / 限制" />
          </Form.Item>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item label="預計完成時間" name="expectedCompletionAt" tooltip="首次設定後寫入 OriginalExpectedAt；各節點修改寫入 ModifiedExpectedAt (issue #20)">
                <DatePicker showTime style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item label="指定首個處理人 (可選)" name="initialAssigneeUserId">
                <Input placeholder="u-xxx；未填則由系統依範本設定預設指派" allowClear />
              </Form.Item>
            </Col>
          </Row>
        </Card>

        <Card size="small" title="動態欄位" style={{ marginBottom: 16 }}>
          {dynamicFields.length === 0 ? (
            <Alert
              type="info"
              showIcon
              message="尚未載入欄位定義"
              description="請確定 issue #11 [3.1.2] 的 mock 或後端 API 能回傳 field-definitions。"
            />
          ) : (
            dynamicFields.map((f) => <DynamicFieldRenderer key={f.fieldDefinitionId} field={f} />)
          )}
        </Card>

        {selectedTemplate && (
          <Card size="small" title="節點預覽 (依選定範本)" style={{ marginBottom: 16 }}>
            <Space wrap>
              {(selectedTemplate.nodes ?? []).map((n) => (
                <Space key={n.nodeKey} size={2}>
                  <span style={{ color: '#999' }}>{n.label}</span>
                  <span style={{ color: '#ccc' }}>→</span>
                </Space>
              ))}
            </Space>
          </Card>
        )}

        <Space style={{ width: '100%', justifyContent: 'flex-end' }}>
          <Button onClick={() => navigate(-1)}>取消</Button>
          <Button type="primary" loading={createMutation.isPending} onClick={onFinish}>
            發起案件
          </Button>
        </Space>
      </Form>
    </div>
  );
}

function defaultDocTypeCode(templateCode: string): string {
  // 極簡化：依範本代碼推斷。實際應由範本模型帶出 documentTypeCode。
  if (templateCode.startsWith('work-request')) return 'F01';
  if (templateCode.startsWith('spec-change')) return 'F03';
  if (templateCode.startsWith('customer-feedback')) return 'F05';
  return 'F01';
}
