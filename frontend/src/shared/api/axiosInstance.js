import axios from 'axios'
import { buildDeviceInfo } from '../utils/deviceInfo'
import { applyTokenExternal } from '../../features/auth/store/authStore'

const BASE_URL = import.meta.env.VITE_API_URL ?? '/api'

const api = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
})

api.interceptors.request.use((config) => {
  const token = sessionStorage.getItem('accessToken')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config
    const isAuthEndpoint =
      original?.url?.includes('/auth/login') ||
      original?.url?.includes('/auth/register') ||
      original?.url?.includes('/auth/refresh') ||
      original?.url?.includes('/setup')
    if (error.response?.status === 401 && !original._retry && !isAuthEndpoint) {
      original._retry = true
      const refreshToken = localStorage.getItem('refreshToken')
      if (refreshToken) {
        try {
          const deviceInfo = await buildDeviceInfo()
          const res = await axios.post(`${BASE_URL}/auth/refresh`, {
            refreshToken,
            ...deviceInfo,
          })
          const { accessToken, refreshToken: newRefresh } = res.data
          sessionStorage.setItem('accessToken', accessToken)
          if (newRefresh) localStorage.setItem('refreshToken', newRefresh)
          applyTokenExternal(accessToken, newRefresh ?? refreshToken)
          original.headers.Authorization = `Bearer ${accessToken}`
          return await api(original)
        } catch {
          sessionStorage.removeItem('accessToken')
          localStorage.removeItem('refreshToken')
          window.location.href = '/login?reason=expired'
        }
      } else {
        window.location.href = '/login?reason=expired'
      }
    }
    return Promise.reject(error)
  }
)

export default api
