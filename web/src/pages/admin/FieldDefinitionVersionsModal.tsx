import {
  Modal,
  Timeline,
  Tag,
  Spin,
  Empty,
  Typography,
  Collapse,
  Space,
  Tooltip,
} from 'antd';
import { useQuery } from '@tanstack/react-query';
import dayjs from 'dayjs';
import relativeTime from 'dayjs/plugin/relativeTime';
import {
  listFieldDefinitionVersions,
  type FieldDefinition,
  type FieldDefinitionVersion,
} from '../../api/fieldDefinitions';
import { findFieldType } from '../../lib/fieldTypes';

dayjs.extend(relativeTime);

const { Text, Paragraph } = Typography;

interface Props {
  open: boolean;
  field: FieldDefinition | null;
  onClose: () => void;
}

/**
 * 欄位版本歷史 Modal（issue #11 [3.1.2] 驗收條件「顯示欄位版本歷史」）。
 *
 * 用 antd Timeline 倒序列出所有版本，每筆顯示 version / 時間 / 修改者 / 異動摘要 / 完整 snapshot JSON（collapsible）。
 */
export default function FieldDefinitionVersionsModal({ open, field, onClose }: Props) {
  const { data: versions, isLoading } = useQuery({
    queryKey: ['field-versions', field?.id],
    queryFn: () => listFieldDefinitionVersions(field!.id),
    enabled: open && field !== null,
  });

  return (
    <Modal
      title={
        <Space>
          <span>版本歷史</span>
          {field && <Tag color="blue">{field.label}</Tag>}
          {field && <code style={{ fontSize: 12, color: '#888' }}>{field.code}</code>}
        </Space>
      }
      width={760}
      open={open}
      onCancel={onClose}
      footer={null}
      destroyOnClose
    >
      {isLoading ? (
        <div style={{ textAlign: 'center', padding: 24 }}>
          <Spin />
        </div>
      ) : !versions || versions.length === 0 ? (
        <Empty description="尚無版本紀錄" />
      ) : (
        <Timeline
          mode="left"
          items={versions.map((v) => ({
            color: v.version === field?.version ? 'green' : 'blue',
            label: (
              <Space direction="vertical" size={0} align="end">
                <Text strong>v{v.version}</Text>
                <Tooltip title={dayjs(v.createdAt).format('YYYY-MM-DD HH:mm:ss')}>
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {dayjs(v.createdAt).fromNow()}
                  </Text>
                </Tooltip>
              </Space>
            ),
            children: <VersionEntry version={v} isLatest={v.version === field?.version} />,
          }))}
        />
      )}
    </Modal>
  );
}

function VersionEntry({
  version,
  isLatest,
}: {
  version: FieldDefinitionVersion;
  isLatest: boolean;
}) {
  const snap = version.snapshot;
  const typeDef = findFieldType(snap.fieldType);
  return (
    <Space direction="vertical" size={4} style={{ width: '100%' }}>
      <Space wrap>
        {isLatest && <Tag color="green">目前版本</Tag>}
        <Tag color="blue">{typeDef?.label ?? snap.fieldType}</Tag>
        {snap.isRequired ? <Tag color="red">必填</Tag> : <Tag>選填</Tag>}
        {!snap.isActive && <Tag color="default">已停用</Tag>}
      </Space>
      <Text strong>{snap.label}</Text>
      {snap.description && (
        <Paragraph type="secondary" style={{ margin: 0 }}>
          {snap.description}
        </Paragraph>
      )}
      {version.changeNote && (
        <Paragraph style={{ margin: 0 }}>
          <Text type="warning">異動說明：</Text>
          {version.changeNote}
        </Paragraph>
      )}
      {version.createdBy && (
        <Text type="secondary" style={{ fontSize: 12 }}>
          修改者：{version.createdBy}
        </Text>
      )}
      {snap.config && Object.keys(snap.config).length > 0 && (
        <Collapse
          size="small"
          style={{ marginTop: 4 }}
          items={[
            {
              key: 'config',
              label: '此版本 config 設定',
              children: (
                <pre style={{ margin: 0, fontSize: 12 }}>
                  {JSON.stringify(snap.config, null, 2)}
                </pre>
              ),
            },
          ]}
        />
      )}
    </Space>
  );
}
