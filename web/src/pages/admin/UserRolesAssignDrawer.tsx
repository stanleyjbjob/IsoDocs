import { useEffect } from 'react';
import {
  Drawer,
  Form,
  Select,
  DatePicker,
  Button,
  Space,
  Typography,
  message,
  Empty,
} from 'antd';
import { MinusCircleOutlined, PlusOutlined } from '@ant-design/icons';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  assignUserRoles,
  listRoles,
  type AssignUserRolesPayload,
  type Role,
  type UserSummary,
} from '../../api/roles';

const { Title, Paragraph, Text } = Typography;

export interface UserRolesAssignDrawerProps {
  open: boolean;
  user: UserSummary | null;
  onClose: () => void;
  onSaved?: (user: UserSummary) => void;
}

interface RoleAssignmentRow {
  roleId: string;
  effectiveFrom?: Dayjs | null;
  effectiveTo?: Dayjs | null;
}

interface FormValues {
  assignments: RoleAssignmentRow[];
}

// MinusCircleOutlined / PlusOutlined 是 antd v5 的 icon，需要 @ant-design/icons。
// 這個套件在 #4 [1.2] 預裝 antd 時未預設裝，這裡我們改用簡單的 unicode/text。
// (使用者可以 npm install @ant-design/icons 後換回 icon component)

export default function UserRolesAssignDrawer({
  open,
  user,
  onClose,
  onSaved,
}: UserRolesAssignDrawerProps) {
  const [form] = Form.useForm<FormValues>();
  const queryClient = useQueryClient();

  const { data: roles } = useQuery({
    queryKey: ['roles'],
    queryFn: listRoles,
    enabled: open, // 只在 drawer 打開時才拉角色清單
  });

  // 同步表單初始值
  useEffect(() => {
    if (!open) return;
    if (user) {
      form.setFieldsValue({
        assignments: user.roles.map((r) => ({
          roleId: r.roleId,
          effectiveFrom: r.effectiveFrom ? dayjs(r.effectiveFrom) : null,
          effectiveTo: r.effectiveTo ? dayjs(r.effectiveTo) : null,
        })),
      });
    } else {
      form.setFieldsValue({ assignments: [] });
    }
  }, [open, user, form]);

  const assignMutation = useMutation({
    mutationFn: ({ userId, payload }: { userId: string; payload: AssignUserRolesPayload }) =>
      assignUserRoles(userId, payload),
    onSuccess: (updated) => {
      message.success(`已更新「${updated.displayName}」的角色指派`);
      void queryClient.invalidateQueries({ queryKey: ['users'] });
      // 達成驗收條件「權限異動即時生效」：被指派者下次拉 effective 時拿到新角色
      void queryClient.invalidateQueries({ queryKey: ['roles', 'effective'] });
      onSaved?.(updated);
    },
    onError: () => message.error('指派角色失敗'),
  });

  const handleSubmit = async () => {
    if (!user) return;
    try {
      const values = await form.validateFields();
      const payload: AssignUserRolesPayload = {
        roles: (values.assignments ?? []).map((row) => ({
          roleId: row.roleId,
          effectiveFrom: row.effectiveFrom ? row.effectiveFrom.toISOString() : null,
          effectiveTo: row.effectiveTo ? row.effectiveTo.toISOString() : null,
        })),
      };
      await assignMutation.mutateAsync({ userId: user.id, payload });
    } catch (err) {
      // eslint-disable-next-line no-console
      console.debug('Form validation failed:', err);
    }
  };

  // 只列出 active 角色供選擇（被停用的不能新指派）
  const activeRoles: Role[] = (roles ?? []).filter((r) => r.isActive);

  return (
    <Drawer
      title={user ? `指派角色：${user.displayName}` : '指派角色'}
      width={720}
      open={open}
      onClose={onClose}
      destroyOnClose
      extra={
        <Space>
          <Button onClick={onClose}>取消</Button>
          <Button type="primary" loading={assignMutation.isPending} onClick={handleSubmit}>
            儲存
          </Button>
        </Space>
      }
    >
      <Title level={5} style={{ marginTop: 0 }}>
        複合角色指派
      </Title>
      <Paragraph type="secondary">
        可一次指派多個角色。每個指派可独立設定生效起始與結束日期，
        未設起始日期表示「即日生效」，未設結束日期表示「無到期」。
      </Paragraph>

      <Form<FormValues> form={form} layout="vertical">
        <Form.List name="assignments">
          {(fields, { add, remove }) => (
            <div>
              {fields.length === 0 && (
                <Empty description="這個使用者尚未被指派任何角色" style={{ marginBlock: 24 }} />
              )}
              {fields.map((field, index) => (
                <div
                  key={field.key}
                  style={{
                    display: 'flex',
                    gap: 8,
                    alignItems: 'flex-start',
                    paddingBlock: 8,
                    borderBottom: '1px solid #f0f0f0',
                  }}
                >
                  <Form.Item
                    {...field}
                    name={[field.name, 'roleId']}
                    label={index === 0 ? '角色' : undefined}
                    rules={[{ required: true, message: '請選擇角色' }]}
                    style={{ flex: 2, marginBottom: 8 }}
                  >
                    <Select
                      placeholder="選擇角色"
                      options={activeRoles.map((r) => ({
                        label: r.name,
                        value: r.id,
                      }))}
                      showSearch
                      filterOption={(input, option) =>
                        (option?.label ?? '').toString().toLowerCase().includes(input.toLowerCase())
                      }
                    />
                  </Form.Item>
                  <Form.Item
                    {...field}
                    name={[field.name, 'effectiveFrom']}
                    label={index === 0 ? '生效起' : undefined}
                    style={{ flex: 1.5, marginBottom: 8 }}
                  >
                    <DatePicker style={{ width: '100%' }} placeholder="即日生效" />
                  </Form.Item>
                  <Form.Item
                    {...field}
                    name={[field.name, 'effectiveTo']}
                    label={index === 0 ? '生效訖' : undefined}
                    style={{ flex: 1.5, marginBottom: 8 }}
                  >
                    <DatePicker style={{ width: '100%' }} placeholder="無到期" />
                  </Form.Item>
                  <div style={{ paddingTop: index === 0 ? 30 : 6 }}>
                    <Button
                      type="text"
                      danger
                      icon={<MinusCircleOutlined />}
                      aria-label="移除這一項"
                      onClick={() => remove(field.name)}
                    />
                  </div>
                </div>
              ))}
              <Button
                type="dashed"
                onClick={() => add({ roleId: '', effectiveFrom: null, effectiveTo: null })}
                icon={<PlusOutlined />}
                style={{ width: '100%', marginTop: 12 }}
              >
                新增角色指派
              </Button>
              <Text type="secondary" style={{ display: 'block', marginTop: 12, fontSize: 12 }}>
                提示：被停用的角色不會出現在選單中。如果使用者原本已被指派一個現在已停用的角色，會仍然顯示但識別為「無法選」。
              </Text>
            </div>
          )}
        </Form.List>
      </Form>
    </Drawer>
  );
}
