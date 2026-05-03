import request from '@/utils/request'

export interface TrackPoint {
  lat: number
  lng: number
  speed: number
  direction: number
  time: string
}

export interface TrackResult {
  deviceCode: string
  deviceName: string
  totalPoints: number
  points: TrackPoint[]
}

export const trackApi = {
  query: (params: {
    deviceCode: string
    startTime: string
    endTime: string
    maxPoints?: number
  }) => request.get<any, TrackResult>('/track/query', { params }),
}
