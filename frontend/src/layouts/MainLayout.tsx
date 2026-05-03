import { useState } from 'react'
import { Outlet, useNavigate, useLocation } from 'react-router-dom'
import {
  Layout, Menu, Avatar, Dropdown, Badge, Space, Typography, theme
} from 'antd'
import {
  DashboardOutlined, CarOutlined, AppstoreOutlined,
  EnvironmentOutlined, HistoryOutlined, BellOutlined,
  UserOutlined, LogoutOutlined, MenuFoldOutlined, MenuUnfoldOutlined
} from '@ant-design/icons'
import { useAuthStore } from '@/store/authStore'

const { Header, Sider, Content } = Layout
const { Text } = Typography

const menuItems = [
  { key: '/realtime', icon: <DashboardOutlined />, label: '实时监控' },
  { key: '/devices',  icon: <CarOutlined />,       label: '设备管理' },
  { key: '/groups',   icon: <AppstoreOutlined />,  label: '设备分组' },
  { key: '/track',    icon: <HistoryOutlined />,   label: '轨迹回放' },
  { key: '/alarms',   icon: <BellOutlined />,      label: '告警日志' },
  { key: '/profile',  icon: <UserOutlined />,      label: '个人中心' },
]

export default function MainLayout() {
  const [collapsed, setCollapsed] = useState(false)
  const navigate  = useNavigate()
  const location  = useLocation()
  const { user, logout } = useAuthStore()
  const { token } = theme.useToken()

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  const userMenu = {
    items: [
      { key: 'profile', icon: <UserOutlined />, label: '个人中心',
        onClick: () => navigate('/profile') },
      { type: 'divider' as const },
      { key: 'logout', icon: <LogoutOutlined />, label: '退出登录',
        onClick: handleLogout, danger: true },
    ]
  }

  const selectedKey = '/' + location.pathname.split('/')[1] || '/realtime'

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Sider
        trigger={null}
        collapsible
        collapsed={collapsed}
        style={{ background: token.colorBgContainer }}
        width={220}
      >
        <div style={{
          height: 64, display: 'flex', alignItems: 'center',
          justifyContent: 'center', padding: '0 16px',
          borderBottom: `1px solid ${token.colorBorderSecondary}`
        }}>
          <EnvironmentOutlined style={{ fontSize: 24, color: token.colorPrimary }} />
          {!collapsed && (
            <Text strong style={{ marginLeft: 10, fontSize: 15, color: token.colorPrimary }}>
              TraceIoT
            </Text>
          )}
        </div>
        <Menu
          mode="inline"
          selectedKeys={[selectedKey]}
          items={menuItems}
          onClick={({ key }) => navigate(key)}
          style={{ borderRight: 0, marginTop: 8 }}
        />
      </Sider>

      <Layout>
        <Header style={{
          padding: '0 24px',
          background: token.colorBgContainer,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          borderBottom: `1px solid ${token.colorBorderSecondary}`,
          height: 64
        }}>
          <Space>
            {collapsed
              ? <MenuUnfoldOutlined onClick={() => setCollapsed(false)} style={{ fontSize: 18, cursor: 'pointer' }} />
              : <MenuFoldOutlined   onClick={() => setCollapsed(true)}  style={{ fontSize: 18, cursor: 'pointer' }} />
            }
          </Space>
          <Dropdown menu={userMenu} placement="bottomRight">
            <Space style={{ cursor: 'pointer' }}>
              <Avatar icon={<UserOutlined />} size="small" />
              {user?.userName}
            </Space>
          </Dropdown>
        </Header>

        <Content style={{
          margin: 24, padding: 24,
          background: token.colorBgContainer,
          borderRadius: token.borderRadius,
          minHeight: 280,
          overflow: 'auto'
        }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  )
}
