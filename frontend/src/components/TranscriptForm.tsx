import { Alert, Button, Form, Input, Select, Space } from 'antd';
import { SendOutlined } from '@ant-design/icons';
import { Controller, useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import type { AnalyzeRequest } from '../types';

/**
 * The "submit a transcript" form: React Hook Form manages the state,
 * Yup validates BEFORE anything is sent (the backend re-validates anyway —
 * client-side validation is for instant feedback, server-side for safety).
 * The rules mirror the backend exactly: non-empty, <= 50,000 chars, en|hy.
 */

const MAX_CHARS = 50_000;

const schema = yup.object({
    transcriptText: yup
        .string()
        .trim()
        .required('Please paste the transcript text.')
        .max(MAX_CHARS, `The transcript must be at most ${MAX_CHARS.toLocaleString()} characters.`),
    language: yup
        .mixed<'en' | 'hy'>()
        .oneOf(['en', 'hy'], 'Language must be English or Armenian.')
        .required('Please choose the transcript language.'),
});

interface Props {
    onSubmit: (request: AnalyzeRequest) => void;
    submitting: boolean;
    errorMessage?: string;
}

export function TranscriptForm({ onSubmit, submitting, errorMessage }: Props) {
    const {
        control,
        handleSubmit,
        formState: { errors },
    } = useForm<AnalyzeRequest>({
        resolver: yupResolver(schema),
        defaultValues: { transcriptText: '', language: 'en' },
    });

    return (
        <Form layout="vertical" onFinish={handleSubmit(onSubmit)} disabled={submitting}>
            <Form.Item
                label="Transcript text"
                required
                validateStatus={errors.transcriptText ? 'error' : undefined}
                help={
                    errors.transcriptText?.message ??
                    'One utterance per line. Labels like "Agent:" / "Caller:" are used when present.'
                }
            >
                <Controller
                    name="transcriptText"
                    control={control}
                    render={({ field }) => (
                        <Input.TextArea
                            {...field}
                            rows={10}
                            showCount
                            maxLength={MAX_CHARS}
                            placeholder={
                                'Agent: Hello, how can I help you?\nCaller: My name is John Smith, my phone is 555-123-4567.'
                            }
                        />
                    )}
                />
            </Form.Item>

            <Form.Item
                label="Language"
                required
                validateStatus={errors.language ? 'error' : undefined}
                help={errors.language?.message}
            >
                <Controller
                    name="language"
                    control={control}
                    render={({ field }) => (
                        <Select
                            {...field}
                            style={{ maxWidth: 260 }}
                            options={[
                                { value: 'en', label: 'English (en)' },
                                { value: 'hy', label: 'Armenian (hy)' },
                            ]}
                        />
                    )}
                />
            </Form.Item>

            <Space orientation="vertical" style={{ width: '100%' }} size="middle">
                {errorMessage && <Alert type="error" showIcon title={errorMessage} />}
                <Button
                    type="primary"
                    htmlType="submit"
                    icon={<SendOutlined />}
                    loading={submitting}
                    size="large"
                >
                    Analyze
                </Button>
            </Space>
        </Form>
    );
}
