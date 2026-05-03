import request from '@/utils/request'
import { PagedResult } from './device'

export interface AlarmRecord {
  id: string
  deviceId: string
  deviceCode: string
  deviceName?: string
  alarmType: 1 | 2 | 3
  alarmTypeName: string
  alarmValue?: number
  lat?: number
  lng?: number
  triggeredAt: string
  handledAt?: string
  isHandled: boolean
  handlerNote?: string
}

export const alarmApi = {
  getList: (params?: {
    deviceId?: string
    alarmType?: number
    isHandled?: boolean
    startTime?: string
    endTime?: string
    skipCount?: number
    maxResultCount?: number
  }) => request.get<any, PagedResult<AlarmRecord>>('/alarms', { params }),

  handle: (id: string, note?: string) =>
    request.put<any, AlarmRecord>(`/alarms/${id}/handle`, { note }),

  getUnhandledCount: () => request.get<any, number>('/alarms/unhandled-count'),
}
