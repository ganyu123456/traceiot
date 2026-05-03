import { useEffect, useState } from 'react'
import {
  Table, Button, Space, Modal, Form, Input, InputNumber,
  Popconfirm, message, Row, Col, Typography, Tag
} from 'antd'
import { PlusOutlined, EditOutlined, DeleteOutlined, ReloadOutlined } from '@ant-design/icons'
import { deviceGroupApi, DeviceGroup } from '@/api/device'
import type { ColumnsType } from 'antd/es/table'
import dayjs from 'dayjs'

const { Title } = Typography

export default function DeviceGroups() {
  const [groups, setGroups]       = useState<DeviceGroup[]>([])
  const [total, setTotal]         = useState(0)
  const [loading, setLoading]     = useState(false)
  const [modalOpen, setModalOpen] = useState(false)
  const [editGroup, setEditGroup] = useState<DeviceGroup | null>(null)
  const [form]                    = Form.useForm()

  const load = async () => {
    setLoading(true)
    try {
      const res = await deviceGroupApi.getList({ maxResultCount: 100 })
      setGroups(res.items)
      setTotal(res.totalCount)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const openCreate = () => {
    setEditGroup(null)
    form.resetFields()
    setModalOpen(true)
  }

  const openEdit = (record: DeviceGroup) => {
    setEditGroup(record)
    form.setFieldsValue(record)
    setModalOpen(true)
  }

  const handleSubmit = async () => {
    const values = await form.validateFields()
    if (editGroup) {
      await deviceGroupApi.update(editGroup.id, values)
      message.success('更新成功')
    } else {
      await deviceGroupApi.create(values)
      message.success('创建成功')
    }
    setModalOpen(false)
    load()
  }

  const handleDelete = async (id: string) => {
    await deviceGroupApi.delete(id)
    message.success('删除成功')
    load()
  }

  const columns: ColumnsType<DeviceGroup> = [
    { title: '分组名称', dataIndex: 'name', width: 160 },
    { title: '描述',     dataIndex: 'description', ellipsis: true },
    { title: '排序',     dataIndex: 'sortOrder', width: 80 },
    {
      title: '设备数量', dataIndex: 'deviceCount', width: 100,
      render: v => <Tag color="blue">{v} 台</Tag>
    },
    {
      title: '操作', width: 120,
      render: (_, record) => (
        <Space>
          <Button icon={<EditOutlined />} type="link" size="small" onClick={() => openEdit(record)}>编辑</Button>
          <Popconfirm title="确认删除？有设备的分组不可删除" onConfirm={() => handleDelete(record.id)}>
            <Button icon={<DeleteOutlined />} type="link" size="small" danger>删除</Button>
          </Popconfirm>
        </Space>
      )
    }
  ]

  return (
    <div>
      <Row justify="space-between" align="middle" style={{ marginBottom: 16 }}>
        <Col><Title level={4} style={{ margin: 0 }}>设备分组</Title></Col>
        <Col>
          <Space>
            <Button icon={<ReloadOutlined />} onClick={load}>刷新</Button>
            <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>新增分组</Button>
          </Space>
        </Col>
      </Row>

      <Table
        columns={columns} dataSource={groups} rowKey="id"
        loading={loading} pagination={{ total, showTotal: t => `共 ${t} 个分组` }}
      />

      <Modal
        title={editGroup ? '编辑分组' : '新增分组'}
        open={modalOpen} onOk={handleSubmit} onCancel={() => setModalOpen(false)}
        width={480} destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item name="name" label="分组名称" rules={[{ required: true, max: 64 }]}>
            <Input placeholder="分组名称" />
          </Form.Item>
          <Form.Item name="description" label="描述">
            <Input.TextArea rows={2} />
          </Form.Item>
          <Form.Item name="sortOrder" label="排序权重" initialValue={0}>
            <InputNumber min={0} style={{ width: '100%' }} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
