import { useEffect, useState } from 'react';
import { Drawer, Form, Input, Switch, Button, Tree, message } from 'antd';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { DataNode } from 'antd/es/tree';
import type { Key } from 'react';
import { PERMISSION_CATEGORIES, type PermissionCode } from '../../lib/permissions';
import { rolesApi, type Role } from '../../api/roles';

interface Props {
  open: boolean;
  role: Role | null;
  onClose: () => void;
}

interface FormValues {
  name: string;
  description: string;
  isActive: boolean;
}

const treeData: DataNode[] = PERMISSION_CATEGORIES.map((cat) => ({
  title: cat.label,
  key: cat.key,
  children: cat.permissions.map((p) => ({
    title: p.label,
    key: p.code,
  })),
}));

const leafKeys = PERMISSION_CATEGORIES.flatMap((c) => c.permissions.map((p) => p.code));

export default function RoleEditDrawer({ open, role, onClose }: Props) {
  const [form] = Form.useForm<FormValues>();
  const [checkedPermissions, setCheckedPermissions] = useState<PermissionCode[]>([]);
  const queryClient = useQueryClient();
  const isEdit = !!role;

  useEffect(() => {
    if (open) {
      form.setFieldsValue({
        name: role?.name ?? '',
        description: role?.description ?? '',
        isActive: role?.isActive ?? true,
      });
      setCheckedPermissions(role?.permissions ?? []);
    }
  }, [open, role, form]);

  const createMutation = useMutation({
    mutationFn: (values: FormValues) =>
      rolesApi.create({
        name: values.name,
        description: values.description,
        permissions: checkedPermissions,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
      void message.success('角色已建立');
      onClose();
    },
  });

  const updateMutation = useMutation({
    mutationFn: (values: FormValues) =>
      rolesApi.update(role!.id, {
        name: values.name,
        description: values.description,
        permissions: checkedPermissions,
        isActive: values.isActive,
      }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['roles'] });
      void queryClient.invalidateQueries({ queryKey: ['roles', 'effective'] });
      void message.success('角色已更新');
      onClose();
    },
  });

  const handleSubmit = () => {
    void form.validateFields().then((values) => {
      if (isEdit) {
        updateMutation.mutate(values);
      } else {
        createMutation.mutate(values);
      }
    });
  };

  const handleCheck = (checked: Key[] | { checked: Key[]; halfChecked: Key[] }) => {
    const keys = Array.isArray(checked) ? checked : checked.checked;
    const permKeys = (keys as string[]).filter((k) => leafKeys.includes(k as PermissionCode));
    setCheckedPermissions(permKeys as PermissionCode[]);
  };

  return (
    <Drawer
      title={isEdit ? '編輯角色' : '新增角色'}
      open={open}
      onClose={onClose}
      width={480}
      extra={
        <Button
          type="primary"
          onClick={handleSubmit}
          loading={createMutation.isPending || updateMutation.isPending}
        >
          儲存
        </Button>
      }
    >
      <Form form={form} layout="vertical">
        <Form.Item
          name="name"
          label="角色名稱"
          rules={[{ required: true, message: '請輸入角色名稱' }]}
        >
          <Input maxLength={64} />
        </Form.Item>
        <Form.Item name="description" label="描述">
          <Input.TextArea maxLength={256} rows={2} />
        </Form.Item>
        {isEdit && (
          <Form.Item name="isActive" label="啟用狀態" valuePropName="checked">
            <Switch />
          </Form.Item>
        )}
        <Form.Item label={`權限設定（已選 ${checkedPermissions.length} 項）`}>
          <Tree
            checkable
            treeData={treeData}
            checkedKeys={checkedPermissions}
            onCheck={handleCheck}
            defaultExpandAll
          />
        </Form.Item>
      </Form>
    </Drawer>
  );
}
