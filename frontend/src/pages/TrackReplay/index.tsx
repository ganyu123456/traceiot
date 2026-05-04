import { useEffect, useRef, useState } from 'react'
import {
  Card, Form, Select, DatePicker, Button, Space, Statistic, Row, Col,
  Typography, Alert, Slider, Tag, Spin
} from 'antd'
import { SearchOutlined, PlayCircleOutlined, PauseCircleOutlined, StopOutlined } from '@ant-design/icons'
import { deviceApi, Device } from '@/api/device'
import { trackApi, TrackPoint } from '@/api/track'
import dayjs from 'dayjs'

const { Title } = Typography
const { RangePicker } = DatePicker

const AMAP_KEY           = '57a5e349af4ba24b6e204e299d91c332'
const AMAP_SECURITY_CODE = 'f8361b07700c70840d85b27ea0bbe6d5'

function loadAMap(): Promise<void> {
  return new Promise((resolve, reject) => {
    if (window.AMap) { resolve(); return }
    window._AMapSecurityConfig = { securityJsCode: AMAP_SECURITY_CODE }
    const script = document.createElement('script')
    script.src = `https://webapi.amap.com/maps?v=2.0&key=${AMAP_KEY}`
    script.onload  = () => resolve()
    script.onerror = reject
    document.head.appendChild(script)
  })
}

export default function TrackReplay() {
  const [form]           = Form.useForm()
  const mapRef           = useRef<HTMLDivElement>(null)
  const mapInstance      = useRef<any>(null)
  const polylineRef      = useRef<any>(null)
  const markerRef        = useRef<any>(null)
  const animTimerRef     = useRef<NodeJS.Timeout | null>(null)

  const [devices, setDevices]   = useState<Device[]>([])
  const [points, setPoints]     = useState<TrackPoint[]>([])
  const [loading, setLoading]   = useState(false)
  const [mapReady, setMapReady] = useState(false)
  const [playing, setPlaying]   = useState(false)
  const [playIdx, setPlayIdx]   = useState(0)
  const [playSpeed, setPlaySpeed] = useState(5) // 每步 interval ms

  useEffect(() => {
    deviceApi.getList({ maxResultCount: 200 }).then(res => setDevices(res.items))
    loadAMap().then(() => {
      if (!mapRef.current || mapInstance.current) return
      mapInstance.current = new window.AMap.Map(mapRef.current, {
        zoom: 10, center: [116.4074, 39.9042]
      })
      setMapReady(true)
    })
    return () => {
      if (animTimerRef.current) clearInterval(animTimerRef.current)
      if (mapInstance.current) mapInstance.current.destroy()
    }
  }, [])

  const handleQuery = async () => {
    const values = await form.validateFields()
    const [start, end] = values.timeRange
    setLoading(true)
    stopPlay()
    try {
      const res = await trackApi.query({
        deviceCode: values.deviceCode,
        startTime:  start.toISOString(),
        endTime:    end.toISOString(),
        maxPoints:  5000,
      })
      setPoints(res.points)
      setPlayIdx(0)
      drawTrack(res.points)
    } finally {
      setLoading(false)
    }
  }

  const drawTrack = (pts: TrackPoint[]) => {
    if (!mapReady || !mapInstance.current || !pts.length) return
    const AMap = window.AMap

    // 清除旧轨迹
    if (polylineRef.current) { polylineRef.current.setMap(null) }
    if (markerRef.current)   { markerRef.current.setMap(null) }

    const path = pts.map(p => [p.lng, p.lat])

    // 轨迹线
    polylineRef.current = new AMap.Polyline({
      path, strokeColor: '#1677ff', strokeWeight: 4, strokeOpacity: 0.8
    })
    polylineRef.current.setMap(mapInstance.current)

    // 起点终点标记
    new AMap.Marker({ position: path[0], label: { content: '<div style="background:#52c41a;color:#fff;padding:2px 6px;border-radius:4px">起点</div>' } }).setMap(mapInstance.current)
    new AMap.Marker({ position: path[path.length - 1], label: { content: '<div style="background:#ff4d4f;color:#fff;padding:2px 6px;border-radius:4px">终点</div>' } }).setMap(mapInstance.current)

    // 行驶标记
    markerRef.current = new AMap.Marker({
      position: path[0],
      icon: new AMap.Icon({ size: new AMap.Size(24, 24), image: 'https://a.amap.com/jsapi_demos/static/demo-center/icons/poi-marker-1.png', imageSize: new AMap.Size(24, 24) })
    })
    markerRef.current.setMap(mapInstance.current)

    mapInstance.current.setBounds(polylineRef.current.getBounds())
  }

  const startPlay = () => {
    if (!points.length || !mapReady) return
    setPlaying(true)
    let idx = playIdx

    animTimerRef.current = setInterval(() => {
      if (idx >= points.length - 1) {
        stopPlay()
        return
      }
      idx++
      setPlayIdx(idx)
      const p = points[idx]
      if (markerRef.current) markerRef.current.setPosition([p.lng, p.lat])
    }, Math.max(50, 500 / playSpeed))
  }

  const pausePlay = () => {
    if (animTimerRef.current) clearInterval(animTimerRef.current)
    setPlaying(false)
  }

  const stopPlay = () => {
    if (animTimerRef.current) clearInterval(animTimerRef.current)
    setPlaying(false)
    setPlayIdx(0)
    if (markerRef.current && points.length)
      markerRef.current.setPosition([points[0].lng, points[0].lat])
  }

  const currentPoint = points[playIdx]

  return (
    <div>
      <Title level={4} style={{ marginBottom: 16 }}>历史轨迹回放</Title>

      <Card style={{ marginBottom: 16 }}>
        <Form form={form} layout="inline" onFinish={handleQuery}>
          <Form.Item name="deviceCode" label="选择设备" rules={[{ required: true }]}>
            <Select placeholder="请选择设备" style={{ width: 200 }} showSearch optionFilterProp="children">
              {devices.map(d => <Select.Option key={d.deviceCode} value={d.deviceCode}>{d.deviceName}</Select.Option>)}
            </Select>
          </Form.Item>
          <Form.Item name="timeRange" label="时间范围" rules={[{ required: true }]}>
            <RangePicker showTime format="YYYY-MM-DD HH:mm:ss" />
          </Form.Item>
          <Form.Item>
            <Button type="primary" htmlType="submit" icon={<SearchOutlined />} loading={loading}>
              查询轨迹
            </Button>
          </Form.Item>
        </Form>
      </Card>

      {points.length > 0 && (
        <Row gutter={16} style={{ marginBottom: 16 }}>
          <Col span={6}><Card><Statistic title="轨迹点数" value={points.length} suffix="个点" /></Card></Col>
          <Col span={6}><Card><Statistic title="当前速度" value={currentPoint?.speed.toFixed(1) ?? '-'} suffix="km/h" /></Card></Col>
          <Col span={6}><Card><Statistic title="当前位置" value={`${currentPoint?.lat.toFixed(5) ?? '-'}, ${currentPoint?.lng.toFixed(5) ?? '-'}`} /></Card></Col>
          <Col span={6}><Card><Statistic title="当前时间" value={currentPoint ? dayjs(currentPoint.time).format('HH:mm:ss') : '-'} /></Card></Col>
        </Row>
      )}

      <Card bodyStyle={{ padding: 0 }}>
        <Spin spinning={loading}>
          <div ref={mapRef} style={{ height: 460, background: '#f5f5f5' }}>
            {!mapReady && (
              <div style={{ display:'flex', alignItems:'center', justifyContent:'center', height:'100%' }}>
                <Typography.Text type="secondary">请配置高德地图 Key</Typography.Text>
              </div>
            )}
          </div>
        </Spin>

        {points.length > 0 && (
          <div style={{ padding: '12px 24px', borderTop: '1px solid #f0f0f0' }}>
            <Row align="middle" gutter={16}>
              <Col>
                <Space>
                  {!playing
                    ? <Button type="primary" icon={<PlayCircleOutlined />} onClick={startPlay}>播放</Button>
                    : <Button icon={<PauseCircleOutlined />} onClick={pausePlay}>暂停</Button>
                  }
                  <Button icon={<StopOutlined />} onClick={stopPlay}>停止</Button>
                </Space>
              </Col>
              <Col flex="auto">
                <Slider
                  min={0} max={points.length - 1} value={playIdx}
                  onChange={idx => {
                    setPlayIdx(idx)
                    if (markerRef.current && points[idx])
                      markerRef.current.setPosition([points[idx].lng, points[idx].lat])
                  }}
                  tooltip={{ formatter: v => v !== undefined ? dayjs(points[v]?.time).format('HH:mm:ss') : '' }}
                />
              </Col>
              <Col>
                <Space>
                  <span style={{ fontSize: 12, color: '#888' }}>播放速度</span>
                  <Slider min={1} max={20} value={playSpeed} onChange={setPlaySpeed} style={{ width: 100 }} />
                  <Tag>{playSpeed}x</Tag>
                </Space>
              </Col>
            </Row>
          </div>
        )}
      </Card>
    </div>
  )
}
