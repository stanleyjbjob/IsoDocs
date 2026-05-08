import { useState } from 'react';
import { Alert, Form, Input, Modal, Tag, Typography, message } from 'antd';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import {
  publishWorkflowTemplate,
  type WorkflowTemplate,
} from '../../api/workflowTemplates';
import { getNodeTypeMeta } from '../../lib/workflowNodeTypes';

const { Paragraph, Text } = Typography;

export interface WorkflowTemplatePublishConfirmModalProps {
  open: boolean;
  template: WorkflowTemplate;
  onClose: () => void;
  onPublished: () => void;
}

/**
 * 發行新版本確認對話框 (issue #13 驗收條件「發行新版本確認流程」)。
 *
 * 設計重點：
 * - 顯示即將被凍結為 TemplateVersion 的節點順序與類型
 * - 提醒「仅套用新案件、進行中案件不受影響」
 * - 可選填寫 changeNote 作為稽核記錄
 */
export default function WorkflowTemplatePublishConfirmModal({
  open,
  template,
  onClose,
  onPublished,
}: WorkflowTemplatePublishConfirmModalProps) {
  const queryClient = useQueryClient();
  const [form] = Form.useForm<{ changeNote?: string }>();
  const [submitting, setSubmitting] = useState(false);

  const publishMutation = useMutation({
    mutationFn: (changeNote?: string) =>
      publishWorkflowTemplate(template.id, { changeNote }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['workflow-templates'] });
      void queryClient.invalidateQueries({ queryKey: ['workflow-templates', template.id] });
      void message.success(`已發行範本 v${template.version + 1}`);
      onPublished();
    },
    onError: (err) => {
      void message.error(
        `發行失敗：${err instanceof Error ? err.message : '未知錯誤'}`,
      );
    },
  });

  const handleOk = async () => {
    try {
      setSubmitting(true);
      const values = await form.validateFields();
      await publishMutation.mutateAsync(values.changeNote);
    } finally {
      setSubmitting(false);
    }
  };

  const nextVersion = template.version + 1;

  return (
    <Modal
      open={open}
      title={`發行新版本 v${nextVersion}：${template.name}`}
      onCancel={onClose}
      onOk={handleOk}
      okText={`確認發行 v${nextVersion}`}
      cancelText="取消"
      confirmLoading={submitting}
      width={680}
      destroyOnClose
    >
      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="發行後的影響範圍"
        description={
          <ul style={{ paddingInlineStart: 18, marginBottom: 0 }}>
            <li>新建立的案件會使用 v{nextVersion} 為出發點</li>
            <li>進行中與已結案的案件不受影響（仍使用他們建立時的版本）</li>
            <li>本次發行會寫入稽核軌跡 (WorkflowTemplateVersion)，不可刪除</li>
          </ul>
        }
      />

      <Paragraph>這個版本中的節點清單：</Paragraph>
      <ol style={{ paddingInlineStart: 24 }}>
        {template.nodes
          .slice()
          .sort((a, b) => a.nodeOrder - b.nodeOrder)
          .map((node) => {
            const meta = getNodeTypeMeta(node.nodeType);
            return (
              <li key={node.nodeKey} style={{ marginBottom: 6 }}>
                <Tag color={meta?.color ?? 'default'}>{meta?.label ?? node.nodeType}</Tag>
                <Text strong>{node.label}</Text>
                <Text type="secondary"> · {node.nodeKey}</Text>
                {node.requiredRoleId && (
                  <Text type="secondary"> · 角色：{node.requiredRoleId}</Text>
                )}
              </li>
            );
          })}
      </ol>

      <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
        <Form.Item
          label="發行說明 (changeNote)"
          name="changeNote"
          tooltip="記錄這次發行的重點變動，依後可在版本歷史看到"
        >
          <Input.TextArea rows={3} maxLength={500} placeholder="例如：加入 PM 核准結案節點" />
        </Form.Item>
      </Form>
    </Modal>
  );
}
