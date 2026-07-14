import { Button, Card, Result, Space, Typography } from 'antd';
import { HistoryOutlined } from '@ant-design/icons';
import { Link } from 'react-router-dom';
import { useAnalyze } from '../hooks/useTranscripts';
import { getApiErrorMessage } from '../api/client';
import { TranscriptForm } from '../components/TranscriptForm';
import { ConversationView } from '../components/ConversationView';
import { AttributesCard } from '../components/AttributesCard';

/**
 * Home page: paste a transcript, analyze it, see the result immediately.
 * Every successful analysis is saved to History automatically.
 */
export function NewTranscriptionPage() {
    const analyze = useAnalyze();

    return (
        <Space orientation="vertical" size="large" style={{ width: '100%' }}>
            <div>
                <Typography.Title level={3} style={{ marginBottom: 4 }}>
                    New Transcription
                </Typography.Title>
                <Typography.Text type="secondary">
                    Paste a call transcript (English or Armenian). The system splits it into Agent/Caller
                    turns and extracts personal information mentioned in it.
                </Typography.Text>
            </div>

            <Card>
                <TranscriptForm
                    onSubmit={(request) => analyze.mutate(request)}
                    submitting={analyze.isPending}
                    errorMessage={analyze.isError ? getApiErrorMessage(analyze.error) : undefined}
                />
            </Card>

            {analyze.isSuccess && (
                <>
                    <Result
                        status="success"
                        title="Analysis complete"
                        subTitle="The result was saved to your history."
                        style={{ padding: '12px 0' }}
                        extra={
                            <Link to={`/transcription/${analyze.data.id}`}>
                                <Button icon={<HistoryOutlined />}>Open in history</Button>
                            </Link>
                        }
                    />
                    <Card title="Conversation" size="small">
                        <ConversationView conversation={analyze.data.response.conversation} />
                    </Card>
                    <AttributesCard attributes={analyze.data.response.extractedAttributes} />
                </>
            )}
        </Space>
    );
}
