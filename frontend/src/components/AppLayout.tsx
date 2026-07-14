import styled from '@emotion/styled';
import { Layout, Menu, Typography } from 'antd';
import { AudioOutlined, HistoryOutlined, PlusCircleOutlined } from '@ant-design/icons';
import { Link, Outlet, useLocation } from 'react-router-dom';

const Header = styled(Layout.Header)`
  display: flex;
  align-items: center;
  gap: 32px;
  background: #ffffff;
  border-bottom: 1px solid #f0f0f0;
  position: sticky;
  top: 0;
  z-index: 10;
`;

const Brand = styled(Link)`
  display: flex;
  align-items: center;
  gap: 10px;
  white-space: nowrap;
`;

const Content = styled(Layout.Content)`
  max-width: 960px;
  width: 100%;
  margin: 0 auto;
  padding: 32px 24px 64px;
`;

/** Page frame: sticky header with navigation + centered content column. */
export function AppLayout() {
    const location = useLocation();
    // Highlight "History" for both /history and /transcription/:id.
    const selectedKey = location.pathname === '/' ? '/' : '/history';

    return (
        <Layout style={{ minHeight: '100vh', background: '#f5f6fa' }}>
            <Header>
                <Brand to="/">
                    <AudioOutlined style={{ fontSize: 22, color: '#2f54eb' }} />
                    <Typography.Text strong style={{ fontSize: 17 }}>
                        Transcript Analysis
                    </Typography.Text>
                </Brand>
                <Menu
                    mode="horizontal"
                    selectedKeys={[selectedKey]}
                    style={{ flex: 1, borderBottom: 'none' }}
                    items={[
                        {
                            key: '/',
                            icon: <PlusCircleOutlined />,
                            label: <Link to="/">New Transcription</Link>,
                        },
                        {
                            key: '/history',
                            icon: <HistoryOutlined />,
                            label: <Link to="/history">History</Link>,
                        },
                    ]}
                />
            </Header>
            <Content>
                <Outlet />
            </Content>
        </Layout>
    );
}
