import { Empty, Modal, Spin, Tag, Timeline, Typography } from 'antd';
import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import {
  listWorkflowTemplateVersions,
  type WorkflowTemplate,
  type WorkflowTemplateVersion,
} from '../../api/workflowTemplates';
import { getNodeTypeMeta } from '../../lib/workflowNodeTypes';

dayjs.extend(relativeTime);

const { Text, Paragraph } = Typography;

export interface WorkflowTemplateVersionsModalProps {
  open: boolean;
  template: WorkflowTemplate;
  onClose: () => void;
}

/**
 * 範本版本歷史 modal。
 *
 * 驗收條件「顯示版本歷史與 PublishedAt」。
 */
export default function WorkflowTemplateVersionsModal({
  open,
  template,
  onClose,
}: WorkflowTemplateVersionsModalProps) {
  const { data: versions = [], isLoading } = useQuery({
    queryKey: ['workflow-templates', template.id, 'versions'],
    queryFn: () => listWorkflowTemplateVersions(template.id),
    enabled: open,
  });

  return (
    <Modal
      open={open}
      onCancel={onClose}
      onOk={onClose}
      okText="關閉"
      cancelButtonProps={{ style: { display: 'none' } }}
      title={`範本版本歷史：${template.name}`}
      width={720}
      destroyOnClose
    >
      {isLoading ? (
        <Spin />
      ) : versions.length === 0 ? (
        <Empty description="尚未發行過任何版本" />
      ) : (
        <Timeline
          mode="left"
          items={versions.map((v: WorkflowTemplateVersion) => ({
            color: v.version === template.version ? 'green' : 'gray',
            label: dayjs(v.publishedAt).format('YYYY-MM-DD HH:mm'),
            children: (
              <div>
                <Paragraph style={{ marginBottom: 4 }}>
                  <Tag color={v.version === template.version ? 'green' : 'blue'}>
                    v{v.version}
                  </Tag>
                  {v.version === template.version && (
                    <Tag color="green">目前生效中</Tag>
                  )}
                  <Text type="secondary" style={{ marginInlineStart: 8 }}>
                    {dayjs(v.publishedAt).fromNow()}
                    {v.publishedBy && ` · 發行人：${v.publishedBy}`}
                  </Text>
                </Paragraph>
                {v.changeNote && (
                  <Paragraph style={{ marginBottom: 4 }}>
                    <Text strong>發行說明：</Text>
                    {v.changeNote}
                  </Paragraph>
                )}
                <Paragraph style={{ marginBottom: 0 }}>
                  <Text strong>節點順序：</Text>
                  <ol style={{ paddingInlineStart: 24, marginTop: 4 }}>
                    {v.snapshot.nodes
                      .slice()
                      .sort((a, b) => a.nodeOrder - b.nodeOrder)
                      .map((node) => {
                        const meta = getNodeTypeMeta(node.nodeType);
                        return (
                          <li key={node.nodeKey}>
                            <Tag color={meta?.color ?? 'default'}>{meta?.label ?? node.nodeType}</Tag>
                            <Text>{node.label}</Text>
                            <Text type="secondary"> · {node.nodeKey}</Text>
                          </li>
                        );
                      })}
                  </ol>
                </Paragraph>
              </div>
            ),
          }))}
        />
      )}
    </Modal>
  );
}
