import { Form, Input, Button, Card, Typography, message } from 'antd'
import { UserOutlined, LockOutlined, EnvironmentOutlined } from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import { authApi } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'
import { useState } from 'react'

const { Title, Text } = Typography

export default function Login() {
  const navigate = useNavigate()
  const login    = useAuthStore(s => s.login)
  const [loading, setLoading] = useState(false)

  const onFinish = async (values: { userName: string; password: string }) => {
    setLoading(true)
    try {
      const res = await authApi.login(values)
      login(res)
      message.success('登录成功')
      navigate('/', { replace: true })
    } finally {
      setLoading(false)
    }
  }

  return (
    <div style={{
      minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'linear-gradient(135deg, #1677ff 0%, #0958d9 100%)'
    }}>
      <Card style={{ width: 400, borderRadius: 12, boxShadow: '0 8px 32px rgba(0,0,0,0.18)' }}>
        <div style={{ textAlign: 'center', marginBottom: 32 }}>
          <EnvironmentOutlined style={{ fontSize: 40, color: '#1677ff' }} />
          <Title level={3} style={{ margin: '12px 0 4px' }}>TraceIoT</Title>
          <Text type="secondary">物联网GPS定位轨迹云平台</Text>
        </div>

        <Form layout="vertical" onFinish={onFinish} initialValues={{ userName: 'admin', password: 'Admin@123456' }}>
          <Form.Item name="userName" rules={[{ required: true, message: '请输入用户名' }]}>
            <Input prefix={<UserOutlined />} placeholder="用户名" size="large" />
          </Form.Item>
          <Form.Item name="password" rules={[{ required: true, message: '请输入密码' }]}>
            <Input.Password prefix={<LockOutlined />} placeholder="密码" size="large" />
          </Form.Item>
          <Form.Item style={{ marginBottom: 0 }}>
            <Button type="primary" htmlType="submit" size="large" block loading={loading}>
              登 录
            </Button>
          </Form.Item>
        </Form>

        <div style={{ textAlign: 'center', marginTop: 16 }}>
          <Text type="secondary" style={{ fontSize: 12 }}>默认账号：admin / Admin@123456</Text>
        </div>
      </Card>
    </div>
  )
}
