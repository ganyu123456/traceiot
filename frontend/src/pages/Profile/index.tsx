import { useState } from 'react'
import { Card, Form, Input, Button, message, Row, Col, Avatar, Typography, Tag, Divider } from 'antd'
import { UserOutlined, LockOutlined } from '@ant-design/icons'
import { useAuthStore } from '@/store/authStore'
import request from '@/utils/request'

const { Title, Text } = Typography

export default function Profile() {
  const { user } = useAuthStore()
  const [loading, setLoading] = useState(false)
  const [form] = Form.useForm()

  const handleChangePassword = async (values: { oldPassword: string; newPassword: string; confirmPassword: string }) => {
    if (values.newPassword !== values.confirmPassword) {
      message.error('两次输入的新密码不一致')
      return
    }
    setLoading(true)
    try {
      await request.post('/identity/my-profile/change-password', {
        currentPassword: values.oldPassword,
        newPassword:     values.newPassword,
      })
      message.success('密码修改成功，请重新登录')
      form.resetFields()
    } finally {
      setLoading(false)
    }
  }

  return (
    <div>
      <Title level={4} style={{ marginBottom: 24 }}>个人中心</Title>
      <Row gutter={24}>
        <Col span={8}>
          <Card>
            <div style={{ textAlign: 'center', padding: '16px 0' }}>
              <Avatar size={72} icon={<UserOutlined />} style={{ background: '#1677ff' }} />
              <div style={{ marginTop: 12 }}>
                <Title level={5} style={{ margin: 0 }}>{user?.userName}</Title>
                <Text type="secondary">{user?.email}</Text>
              </div>
              <Divider />
              <div>
                {user?.roles.map(r => <Tag key={r} color="blue">{r}</Tag>)}
              </div>
            </div>
          </Card>
        </Col>

        <Col span={16}>
          <Card title={<><LockOutlined /> 修改密码</>}>
            <Form form={form} layout="vertical" onFinish={handleChangePassword} style={{ maxWidth: 400 }}>
              <Form.Item name="oldPassword" label="当前密码" rules={[{ required: true }]}>
                <Input.Password />
              </Form.Item>
              <Form.Item name="newPassword" label="新密码"
                rules={[{ required: true }, { min: 6, message: '密码至少 6 位' }]}>
                <Input.Password />
              </Form.Item>
              <Form.Item name="confirmPassword" label="确认新密码" rules={[{ required: true }]}>
                <Input.Password />
              </Form.Item>
              <Form.Item>
                <Button type="primary" htmlType="submit" loading={loading}>保存修改</Button>
              </Form.Item>
            </Form>
          </Card>
        </Col>
      </Row>
    </div>
  )
}
