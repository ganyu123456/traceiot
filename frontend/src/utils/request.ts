import axios, { AxiosInstance, AxiosResponse } from 'axios'
import { message } from 'antd'

const request: AxiosInstance = axios.create({
  baseURL: '/api',
  timeout: 15000,
})

// 请求拦截：注入 JWT token
request.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

// 响应拦截：统一错误处理
request.interceptors.response.use(
  (res: AxiosResponse) => res.data,
  error => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      window.location.href = '/login'
      return Promise.reject(error)
    }
    const msg = error.response?.data?.error?.message || error.message || '请求失败'
    message.error(msg)
    return Promise.reject(error)
  }
)

export default request
