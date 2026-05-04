import { useEffect, useRef, useState, useCallback } from 'react'
import { Card, Row, Col, Statistic, Tag, Table, Typography, Badge, Space, Spin } from 'antd'
import {
  CarOutlined, WifiOutlined, DisconnectOutlined, WarningOutlined
} from '@ant-design/icons'
import { realtimeApi, DevicePosition, DashboardStats } from '@/api/realtime'
import dayjs from 'dayjs'

const { Title } = Typography

// 高德地图 Key（请替换为你自己的 Key）
const AMAP_KEY = '35676052942c09da714f53640f0eedcb'

declare global {
  interface Window {
    AMap: any
    _amapLoaded: boolean
  }
}

function loadAMap(): Promise<void> {
  return new Promise((resolve, reject) => {
    if (window._amapLoaded && window.AMap) { resolve(); return }
    const script = document.createElement('script')
    script.src = `https://webapi.amap.com/maps?v=2.0&key=${AMAP_KEY}`
    script.onload = () => { window._amapLoaded = true; resolve() }
    script.onerror = reject
    document.head.appendChild(script)
  })
}

export default function RealtimeMap() {
  const mapRef       = useRef<HTMLDivElement>(null)
  const mapInstance  = useRef<any>(null)
  const markersRef   = useRef<Record<string, any>>({})
  const [positions, setPositions] = useState<DevicePosition[]>([])
  const [stats, setStats]         = useState<DashboardStats | null>(null)
  const [mapReady, setMapReady]   = useState(false)
  const [loading, setLoading]     = useState(true)

  // 初始化高德地图
  useEffect(() => {
    loadAMap().then(() => {
      if (!mapRef.current || mapInstance.current) return
      mapInstance.current = new window.AMap.Map(mapRef.current, {
        zoom: 10,
        center: [116.4074, 39.9042], // 北京
        mapStyle: 'amap://styles/normal',
      })
      setMapReady(true)
      setLoading(false)
    }).catch(() => {
      setLoading(false)
    })
    return () => {
      if (mapInstance.current) mapInstance.current.destroy()
    }
  }, [])

  const updateMarkers = useCallback((data: DevicePosition[]) => {
    if (!mapInstance.current) return
    const AMap = window.AMap

    data.forEach(pos => {
      const lnglat = [pos.lng, pos.lat]
      const title  = `${pos.deviceName}\n速度: ${pos.speed.toFixed(1)} km/h`

      if (markersRef.current[pos.deviceCode]) {
        // 更新已有标注位置
        markersRef.current[pos.deviceCode].setPosition(lnglat)
        markersRef.current[pos.deviceCode].setTitle(title)
      } else {
        // 创建新标注
        const marker = new AMap.Marker({
          position: lnglat,
          title,
          icon: new AMap.Icon({
            size: new AMap.Size(32, 32),
            image: pos.online
              ? 'https://a.amap.com/jsapi_demos/static/demo-center/icons/poi-marker-1.png'
              : 'https://a.amap.com/jsapi_demos/static/demo-center/icons/poi-marker-red.png',
            imageSize: new AMap.Size(32, 32),
          }),
          label: {
            content: `<div style="background:#fff;padding:2px 6px;border-radius:4px;font-size:12px;border:1px solid #ddd">${pos.deviceName}</div>`,
            offset: new AMap.Pixel(-20, -40),
          }
        })
        marker.on('click', () => {
          const info = new AMap.InfoWindow({
            content: `
              <div style="padding:8px;min-width:180px">
                <b>${pos.deviceName}</b><br/>
                编号：${pos.deviceCode}<br/>
                状态：${pos.online ? '在线' : '离线'}<br/>
                速度：${pos.speed.toFixed(1)} km/h<br/>
                方向：${pos.direction.toFixed(0)}°<br/>
                时间：${dayjs(pos.timestamp).format('HH:mm:ss')}
              </div>
            `,
            offset: new AMap.Pixel(0, -40),
          })
          info.open(mapInstance.current, marker.getPosition())
        })
        marker.setMap(mapInstance.current)
        markersRef.current[pos.deviceCode] = marker
      }
    })
  }, [])

  const fetchData = useCallback(async () => {
    const [posRes, statsRes] = await Promise.all([
      realtimeApi.getAllPositions(),
      realtimeApi.getDashboard(),
    ])
    setPositions(posRes)
    setStats(statsRes)
    if (mapReady) updateMarkers(posRes)
  }, [mapReady, updateMarkers])

  useEffect(() => {
    fetchData()
    const timer = setInterval(fetchData, 3000) // 3 秒刷新一次
    return () => clearInterval(timer)
  }, [fetchData])

  const columns = [
    { title: '设备名称', dataIndex: 'deviceName', width: 120 },
    { title: '编号',     dataIndex: 'deviceCode', width: 120 },
    {
      title: '状态', dataIndex: 'online', width: 70,
      render: (v: boolean) => v
        ? <Badge status="success" text="在线" />
        : <Badge status="default" text="离线" />
    },
    { title: '速度', dataIndex: 'speed', width: 90,
      render: (v: number) => `${v.toFixed(1)} km/h` },
    { title: '纬度', dataIndex: 'lat', width: 110,
      render: (v: number) => v.toFixed(6) },
    { title: '经度', dataIndex: 'lng', width: 110,
      render: (v: number) => v.toFixed(6) },
    { title: '更新时间', dataIndex: 'timestamp', width: 90,
      render: (v: number) => v ? dayjs(v).format('HH:mm:ss') : '-' },
  ]

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>实时监控</Title>

      {/* 统计卡片 */}
      <Row gutter={16} style={{ marginBottom: 16 }}>
        {[
          { title: '总设备',   value: stats?.totalDevices   ?? 0, icon: <CarOutlined />,        color: '#1677ff' },
          { title: '在线设备', value: stats?.onlineDevices  ?? 0, icon: <WifiOutlined />,        color: '#52c41a' },
          { title: '离线设备', value: stats?.offlineDevices ?? 0, icon: <DisconnectOutlined />,  color: '#8c8c8c' },
          { title: '今日告警', value: stats?.todayAlarmCount ?? 0, icon: <WarningOutlined />,    color: '#fa8c16' },
        ].map(item => (
          <Col span={6} key={item.title}>
            <Card>
              <Statistic title={item.title} value={item.value}
                prefix={<span style={{ color: item.color }}>{item.icon}</span>} />
            </Card>
          </Col>
        ))}
      </Row>

      {/* 地图 */}
      <Card style={{ marginBottom: 16 }} bodyStyle={{ padding: 0 }}>
        <Spin spinning={loading} tip="地图加载中...">
          <div ref={mapRef} style={{ height: 420, width: '100%', background: '#f0f0f0' }}>
            {!mapReady && !loading && (
              <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
                <Space direction="vertical" align="center">
                  <CarOutlined style={{ fontSize: 40, color: '#bbb' }} />
                  <Typography.Text type="secondary">
                    请在 RealtimeMap/index.tsx 中配置高德地图 Key
                  </Typography.Text>
                </Space>
              </div>
            )}
          </div>
        </Spin>
      </Card>

      {/* 设备列表 */}
      <Card title={<Space><WifiOutlined />在线设备列表（3秒自动刷新）</Space>}>
        <Table
          columns={columns}
          dataSource={positions.filter(p => p.online)}
          rowKey="deviceCode"
          size="small"
          scroll={{ x: 700 }}
          pagination={false}
        />
      </Card>
    </div>
  )
}
