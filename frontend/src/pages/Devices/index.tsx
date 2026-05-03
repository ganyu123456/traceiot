import { useEffect, useState } from 'react'
import {
  Table, Button, Space, Tag, Modal, Form, Input, Select,
  InputNumber, Popconfirm, message, Card, Row, Col, Typography, Switch, Tooltip
} from 'antd'
import {
  PlusOutlined, EditOutlined, DeleteOutlined,
  CheckCircleOutlined, StopOutlined, ReloadOutlined
} from '@ant-design/icons'
import { deviceApi, deviceGroupApi, Device, DeviceGroup } from '@/api/device'
import type { ColumnsType } from 'antd/es/table'
import dayjs from 'dayjs'

const { Title } = Typography
const statusMap: Record<number, { color: string; text: string }> = {
  0: { color: 'default', text: '离线' },
  1: { color: 'success', text: '在线' },
  2: { color: 'error',   text: '禁用' },
}

export default function Devices() {
  const [devices, setDevices]       = useState<Device[]>([])
  const [groups, setGroups]         = useState<DeviceGroup[]>([])
  const [total, setTotal]           = useState(0)
  const [loading, setLoading]       = useState(false)
  const [modalOpen, setModalOpen]   = useState(false)
  const [editDevice, setEditDevice] = useState<Device | null>(null)
  const [page, setPage]             = useState(1)
  const [pageSize]                  = useState(10)
  const [filter, setFilter]         = useState('')
  const [form]                      = Form.useForm()

  const load = async () => {
    setLoading(true)
    try {
      const res = await deviceApi.getList({
        filter, skipCount: (page - 1) * pageSize, maxResultCount: pageSize
      })
      setDevices(res.items)
      setTotal(res.totalCount)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [page, filter])
  useEffect(() => {
    deviceGroupApi.getAll().then(setGroups)
  }, [])

  const openCreate = () => {
    setEditDevice(null)
    form.resetFields()
    setModalOpen(true)
  }

  const openEdit = (record: Device) => {
    setEditDevice(record)
    form.setFieldsValue({
      deviceCode: record.deviceCode,
      deviceName: record.deviceName,
      groupId: record.groupId,
      remark: record.remark,
      overspeedThreshold: record.overspeedThreshold,
      offlineTimeoutSec: record.offlineTimeoutSec,
    })
    setModalOpen(true)
  }

  const handleSubmit = async () => {
    const values = await form.validateFields()
    if (editDevice) {
      await deviceApi.update(editDevice.id, values)
      message.success('更新成功')
    } else {
      await deviceApi.create(values)
      message.success('创建成功')
    }
    setModalOpen(false)
    load()
  }

  const handleDelete = async (id: string) => {
    await deviceApi.delete(id)
    message.success('删除成功')
    load()
  }

  const toggleEnabled = async (record: Device) => {
    if (record.isEnabled) {
      await deviceApi.disable(record.id)
    } else {
      await deviceApi.enable(record.id)
    }
    message.success(record.isEnabled ? '已禁用' : '已启用')
    load()
  }

  const columns: ColumnsType<Device> = [
    { title: '设备编号', dataIndex: 'deviceCode', width: 160, ellipsis: true },
    { title: '设备名称', dataIndex: 'deviceName', width: 150, ellipsis: true },
    { title: '分组', dataIndex: 'groupName', width: 120, render: v => v || <Tag color="default">未分组</Tag> },
    {
      title: '状态', dataIndex: 'status', width: 80,
      render: (v: number) => <Tag color={statusMap[v]?.color}>{statusMap[v]?.text}</Tag>
    },
    {
      title: '最后心跳', dataIndex: 'lastHeartbeatAt', width: 160,
      render: v => v ? dayjs(v).format('YYYY-MM-DD HH:mm:ss') : '-'
    },
    {
      title: '速度', dataIndex: 'lastSpeed', width: 90,
      render: v => v != null ? `${v.toFixed(1)} km/h` : '-'
    },
    { title: '备注', dataIndex: 'remark', ellipsis: true },
    {
      title: '启用', dataIndex: 'isEnabled', width: 70,
      render: (v, record) => <Switch checked={v} onChange={() => toggleEnabled(record)} />
    },
    {
      title: '操作', width: 120, fixed: 'right',
      render: (_, record) => (
        <Space>
          <Tooltip title="编辑"><Button icon={<EditOutlined />} type="link" size="small" onClick={() => openEdit(record)} /></Tooltip>
          <Popconfirm title="确认删除此设备？" onConfirm={() => handleDelete(record.id)}>
            <Tooltip title="删除"><Button icon={<DeleteOutlined />} type="link" size="small" danger /></Tooltip>
          </Popconfirm>
        </Space>
      )
    }
  ]

  return (
    <div>
      <Row justify="space-between" align="middle" style={{ marginBottom: 16 }}>
        <Col><Title level={4} style={{ margin: 0 }}>设备管理</Title></Col>
        <Col>
          <Space>
            <Input.Search
              placeholder="搜索设备编号/名称"
              allowClear
              onSearch={setFilter}
              style={{ width: 220 }}
            />
            <Button icon={<ReloadOutlined />} onClick={load}>刷新</Button>
            <Button type="primary" icon={<PlusOutlined />} onClick={openCreate}>新增设备</Button>
          </Space>
        </Col>
      </Row>

      <Table
        columns={columns}
        dataSource={devices}
        rowKey="id"
        loading={loading}
        scroll={{ x: 1100 }}
        pagination={{
          total, current: page, pageSize,
          showSizeChanger: false, showTotal: t => `共 ${t} 条`,
          onChange: setPage
        }}
      />

      <Modal
        title={editDevice ? '编辑设备' : '新增设备'}
        open={modalOpen}
        onOk={handleSubmit}
        onCancel={() => setModalOpen(false)}
        width={520}
        destroyOnClose
      >
        <Form form={form} layout="vertical" style={{ marginTop: 16 }}>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="deviceCode" label="设备编号" rules={[{ required: true }]}>
                <Input disabled={!!editDevice} placeholder="IMEI 或自定义编号" />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="deviceName" label="设备名称" rules={[{ required: true }]}>
                <Input placeholder="设备名称" />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="groupId" label="所属分组">
            <Select placeholder="选择分组" allowClear>
              {groups.map(g => <Select.Option key={g.id} value={g.id}>{g.name}</Select.Option>)}
            </Select>
          </Form.Item>
          <Row gutter={16}>
            <Col span={12}>
              <Form.Item name="overspeedThreshold" label="超速阈值(km/h)" initialValue={120}>
                <InputNumber min={0} max={300} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
            <Col span={12}>
              <Form.Item name="offlineTimeoutSec" label="离线超时(秒)" initialValue={60}>
                <InputNumber min={10} max={3600} style={{ width: '100%' }} />
              </Form.Item>
            </Col>
          </Row>
          <Form.Item name="remark" label="备注">
            <Input.TextArea rows={2} />
          </Form.Item>
        </Form>
      </Modal>
    </div>
  )
}
