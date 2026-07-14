import styled from '@emotion/styled';
import { Empty } from 'antd';
import type { ConversationTurn } from '../types';

/**
 * Chat-style rendering of the conversation:
 * Agent / Speaker 1 on the left, Caller / Speaker 2 on the right.
 */

const List = styled.div`
  display: flex;
  flex-direction: column;
  gap: 12px;
`;

const Row = styled.div<{ right: boolean }>`
  display: flex;
  flex-direction: column;
  align-items: ${(p) => (p.right ? 'flex-end' : 'flex-start')};
`;

const RoleLabel = styled.span`
  font-size: 12px;
  color: #8c8c8c;
  margin: 0 6px 3px;
`;

const Bubble = styled.div<{ right: boolean }>`
  max-width: 75%;
  padding: 10px 14px;
  font-size: 14px;
  line-height: 1.5;
  white-space: pre-wrap;
  word-break: break-word;
  background: ${(p) => (p.right ? '#2f54eb' : '#ffffff')};
  color: ${(p) => (p.right ? '#ffffff' : 'rgba(0, 0, 0, 0.88)')};
  border: 1px solid ${(p) => (p.right ? '#2f54eb' : '#e5e7eb')};
  border-radius: ${(p) => (p.right ? '14px 14px 4px 14px' : '14px 14px 14px 4px')};
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
`;

function speaksFromRight(role: string): boolean {
    const normalized = role.trim().toLowerCase();
    return normalized === 'caller' || normalized === 'speaker 2';
}

export function ConversationView({ conversation }: { conversation: ConversationTurn[] }) {
    if (conversation.length === 0) {
        return <Empty description="No conversation turns" />;
    }
    return (
        <List>
            {conversation.map((turn, index) => {
                const right = speaksFromRight(turn.role);
                return (
                    <Row key={index} right={right}>
                        <RoleLabel>{turn.role}</RoleLabel>
                        <Bubble right={right}>{turn.text}</Bubble>
                    </Row>
                );
            })}
        </List>
    );
}
