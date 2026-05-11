import { useEffect } from 'react';
import { Drawer, Form, Button, Select, DatePicker, Space, message } from 'antd';
import { PlusOutlined, MinusCircleOutlined } from '@ant-design/icons';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import dayjs from 'dayjs';
import { rolesApi, type UserSummary, type Role, type UserRoleAssignment } from '../../api/roles';

interface Props {
  open: boolean;
  user: UserSummary;
  roles: Role[];
  onClose: () => void;
}

interface AssignmentFormItem {
  roleId: string;
  effectiveFrom?: dayjs.Dayjs;
  effectiveTo?: dayjs.Dayjs;
}

interface FormValues {
  assignments: AssignmentFormItem[];
}

export default function UserRolesAssignDrawer({ open, user, roles, onClose }: Props) {
  const [form] = Form.useForm<FormValues>();
  const queryClient = useQueryClient();

  const { data: currentAssignments = [] } = useQuery({
    queryKey: ['users', user.id, 'roles'],
    queryFn: () => rolesApi.getUserRoles(user.id),
    enabled: open,
  });

  useEffect(() => {
    if (open && currentAssignments.length > 0) {
      form.setFieldsValue({
        assignments: currentAssignments.map((a) => ({
          roleId: a.roleId,
          effectiveFrom: a.effectiveFrom ? dayjs(a.effectiveFrom) : undefined,
          effectiveTo: a.effectiveTo ? dayjs(a.effectiveTo) : undefined,
        })),
      });
    } else if (open) {
      form.setFieldsValue({ assignments: [{ roleId: '' }] });
    }
  }, [open, currentAssignments, form]);

  const assignMutation = useMutation({
    mutationFn: (assignments: UserRoleAssignment[]) =>
      rolesApi.assignUserRoles(user.id, assignments),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['users', user.id, 'roles'] });
      void message.success(`已更新 ${user.displayName} 的角色指派`);
      onClose();
    },
  });

  const handleSubmit = () => {
    void form.validateFields().then((values) => {
      const assignments: UserRoleAssignment[] = values.assignments
        .filter((a) => a.roleId)
        .map((a) => ({
          roleId: a.roleId,
          roleName: roles.find((r) => r.id === a.roleId)?.name,
          effectiveFrom: a.effectiveFrom?.toISOString(),
          effectiveTo: a.effectiveTo?.toISOString(),
        }));
      assignMutation.mutate(assignments);
    });
  };

  const roleOptions = roles
    .filter((r) => r.isActive)
    .map((r) => ({ value: r.id, label: r.name }));

  return (
    <Drawer
      title={`指派角色：${user.displayName}`}
      open={open}
      onClose={onClose}
      width={520}
      extra={
        <Button
          type="primary"
          onClick={handleSubmit}
          loading={assignMutation.isPending}
        >
          儲存
        </Button>
      }
    >
      <Form form={form} layout="vertical">
        <Form.List name="assignments">
          {(fields, { add, remove }) => (
            <>
              {fields.map(({ key, name }) => (
                <Space key={key} style={{ display: 'flex', marginBottom: 8 }} align="baseline">
                  <Form.Item
                    name={[name, 'roleId']}
                    rules={[{ required: true, message: '請選擇角色' }]}
                  >
                    <Select
                      placeholder="選擇角色"
                      options={roleOptions}
                      style={{ width: 160 }}
                    />
                  </Form.Item>
                  <Form.Item name={[name, 'effectiveFrom']}>
                    <DatePicker placeholder="生效日期" style={{ width: 130 }} />
                  </Form.Item>
                  <Form.Item name={[name, 'effectiveTo']}>
                    <DatePicker placeholder="失效日期" style={{ width: 130 }} />
                  </Form.Item>
                  <MinusCircleOutlined onClick={() => remove(name)} style={{ color: '#ff4d4f' }} />
                </Space>
              ))}
              <Form.Item>
                <Button
                  type="dashed"
                  onClick={() => add({ roleId: '' })}
                  block
                  icon={<PlusOutlined />}
                >
                  新增角色
                </Button>
              </Form.Item>
            </>
          )}
        </Form.List>
      </Form>
    </Drawer>
  );
}
