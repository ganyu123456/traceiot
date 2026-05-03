import request from '@/utils/request'

export interface LoginParams {
  userName: string
  password: string
}

export interface LoginResult {
  token: string
  userName: string
  email: string
  roles: string[]
  expiresAt: string
}

export const authApi = {
  login: (params: LoginParams): Promise<LoginResult> =>
    request.post('/auth/login', params),

  getProfile: () => request.get('/auth/profile'),
}
