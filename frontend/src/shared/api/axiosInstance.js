import axios from 'axios'

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
    const isAuthEndpoint = original?.url?.includes('/auth/login') || original?.url?.includes('/auth/register')
    if (error.response?.status === 401 && !original._retry && !isAuthEndpoint) {
      original._retry = true
      const refreshToken = localStorage.getItem('refreshToken')
      if (refreshToken) {
        try {
          const deviceFingerprint = navigator.userAgent.substring(0, 50)
          const res = await axios.post(`${BASE_URL}/auth/refresh`, {
            refreshToken,
            deviceFingerprint,
            country: 'Unknown',
            browser: navigator.userAgent.split(' ').slice(-1)[0] || 'Unknown',
            operatingSystem: navigator.platform || 'Unknown',
          })
          const { accessToken, refreshToken: newRefresh } = res.data
          sessionStorage.setItem('accessToken', accessToken)
          if (newRefresh) localStorage.setItem('refreshToken', newRefresh)
          original.headers.Authorization = `Bearer ${accessToken}`
          return api(original)
        } catch {
          sessionStorage.removeItem('accessToken')
          localStorage.removeItem('refreshToken')
          window.location.href = '/login'
        }
      } else {
        window.location.href = '/login'
      }
    }
    return Promise.reject(error)
  }
)

export default api
