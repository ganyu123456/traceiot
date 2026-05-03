import { createBrowserRouter, Navigate } from 'react-router-dom'
import MainLayout from '@/layouts/MainLayout'
import Login from '@/pages/Login'
import Devices from '@/pages/Devices'
import DeviceGroups from '@/pages/DeviceGroups'
import RealtimeMap from '@/pages/RealtimeMap'
import TrackReplay from '@/pages/TrackReplay'
import Alarms from '@/pages/Alarms'
import Profile from '@/pages/Profile'
import AuthGuard from '@/components/AuthGuard'

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <Login />,
  },
  {
    path: '/',
    element: (
      <AuthGuard>
        <MainLayout />
      </AuthGuard>
    ),
    children: [
      { index: true, element: <Navigate to="/realtime" replace /> },
      { path: 'realtime',  element: <RealtimeMap /> },
      { path: 'devices',   element: <Devices /> },
      { path: 'groups',    element: <DeviceGroups /> },
      { path: 'track',     element: <TrackReplay /> },
      { path: 'alarms',    element: <Alarms /> },
      { path: 'profile',   element: <Profile /> },
    ],
  },
])
