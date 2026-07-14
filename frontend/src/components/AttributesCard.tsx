import { Card, Descriptions, Tag, Typography } from 'antd';
import {
    EnvironmentOutlined,
    IdcardOutlined,
    MailOutlined,
    PhoneOutlined,
    UserOutlined,
} from '@ant-design/icons';
import type { ReactNode } from 'react';
import type { ExtractedAttributes } from '../types';

/**
 * The extracted PII attributes. null = "the attribute was not mentioned
 * in the transcript" (the backend never sends empty strings).
 */

function Value({ value }: { value: string | null }) {
    if (value === null || value === '') {
        return <Tag>Not found</Tag>;
    }
    return <Typography.Text copyable>{value}</Typography.Text>;
}

export function AttributesCard({ attributes }: { attributes: ExtractedAttributes }) {
    const rows: Array<{ key: string; label: ReactNode; value: string | null }> = [
        {
            key: 'name',
            label: (
                <>
                    <UserOutlined /> Name
                </>
            ),
            value: attributes.name,
        },
        {
            key: 'address',
            label: (
                <>
                    <EnvironmentOutlined /> Address
                </>
            ),
            value: attributes.address,
        },
        {
            key: 'ssn',
            label: (
                <>
                    <IdcardOutlined /> Social Security Number
                </>
            ),
            value: attributes.socialSecurityNumber,
        },
        {
            key: 'phone',
            label: (
                <>
                    <PhoneOutlined /> Phone Number
                </>
            ),
            value: attributes.phoneNumber,
        },
        {
            key: 'email',
            label: (
                <>
                    <MailOutlined /> Email
                </>
            ),
            value: attributes.email,
        },
    ];

    return (
        <Card title="Extracted attributes" size="small">
            <Descriptions column={1} size="small" bordered>
                {rows.map((row) => (
                    <Descriptions.Item key={row.key} label={row.label}>
                        <Value value={row.value} />
                    </Descriptions.Item>
                ))}
            </Descriptions>
        </Card>
    );
}
