import { Form, Input, InputNumber, DatePicker, Switch, Select } from 'antd';
import type { Rule } from 'antd/es/form';
import dayjs from 'dayjs';
import type { ReactNode } from 'react';

/**
 * 動態欄位渲染（issue #21 [5.5]）
 *
 * 對應 issue #11 [3.1.2] 的 9 種 fieldType：
 *   text / textarea / number / date / datetime / boolean / select / multiselect / user
 *
 * 在「發起 / 編輯」模式下會渲染 Form.Item + 對應 input；
 * 在 readonly 模式下渲染純文字（給 detail 頁直接展示）。
 */

export interface DynamicField {
  fieldDefinitionId: string;
  code: string;
  label: string;
  fieldType: string;
  required: boolean;
  helpText?: string;
  /** 對齊 issue #11 [3.1.2] FieldDefinition.config，常見 keys：options / min / max / placeholder */
  config?: {
    options?: Array<{ value: string; label: string }>;
    placeholder?: string;
    min?: number;
    max?: number;
    rows?: number;
  };
}

export interface DynamicFieldRendererProps {
  field: DynamicField;
  /** form name path 前綴，預設 ['fieldValues', field.fieldDefinitionId] */
  namePath?: (string | number)[];
  /** readonly 模式只渲染值 */
  readonly?: boolean;
  /** readonly 模式下要顯示的值 */
  value?: unknown;
}

export function DynamicFieldRenderer({ field, namePath, readonly, value }: DynamicFieldRendererProps) {
  if (readonly) {
    return <ReadonlyValue field={field} value={value} />;
  }
  const path = namePath ?? ['fieldValues', field.fieldDefinitionId];
  const rules: Rule[] = field.required
    ? [{ required: true, message: `「${field.label}」為必填` }]
    : [];
  const placeholder = field.config?.placeholder;

  switch (field.fieldType) {
    case 'text':
      return (
        <Form.Item label={field.label} name={path} rules={rules} extra={field.helpText}>
          <Input placeholder={placeholder} />
        </Form.Item>
      );
    case 'textarea':
      return (
        <Form.Item label={field.label} name={path} rules={rules} extra={field.helpText}>
          <Input.TextArea rows={field.config?.rows ?? 3} placeholder={placeholder} />
        </Form.Item>
      );
    case 'number':
      return (
        <Form.Item label={field.label} name={path} rules={rules} extra={field.helpText}>
          <InputNumber min={field.config?.min} max={field.config?.max} style={{ width: '100%' }} placeholder={placeholder} />
        </Form.Item>
      );
    case 'date':
      return (
        <Form.Item label={field.label} name={path} rules={rules} extra={field.helpText}>
          <DatePicker style={{ width: '100%' }} />
        </Form.Item>
      );
    case 'datetime':
      return (
        <Form.Item label={field.label} name={path} rules={rules} extra={field.helpText}>
          <DatePicker showTime style={{ width: '100%' }} />
        </Form.Item>
      );
    case 'boolean':
      return (
        <Form.Item label={field.label} name={path} valuePropName="checked" extra={field.helpText}>
          <Switch />
        </Form.Item>
      );
    case 'select':
      return (
        <Form.Item label={field.label} name={path} rules={rules} extra={field.helpText}>
          <Select
            options={field.config?.options ?? []}
            placeholder={placeholder ?? '請選擇'}
            allowClear={!field.required}
          />
        </Form.Item>
      );
    case 'multiselect':
      return (
        <Form.Item label={field.label} name={path} rules={rules} extra={field.helpText}>
          <Select
            mode="multiple"
            options={field.config?.options ?? []}
            placeholder={placeholder ?? '請選擇 (可複選)'}
            allowClear
          />
        </Form.Item>
      );
    case 'user':
      // 暫時用 select 撐，等 issue #6 / #8 落地後改為串 listUsers 的 Select
      return (
        <Form.Item label={field.label} name={path} rules={rules} extra={field.helpText ?? '依使用者 ID 輸入；後續會改為使用者下拉'}>
          <Input placeholder={placeholder ?? 'u-xxx'} />
        </Form.Item>
      );
    default:
      return (
        <Form.Item label={field.label} name={path} rules={rules} extra={field.helpText}>
          <Input placeholder={`unsupported fieldType: ${field.fieldType}`} disabled />
        </Form.Item>
      );
  }
}

function ReadonlyValue({ field, value }: { field: DynamicField; value: unknown }): ReactNode {
  if (value == null || value === '') return <span style={{ color: '#999' }}>—</span>;
  switch (field.fieldType) {
    case 'boolean':
      return value === true || value === 'true' ? '是' : '否';
    case 'date':
      return dayjs(value as string).format('YYYY-MM-DD');
    case 'datetime':
      return dayjs(value as string).format('YYYY-MM-DD HH:mm');
    case 'select': {
      const opts = field.config?.options ?? [];
      const found = opts.find((o) => o.value === value);
      return found ? found.label : String(value);
    }
    case 'multiselect': {
      const opts = field.config?.options ?? [];
      const list = Array.isArray(value) ? value : [];
      return list.map((v) => opts.find((o) => o.value === v)?.label ?? String(v)).join('、');
    }
    case 'textarea':
      return <pre style={{ whiteSpace: 'pre-wrap', margin: 0, fontFamily: 'inherit' }}>{String(value)}</pre>;
    default:
      return String(value);
  }
}
