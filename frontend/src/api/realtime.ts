import request from '@/utils/request'

export interface DevicePosition {
  deviceId: string
  deviceCode: string
  deviceName: string
  lat: number
  lng: number
  speed: number
  direction: number
  timestamp: number
  online: boolean
  groupName?: string
}

export interface DashboardStats {
  totalDevices: number
  onlineDevices: number
  offlineDevices: number
  disabledDevices: number
  todayAlarmCount: number
}

export const realtimeApi = {
  getAllPositions: () => request.get<any, DevicePosition[]>('/realtime/positions'),
  getPosition:    (deviceCode: string) => request.get<any, DevicePosition>(`/realtime/position/${deviceCode}`),
  getDashboard:   () => request.get<any, DashboardStats>('/realtime/dashboard'),
}
