import request from '@/utils/request'

export interface DeviceGroup {
  id: string
  name: string
  description?: string
  sortOrder: number
  deviceCount: number
}

export interface Device {
  id: string
  deviceCode: string
  deviceName: string
  groupId?: string
  groupName?: string
  status: 0 | 1 | 2  // 0=离线 1=在线 2=禁用
  isEnabled: boolean
  lastHeartbeatAt?: string
  lastLat?: number
  lastLng?: number
  lastSpeed?: number
  lastDirection?: number
  remark?: string
  overspeedThreshold: number
  offlineTimeoutSec: number
  geofenceEnabled: boolean
  creationTime: string
}

export interface PagedResult<T> {
  totalCount: number
  items: T[]
}

export const deviceGroupApi = {
  getList: (params?: { filter?: string; skipCount?: number; maxResultCount?: number }) =>
    request.get<any, PagedResult<DeviceGroup>>('/device-groups', { params }),
  getAll: () => request.get<any, DeviceGroup[]>('/device-groups/all'),
  create: (data: { name: string; description?: string; sortOrder?: number }) =>
    request.post<any, DeviceGroup>('/device-groups', data),
  update: (id: string, data: { name: string; description?: string; sortOrder?: number }) =>
    request.put<any, DeviceGroup>(`/device-groups/${id}`, data),
  delete: (id: string) => request.delete(`/device-groups/${id}`),
}

export const deviceApi = {
  getList: (params?: {
    filter?: string
    groupId?: string
    status?: number
    isEnabled?: boolean
    skipCount?: number
    maxResultCount?: number
  }) => request.get<any, PagedResult<Device>>('/devices', { params }),

  get: (id: string) => request.get<any, Device>(`/devices/${id}`),

  create: (data: {
    deviceCode: string
    deviceName: string
    groupId?: string
    remark?: string
    overspeedThreshold?: number
    offlineTimeoutSec?: number
  }) => request.post<any, Device>('/devices', data),

  update: (id: string, data: {
    deviceName: string
    groupId?: string
    remark?: string
    overspeedThreshold?: number
    offlineTimeoutSec?: number
    geofenceEnabled?: boolean
  }) => request.put<any, Device>(`/devices/${id}`, data),

  delete: (id: string) => request.delete(`/devices/${id}`),
  enable:  (id: string) => request.put<any, Device>(`/devices/${id}/enable`),
  disable: (id: string) => request.put<any, Device>(`/devices/${id}/disable`),
  bindGroup: (id: string, groupId?: string) =>
    request.put<any, Device>(`/devices/${id}/bind-group`, null, { params: { groupId } }),
}
