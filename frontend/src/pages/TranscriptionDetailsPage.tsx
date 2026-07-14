import { Button, Card, Collapse, Result, Space, Tag, Typography } from 'antd';
import { ArrowLeftOutlined } from '@ant-design/icons';
import { Link, useParams } from 'react-router-dom';
import dayjs from 'dayjs';
import { useHistoryItem } from '../hooks/useTranscripts';
import { ConversationView } from '../components/ConversationView';
import { AttributesCard } from '../components/AttributesCard';

/** Details of one saved analysis: /transcription/:id */
export function TranscriptionDetailsPage() {
    const { id } = useParams<{ id: string }>();
    const item = useHistoryItem(id);

    if (item.isLoading) return null;

    if (!item.data) {
        return (
            <Result
                status="404"
                title="Analysis not found"
                subTitle="It may have been deleted, or it was saved in a different browser."
                extra={
                    <Link to="/history">
                        <Button icon={<ArrowLeftOutlined />}>Back to history</Button>
                    </Link>
                }
            />
        );
    }

    const { request, response, createdAt } = item.data;

    return (
        <Space orientation="vertical" size="large" style={{ width: '100%' }}>
            <Space style={{ width: '100%', justifyContent: 'space-between' }}>
                <div>
                    <Typography.Title level={3} style={{ marginBottom: 4 }}>
                        {response.extractedAttributes.name ?? 'Unknown caller'}
                    </Typography.Title>
                    <Space size="small">
                        <Tag color={request.language === 'hy' ? 'volcano' : 'blue'}>
                            {request.language === 'hy' ? 'Armenian' : 'English'}
                        </Tag>
                        <Typography.Text type="secondary">
                            {dayjs(createdAt).format('MMMM D, YYYY HH:mm')}
                        </Typography.Text>
                    </Space>
                </div>
                <Link to="/history">
                    <Button icon={<ArrowLeftOutlined />}>Back to history</Button>
                </Link>
            </Space>

            <Card title="Conversation" size="small">
                <ConversationView conversation={response.conversation} />
            </Card>

            <AttributesCard attributes={response.extractedAttributes} />

            <Collapse
                items={[
                    {
                        key: 'original',
                        label: 'Original transcript text',
                        children: (
                            <Typography.Paragraph style={{ whiteSpace: 'pre-wrap', marginBottom: 0 }}>
                                {request.transcriptText}
                            </Typography.Paragraph>
                        ),
                    },
                ]}
            />
        </Space>
    );
}
