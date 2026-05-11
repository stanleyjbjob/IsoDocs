import { useEffect, useMemo } from 'react';
import {
  Drawer,
  Form,
  Input,
  Button,
  Space,
  Switch,
  Tree,
  Typography,
  message,
  Tag,
  Divider,
} from 'antd';
import type { DataNode } from 'antd/es/tree';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  PERMISSION_CATEGORIES,
  groupPermissionsByCategory,
  type PermissionCategoryKey,
} from '../../lib/permissions';
import {
  createRole,
  updateRole,
  type Role,
  type RoleCreatePayload,
  type RoleUpdatePayload,
} from '../../api/roles';

const { Title, Paragraph, Text } = Typography;

export type RoleEditDrawerMode = 'create' | 'edit';

export interface RoleEditDrawerProps {
  open: boolean;
  mode: RoleEditDrawerMode;
  /** mode='edit' 時必填 */
  role?: Role | null;
  onClose: () => void;
  onSaved?: (role: Role) => void;
}

interface RoleFormValues {
  name: string;
  description?: string;
  permissions: string[];
  isActive: boolean;
}

function buildPermissionTree(): DataNode[] {
  const grouped = groupPermissionsByCategory();
  return PERMISSION_CATEGORIES.map((cat) => {
    const items = grouped[cat.key as PermissionCategoryKey];
    return {
      key: `cat:${cat.key}`,
      title: (
        <Space>
          <Text strong>{cat.label}</Text>
          <Tag>{items.length}</Tag>
        </Space>
      ),
      selectable: false,
      children: items.map((p) => ({
        key: p.key,
        title: (
          <Space size={4}>
            <Text>{p.label}</Text>
            <Text type="secondary" code style={{ fontSize: 12 }}>
              {p.key}
            </Text>
          </Space>
        ),
      })),
    };
  });
}

export default function RoleEditDrawer({
  open,
  mode,
  role,
  onClose,
  onSaved,
}: RoleEditDrawerProps) {
  const [form] = Form.useForm<RoleFormValues>();
  const queryClient = useQueryClient();

  const treeData = useMemo(() => buildPermissionTree(), []);

  // 打開 drawer 時同步表單初始值
  useEffect(() => {
    if (!open) return;
    if (mode === 'edit' && role) {
      form.setFieldsValue({
        name: role.name,
        description: role.description,
        permissions: role.permissions,
        isActive: role.isActive,
      });
    } else {
      form.setFieldsValue({
        name: '',
        description: '',
        permissions: [],
        isActive: true,
      });
    }
  }, [open, mode, role, form]);

  const createMutation = useMutation({
    mutationFn: (payload: RoleCreatePayload) => createRole(payload),
    onSuccess: (newRole) => {
      message.success(`已建立角色：${newRole.name}`);
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
      void queryClient.invalidateQueries({ queryKey: ['roles', 'effective'] });
      onSaved?.(newRole);
    },
    onError: () => message.error('建立角色失敗'),
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, payload }: { id: string; payload: RoleUpdatePayload }) =>
      updateRole(id, payload),
    onSuccess: (updated) => {
      message.success(`已更新角色：${updated.name}`);
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
      void queryClient.invalidateQueries({ queryKey: ['roles', 'effective'] });
      onSaved?.(updated);
    },
    onError: () => message.error('更新角色失敗'),
  });

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (mode === 'create') {
        await createMutation.mutateAsync({
          name: values.name,
          description: values.description,
          permissions: values.permissions ?? [],
        });
      } else if (role) {
        await updateMutation.mutateAsync({
          id: role.id,
          payload: {
            name: values.name,
            description: values.description,
            permissions: values.permissions ?? [],
            isActive: values.isActive,
          },
        });
      }
    } catch (err) {
      // form validation error 會丟在這邊；antd Form 會自動顯示錯誤不需多作
      // eslint-disable-next-line no-console
      console.debug('Form validation failed:', err);
    }
  };

  const submitting = createMutation.isPending || updateMutation.isPending;

  return (
    <Drawer
      title={mode === 'create' ? '建立角色' : `編輯角色：${role?.name ?? ''}`}
      width={640}
      open={open}
      onClose={onClose}
      destroyOnClose
      extra={
        <Space>
          <Button onClick={onClose}>取消</Button>
          <Button type="primary" loading={submitting} onClick={handleSubmit}>
            儲存
          </Button>
        </Space>
      }
    >
      <Form<RoleFormValues> form={form} layout="vertical" requiredMark>
        <Form.Item
          name="name"
          label="角色名稱"
          rules={[
            { required: true, message: '請輸入角色名稱' },
            { max: 64, message: '名稱不超過 64 字元' },
          ]}
        >
          <Input placeholder="例：顧問、開發人員" />
        </Form.Item>
        <Form.Item
          name="description"
          label="說明"
          rules={[{ max: 256, message: '說明不超過 256 字元' }]}
        >
          <Input.TextArea placeholder="這個角色負責什麼任務。" autoSize={{ minRows: 2, maxRows: 4 }} />
        </Form.Item>
        {mode === 'edit' && (
          <Form.Item name="isActive" label="是否啟用" valuePropName="checked">
            <Switch />
          </Form.Item>
        )}

        <Divider orientation="left" style={{ marginTop: 8 }}>
          <Title level={5} style={{ margin: 0 }}>
            權限設定
          </Title>
        </Divider>
        <Paragraph type="secondary">
          可勾選多個權限；背後以 JSON 字串陣列儲存。存檔後該角色的使用者會即時取得新權限。
        </Paragraph>
        <Form.Item
          name="permissions"
          valuePropName="checkedKeys"
          trigger="onCheck"
          getValueFromEvent={(checked: { checked: string[] } | string[]) => {
            // antd Tree 在 checkStrictly=true 時，oncheck 的參數是物件 { checked, halfChecked }
            // 這裡我們不用 strict mode，所以參數是 checkedKeys: Key[]
            if (Array.isArray(checked)) {
              return checked.map(String).filter((k) => !k.startsWith('cat:'));
            }
            // 保險丝：如果是 { checked }
            return (checked.checked ?? []).map(String).filter((k) => !k.startsWith('cat:'));
          }}
        >
          <Tree
            checkable
            selectable={false}
            defaultExpandAll
            treeData={treeData}
          />
        </Form.Item>
      </Form>
    </Drawer>
  );
}
