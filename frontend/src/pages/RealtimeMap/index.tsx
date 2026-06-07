import { useEffect, useRef, useState, useCallback } from 'react'
import { Card, Row, Col, Statistic, Tag, Table, Typography, Badge, Space, Spin } from 'antd'
import {
  CarOutlined, WifiOutlined, DisconnectOutlined, WarningOutlined
} from '@ant-design/icons'
import { realtimeApi, DevicePosition, DashboardStats } from '@/api/realtime'
import dayjs from 'dayjs'

const { Title } = Typography

const AMAP_KEY           = '57a5e349af4ba24b6e204e299d91c332'
const AMAP_SECURITY_CODE = 'f8361b07700c70840d85b27ea0bbe6d5'

// WGS84（GPS原始坐标）→ GCJ02（高德/火星坐标）转换
// 高德地图使用 GCJ02，GPS 模块输出 WGS84，不转换会有 ~200-500m 偏差
const GCJ_A  = 6378245.0
const GCJ_EE = 0.00669342162296594323

function outOfChina(lng: number, lat: number) {
  return lng < 72.004 || lng > 137.8347 || lat < 0.8293 || lat > 55.8271
}
function transformLat(x: number, y: number) {
  let r = -100 + 2*x + 3*y + 0.2*y*y + 0.1*x*y + 0.2*Math.sqrt(Math.abs(x))
  r += (20*Math.sin(6*x*Math.PI) + 20*Math.sin(2*x*Math.PI)) * 2/3
  r += (20*Math.sin(y*Math.PI)   + 40*Math.sin(y/3*Math.PI)) * 2/3
  r += (160*Math.sin(y/12*Math.PI) + 320*Math.sin(y*Math.PI/30)) * 2/3
  return r
}
function transformLng(x: number, y: number) {
  let r = 300 + x + 2*y + 0.1*x*x + 0.1*x*y + 0.1*Math.sqrt(Math.abs(x))
  r += (20*Math.sin(6*x*Math.PI) + 20*Math.sin(2*x*Math.PI)) * 2/3
  r += (20*Math.sin(x*Math.PI)   + 40*Math.sin(x/3*Math.PI)) * 2/3
  r += (150*Math.sin(x/12*Math.PI) + 300*Math.sin(x/30*Math.PI)) * 2/3
  return r
}
function wgs84ToGcj02(lng: number, lat: number): [number, number] {
  if (outOfChina(lng, lat)) return [lng, lat]
  let dLat = transformLat(lng - 105, lat - 35)
  let dLng = transformLng(lng - 105, lat - 35)
  const radLat  = lat / 180 * Math.PI
  let   magic   = Math.sin(radLat)
  magic = 1 - GCJ_EE * magic * magic
  const sqrtM   = Math.sqrt(magic)
  dLat = dLat * 180 / ((GCJ_A * (1 - GCJ_EE)) / (magic * sqrtM) * Math.PI)
  dLng = dLng * 180 / (GCJ_A / sqrtM * Math.cos(radLat) * Math.PI)
  return [lng + dLng, lat + dLat]
}

declare global {
  interface Window {
    AMap: any
    _amapLoaded: boolean
    _AMapSecurityConfig: { securityJsCode: string }
  }
}

function loadAMap(): Promise<void> {
  return new Promise((resolve, reject) => {
    if (window._amapLoaded && window.AMap) { resolve(); return }
    window._AMapSecurityConfig = { securityJsCode: AMAP_SECURITY_CODE }
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
  const fitViewDone  = useRef(false)   // 只在首次有标注时自动定位，之后不干扰用户缩放
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
    const validData = data.filter(p => p.lat !== 0 || p.lng !== 0)

    validData.forEach(pos => {
      // WGS84 → GCJ02，消除高德地图坐标偏差
      const [gcjLng, gcjLat] = wgs84ToGcj02(pos.lng, pos.lat)
      const lnglat = [gcjLng, gcjLat]
      const title  = `${pos.deviceName}\n速度: ${pos.speed.toFixed(1)} km/h`

      const color    = pos.online ? '#1677ff' : '#8c8c8c'
      // 固定 20×20 圆点，offset(-10,-10) 使圆心精确落在坐标点，避免 CSS transform 被 AMap 容器裁剪
      const dotHTML  = `<div style="width:20px;height:20px;border-radius:50%;background:${color};border:3px solid #fff;box-shadow:0 2px 8px rgba(0,0,0,0.45);cursor:pointer"></div>`
      const labelHTML = `<div style="background:${color};color:#fff;padding:3px 10px;border-radius:10px;font-size:12px;font-weight:600;white-space:nowrap;box-shadow:0 2px 6px rgba(0,0,0,0.3)">${pos.deviceName}</div>`

      if (markersRef.current[pos.deviceCode]) {
        markersRef.current[pos.deviceCode].setPosition(lnglat)
        markersRef.current[pos.deviceCode].setContent(dotHTML)
        markersRef.current[pos.deviceCode].setLabel({ content: labelHTML, direction: 'top' })
      } else {
        const marker = new AMap.Marker({
          map: mapInstance.current,   // 直接在构造时挂载，比 setMap() 更可靠
          position: lnglat,
          content: dotHTML,
          offset: new AMap.Pixel(-10, -10),  // 圆点居中
          label: { content: labelHTML, direction: 'top' },
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
            anchor: 'bottom-center',
            offset: new AMap.Pixel(0, -14),
          })
          info.open(mapInstance.current, marker.getPosition())
        })
        markersRef.current[pos.deviceCode] = marker
      }
    })

    // 首次有设备数据时定位到第一个设备；之后不干扰用户的缩放/平移
    if (!fitViewDone.current && validData.length > 0) {
      fitViewDone.current = true
      const [lng0, lat0] = wgs84ToGcj02(validData[0].lng, validData[0].lat)
      mapInstance.current.setZoomAndCenter(14, [lng0, lat0])
    }
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
