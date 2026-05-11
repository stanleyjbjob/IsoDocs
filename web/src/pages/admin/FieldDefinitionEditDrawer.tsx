import { useEffect } from 'react';
import {
  Drawer,
  Form,
  Input,
  Select,
  Switch,
  Button,
  Space,
  message,
  Alert,
  InputNumber,
  Divider,
  Typography,
} from 'antd';
import { MinusCircleOutlined, PlusOutlined } from '@ant-design/icons';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  createFieldDefinition,
  updateFieldDefinition,
  type FieldDefinition,
  type FieldDefinitionCreatePayload,
} from '../../api/fieldDefinitions';
import { FIELD_TYPES, findFieldType, type FieldType, type FieldConfig } from '../../lib/fieldTypes';

const { Text } = Typography;

export type FieldDefinitionEditDrawerMode = 'create' | 'edit';

interface Props {
  open: boolean;
  mode: FieldDefinitionEditDrawerMode;
  field: FieldDefinition | null;
  onClose: () => void;
  onSaved: () => void;
}

interface FormValues {
  code: string;
  label: string;
  description?: string;
  fieldType: FieldType;
  isRequired: boolean;
  isActive: boolean;
  changeNote?: string;
  // type-specific config（直接攤平到 form values 方便 antd Form.List）
  maxLength?: number;
  rows?: number;
  min?: number;
  max?: number;
  precision?: number;
  format?: string;
  allowCustom?: boolean;
  options?: Array<{ value: string; label: string }>;
}

/**
 * 欄位建立 / 編輯 drawer。
 *
 * 注意：
 * - `code` 與 `fieldType` 在 edit mode 為 readonly（一旦建立不可改），避免破壞既有案件資料相容性
 * - edit mode 顯示警告 Alert：「異動會建立新版本，不影響進行中或歷史案件」
 * - select / multiselect 用 antd Form.List 動態增減 options
 */
export default function FieldDefinitionEditDrawer({
  open,
  mode,
  field,
  onClose,
  onSaved,
}: Props) {
  const queryClient = useQueryClient();
  const [form] = Form.useForm<FormValues>();

  const fieldType = Form.useWatch('fieldType', form);
  const typeDef = fieldType ? findFieldType(fieldType) : null;
  const configKeys = typeDef?.configKeys ?? [];

  // open 時填入初始值
  useEffect(() => {
    if (!open) return;
    if (mode === 'edit' && field) {
      form.setFieldsValue({
        code: field.code,
        label: field.label,
        description: field.description,
        fieldType: field.fieldType,
        isRequired: field.isRequired,
        isActive: field.isActive,
        changeNote: undefined,
        maxLength: field.config?.maxLength,
        rows: field.config?.rows,
        min: field.config?.min,
        max: field.config?.max,
        precision: field.config?.precision,
        format: field.config?.format,
        allowCustom: field.config?.allowCustom,
        options: field.config?.options ? [...field.config.options] : undefined,
      });
    } else {
      form.resetFields();
      form.setFieldsValue({
        fieldType: 'text',
        isRequired: false,
        isActive: true,
      });
    }
  }, [open, mode, field, form]);

  function buildConfigFromForm(values: FormValues): FieldConfig | undefined {
    const cfg: FieldConfig = {};
    let hasAny = false;
    if (configKeys.includes('maxLength') && values.maxLength != null) {
      cfg.maxLength = values.maxLength;
      hasAny = true;
    }
    if (configKeys.includes('rows') && values.rows != null) {
      cfg.rows = values.rows;
      hasAny = true;
    }
    if (configKeys.includes('min') && values.min != null) {
      cfg.min = values.min;
      hasAny = true;
    }
    if (configKeys.includes('max') && values.max != null) {
      cfg.max = values.max;
      hasAny = true;
    }
    if (configKeys.includes('precision') && values.precision != null) {
      cfg.precision = values.precision;
      hasAny = true;
    }
    if (configKeys.includes('format') && values.format) {
      cfg.format = values.format;
      hasAny = true;
    }
    if (configKeys.includes('allowCustom') && values.allowCustom != null) {
      cfg.allowCustom = values.allowCustom;
      hasAny = true;
    }
    if (configKeys.includes('options') && Array.isArray(values.options)) {
      const opts = values.options.filter((o) => o && o.value);
      if (opts.length > 0) {
        cfg.options = opts;
        hasAny = true;
      }
    }
    return hasAny ? cfg : undefined;
  }

  const createMutation = useMutation({
    mutationFn: (payload: FieldDefinitionCreatePayload) => createFieldDefinition(payload),
    onSuccess: () => {
      message.success('欄位已建立');
      void queryClient.invalidateQueries({ queryKey: ['field-definitions'] });
      onSaved();
    },
    onError: (err) => {
      // eslint-disable-next-line no-console
      console.error(err);
      message.error('建立欄位失敗');
    },
  });

  const updateMutation = useMutation({
    mutationFn: (vars: { id: string; payload: Record<string, unknown> }) =>
      updateFieldDefinition(vars.id, vars.payload as never),
    onSuccess: () => {
      message.success('欄位已更新，新版本已建立');
      void queryClient.invalidateQueries({ queryKey: ['field-definitions'] });
      void queryClient.invalidateQueries({ queryKey: ['field-versions'] });
      onSaved();
    },
    onError: (err) => {
      // eslint-disable-next-line no-console
      console.error(err);
      message.error('更新欄位失敗');
    },
  });

  const handleSubmit = async () => {
    const values = await form.validateFields();
    const config = buildConfigFromForm(values);
    if (mode === 'create') {
      createMutation.mutate({
        code: values.code,
        label: values.label,
        description: values.description,
        fieldType: values.fieldType,
        isRequired: values.isRequired,
        config,
      });
    } else if (field) {
      updateMutation.mutate({
        id: field.id,
        payload: {
          label: values.label,
          description: values.description,
          isRequired: values.isRequired,
          isActive: values.isActive,
          config,
          changeNote: values.changeNote,
        },
      });
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <Drawer
      title={mode === 'create' ? '建立自訂欄位' : `編輯欄位：${field?.label ?? ''}`}
      width={560}
      open={open}
      onClose={onClose}
      destroyOnClose
      footer={
        <Space style={{ float: 'right' }}>
          <Button onClick={onClose}>取消</Button>
          <Button type="primary" loading={isPending} onClick={handleSubmit}>
            {mode === 'create' ? '建立' : '儲存（建立新版本）'}
          </Button>
        </Space>
      }
    >
      {mode === 'edit' && (
        <Alert
          type="warning"
          showIcon
          message="異動會自動建立新版本快照"
          description="進行中與歷史案件會繼續使用建立當時的欄位版本，因此你的修改不會影響既有紀錄。「修改說明」會寫入版本歷史供稽核。"
          style={{ marginBottom: 16 }}
        />
      )}

      <Form<FormValues> form={form} layout="vertical">
        <Form.Item
          label="識別碼 (code)"
          name="code"
          rules={[
            { required: true, message: '請輸入欄位識別碼' },
            {
              pattern: /^[a-z][a-z0-9_.]*$/,
              message: '只能用小寫英文、數字、底線、點，且須以英文開頭',
            },
          ]}
          extra="Machine-readable 識別碼，例如 case.priority。建立後不可修改。"
        >
          <Input placeholder="case.priority" disabled={mode === 'edit'} />
        </Form.Item>

        <Form.Item
          label="欄位顯示名稱"
          name="label"
          rules={[{ required: true, message: '請輸入顯示名稱' }, { max: 64 }]}
        >
          <Input placeholder="優先級" />
        </Form.Item>

        <Form.Item label="說明" name="description" rules={[{ max: 256 }]}>
          <Input.TextArea rows={2} placeholder="（選填）給管理者看的說明" />
        </Form.Item>

        <Form.Item
          label="欄位類型"
          name="fieldType"
          rules={[{ required: true, message: '請選擇欄位類型' }]}
          extra={mode === 'edit' ? '建立後不可修改類型，需要更換請建立新欄位' : undefined}
        >
          <Select
            disabled={mode === 'edit'}
            options={FIELD_TYPES.map((t) => ({
              value: t.key,
              label: `${t.label}（${t.description}）`,
            }))}
          />
        </Form.Item>

        <Form.Item label="必填" name="isRequired" valuePropName="checked">
          <Switch />
        </Form.Item>

        {mode === 'edit' && (
          <Form.Item label="啟用狀態" name="isActive" valuePropName="checked">
            <Switch />
          </Form.Item>
        )}

        {/* ===== 類型特定 config ===== */}
        {configKeys.length > 0 && (
          <>
            <Divider plain>
              <Text type="secondary">{typeDef?.label} 進階設定</Text>
            </Divider>

            {configKeys.includes('maxLength') && (
              <Form.Item label="最大字數" name="maxLength">
                <InputNumber min={1} max={10000} placeholder="（選填）" style={{ width: '100%' }} />
              </Form.Item>
            )}
            {configKeys.includes('rows') && (
              <Form.Item label="顯示列數" name="rows">
                <InputNumber min={2} max={20} placeholder="預設 4" style={{ width: '100%' }} />
              </Form.Item>
            )}
            {configKeys.includes('min') && (
              <Form.Item label="最小值" name="min">
                <InputNumber placeholder="（選填）" style={{ width: '100%' }} />
              </Form.Item>
            )}
            {configKeys.includes('max') && (
              <Form.Item label="最大值" name="max">
                <InputNumber placeholder="（選填）" style={{ width: '100%' }} />
              </Form.Item>
            )}
            {configKeys.includes('precision') && (
              <Form.Item
                label="小數位數"
                name="precision"
                tooltip="0 = 整數；2 = 兩位小數"
              >
                <InputNumber min={0} max={10} placeholder="預設 0" style={{ width: '100%' }} />
              </Form.Item>
            )}
            {configKeys.includes('format') && (
              <Form.Item label="日期格式" name="format">
                <Input placeholder="YYYY-MM-DD" />
              </Form.Item>
            )}
            {configKeys.includes('allowCustom') && (
              <Form.Item
                label="允許自訂值"
                name="allowCustom"
                valuePropName="checked"
                tooltip="允許使用者輸入清單以外的自訂選項"
              >
                <Switch />
              </Form.Item>
            )}
            {configKeys.includes('options') && (
              <Form.Item label="選項清單" required>
                <Form.List
                  name="options"
                  rules={[
                    {
                      validator: async (_rule, options) => {
                        if (!options || options.length === 0) {
                          throw new Error('至少要有一個選項');
                        }
                      },
                    },
                  ]}
                >
                  {(items, { add, remove }, { errors }) => (
                    <>
                      {items.map((item) => (
                        <Space key={item.key} align="baseline" style={{ display: 'flex' }}>
                          <Form.Item
                            {...item}
                            name={[item.name, 'value']}
                            rules={[{ required: true, message: 'value 必填' }]}
                            style={{ flex: 1, marginBottom: 8 }}
                          >
                            <Input placeholder="value" />
                          </Form.Item>
                          <Form.Item
                            {...item}
                            name={[item.name, 'label']}
                            rules={[{ required: true, message: 'label 必填' }]}
                            style={{ flex: 1, marginBottom: 8 }}
                          >
                            <Input placeholder="顯示文字" />
                          </Form.Item>
                          <MinusCircleOutlined onClick={() => remove(item.name)} />
                        </Space>
                      ))}
                      <Form.Item>
                        <Button
                          type="dashed"
                          onClick={() => add({ value: '', label: '' })}
                          block
                          icon={<PlusOutlined />}
                        >
                          新增選項
                        </Button>
                        <Form.ErrorList errors={errors} />
                      </Form.Item>
                    </>
                  )}
                </Form.List>
              </Form.Item>
            )}
          </>
        )}

        {mode === 'edit' && (
          <>
            <Divider plain>
              <Text type="secondary">版本紀錄</Text>
            </Divider>
            <Form.Item
              label="修改說明"
              name="changeNote"
              extra="（選填）寫入版本歷史，方便日後追溯為何修改"
              rules={[{ max: 200 }]}
            >
              <Input.TextArea rows={2} placeholder="例如：加入「緊急」選項、改為必填" />
            </Form.Item>
          </>
        )}
      </Form>
    </Drawer>
  );
}
