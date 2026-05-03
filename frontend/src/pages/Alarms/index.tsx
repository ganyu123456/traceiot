import { useEffect, useState } from 'react'
import {
  Table, Tag, Button, Space, Modal, Input, DatePicker, Select,
  Row, Col, Typography, Badge, Tooltip, message
} from 'antd'
import { CheckOutlined, ReloadOutlined } from '@ant-design/icons'
import { alarmApi, AlarmRecord } from '@/api/alarm'
import type { ColumnsType } from 'antd/es/table'
import dayjs from 'dayjs'

const { Title } = Typography
const { RangePicker } = DatePicker

const alarmTypeColors: Record<number, string> = { 1: 'orange', 2: 'purple', 3: 'red' }

export default function Alarms() {
  const [records, setRecords]     = useState<AlarmRecord[]>([])
  const [total, setTotal]         = useState(0)
  const [loading, setLoading]     = useState(false)
  const [page, setPage]           = useState(1)
  const [pageSize]                = useState(20)
  const [filterType, setFilterType]     = useState<number | undefined>()
  const [filterHandled, setHandled]     = useState<boolean | undefined>()
  const [timeRange, setTimeRange]       = useState<[string, string] | null>(null)
  const [handleModal, setHandleModal]   = useState(false)
  const [selectedId, setSelectedId]     = useState('')
  const [note, setNote]                 = useState('')

  const load = async () => {
    setLoading(true)
    try {
      const res = await alarmApi.getList({
        alarmType:      filterType,
        isHandled:      filterHandled,
        startTime:      timeRange?.[0],
        endTime:        timeRange?.[1],
        skipCount:      (page - 1) * pageSize,
        maxResultCount: pageSize,
      })
      setRecords(res.items)
      setTotal(res.totalCount)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [page, filterType, filterHandled, timeRange])

  const openHandle = (id: string) => {
    setSelectedId(id)
    setNote('')
    setHandleModal(true)
  }

  const handleAlarm = async () => {
    await alarmApi.handle(selectedId, note)
    message.success('已处理')
    setHandleModal(false)
    load()
  }

  const columns: ColumnsType<AlarmRecord> = [
    { title: '设备名称', dataIndex: 'deviceName', width: 120, ellipsis: true },
    { title: '设备编号', dataIndex: 'deviceCode', width: 140 },
    {
      title: '告警类型', dataIndex: 'alarmType', width: 100,
      render: (v: number, r) => <Tag color={alarmTypeColors[v]}>{r.alarmTypeName}</Tag>
    },
    { title: '告警值', dataIndex: 'alarmValue', width: 100, render: v => v ? v.toFixed(1) : '-' },
    {
      title: '触发时间', dataIndex: 'triggeredAt', width: 170,
      render: v => dayjs(v).format('YYYY-MM-DD HH:mm:ss')
    },
    {
      title: '状态', dataIndex: 'isHandled', width: 80,
      render: v => v
        ? <Badge status="success" text="已处理" />
        : <Badge status="error"   text="未处理" />
    },
    {
      title: '处理时间', dataIndex: 'handledAt', width: 160,
      render: v => v ? dayjs(v).format('YYYY-MM-DD HH:mm:ss') : '-'
    },
    { title: '处理备注', dataIndex: 'handlerNote', ellipsis: true },
    {
      title: '操作', width: 80, fixed: 'right',
      render: (_, r) => !r.isHandled && (
        <Tooltip title="标记已处理">
          <Button icon={<CheckOutlined />} type="link" size="small" onClick={() => openHandle(r.id)}>处理</Button>
        </Tooltip>
      )
    }
  ]

  return (
    <div>
      <Row justify="space-between" align="middle" style={{ marginBottom: 16 }}>
        <Col><Title level={4} style={{ margin: 0 }}>告警日志</Title></Col>
        <Col>
          <Space>
            <Select placeholder="告警类型" allowClear style={{ width: 120 }} onChange={v => { setFilterType(v); setPage(1) }}>
              <Select.Option value={1}>超速</Select.Option>
              <Select.Option value={2}>电子围栏</Select.Option>
              <Select.Option value={3}>设备离线</Select.Option>
            </Select>
            <Select placeholder="处理状态" allowClear style={{ width: 120 }} onChange={v => { setHandled(v); setPage(1) }}>
              <Select.Option value={false}>未处理</Select.Option>
              <Select.Option value={true}>已处理</Select.Option>
            </Select>
            <RangePicker showTime
              onChange={(_, strs) => setTimeRange(strs[0] ? [strs[0], strs[1]] : null)}
            />
            <Button icon={<ReloadOutlined />} onClick={load}>刷新</Button>
          </Space>
        </Col>
      </Row>

      <Table
        columns={columns} dataSource={records} rowKey="id"
        loading={loading} scroll={{ x: 1100 }}
        pagination={{ total, current: page, pageSize, showTotal: t => `共 ${t} 条`, onChange: setPage }}
      />

      <Modal
        title="处理告警" open={handleModal}
        onOk={handleAlarm} onCancel={() => setHandleModal(false)}
      >
        <Input.TextArea
          rows={3} placeholder="请输入处理说明（可选）"
          value={note} onChange={e => setNote(e.target.value)}
        />
      </Modal>
    </div>
  )
}
