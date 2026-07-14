import { Button, Card, Divider, Empty, Popconfirm, Space, Tag, Typography } from 'antd';
import { DeleteOutlined, RightOutlined } from '@ant-design/icons';
import { Link } from 'react-router-dom';
import dayjs from 'dayjs';
import { useDeleteHistory, useHistory } from '../hooks/useTranscripts';

/** All past analyses (saved in this browser), newest first. */
export function HistoryPage() {
    const history = useHistory();
    const { removeItem, clearAll } = useDeleteHistory();
    const items = history.data ?? [];

    return (
        <Space orientation="vertical" size="large" style={{ width: '100%' }}>
            <Space style={{ width: '100%', justifyContent: 'space-between' }}>
                <div>
                    <Typography.Title level={3} style={{ marginBottom: 4 }}>
                        History
                    </Typography.Title>
                    <Typography.Text type="secondary">
                        Analyses saved in this browser ({items.length}).
                    </Typography.Text>
                </div>
                {items.length > 0 && (
                    <Popconfirm title="Delete ALL saved analyses?" onConfirm={clearAll}>
                        <Button danger icon={<DeleteOutlined />}>
                            Clear all
                        </Button>
                    </Popconfirm>
                )}
            </Space>

            <Card>
                {items.length === 0 ? (
                    <Empty description="No analyses yet — run one on the New Transcription page." />
                ) : (
                    <Space orientation="vertical" size={0} style={{ width: '100%' }}>
                        {items.map((item, idx) => {
                            const snippet =
                                item.request.transcriptText.length > 120
                                    ? `${item.request.transcriptText.slice(0, 120)}…`
                                    : item.request.transcriptText;
                            return (
                                <div key={item.id}>
                                    {idx > 0 && <Divider style={{ margin: '16px 0' }} />}
                                    <div
                                        style={{
                                            display: 'flex',
                                            alignItems: 'flex-start',
                                            justifyContent: 'space-between',
                                            gap: '16px',
                                        }}
                                    >
                                        <div style={{ flex: 1, minWidth: 0 }}>
                                            <Space size="small" style={{ marginBottom: 4, flexWrap: 'wrap' }}>
                                                <Link
                                                    to={`/transcription/${item.id}`}
                                                    style={{
                                                        fontWeight: 600,
                                                        fontSize: '15px',
                                                        color: '#1677ff',
                                                    }}
                                                >
                                                    {item.response.extractedAttributes.name ?? 'Unknown caller'}
                                                </Link>
                                                <Tag color={item.request.language === 'hy' ? 'volcano' : 'blue'}>
                                                    {item.request.language === 'hy' ? 'Armenian' : 'English'}
                                                </Tag>
                                            </Space>

                                            <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
                                                <Typography.Text type="secondary" style={{ fontSize: '12px' }}>
                                                    {dayjs(item.createdAt).format('MMM D, YYYY HH:mm')} ·{' '}
                                                    {item.response.conversation.length} turns
                                                </Typography.Text>
                                                <Typography.Text type="secondary" style={{ display: 'block', wordBreak: 'break-word' }}>
                                                    {snippet}
                                                </Typography.Text>
                                            </div>
                                        </div>

                                        <Space size="small" style={{ flexShrink: 0, alignItems: 'center' }}>
                                            <Link to={`/transcription/${item.id}`}>
                                                <Button type="link" icon={<RightOutlined />}>
                                                    Open
                                                </Button>
                                            </Link>
                                            <Popconfirm
                                                title="Delete this analysis?"
                                                onConfirm={() => removeItem(item.id)}
                                            >
                                                <Button type="link" danger icon={<DeleteOutlined />} />
                                            </Popconfirm>
                                        </Space>
                                    </div>
                                </div>
                            );
                        })}
                    </Space>
                )}
            </Card>
        </Space>
    );
}
