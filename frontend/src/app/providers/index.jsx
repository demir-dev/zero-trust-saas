import { createContext, useContext } from 'react'
import { BrowserRouter } from 'react-router-dom'
import { ThemeProvider, CssBaseline } from '@mui/material'
import { QueryClient, QueryClientProvider, useQuery } from '@tanstack/react-query'
import { AuthProvider } from '../../features/auth/store/authStore'
import theme from '../../shared/theme/theme'
import api from '../../shared/api/axiosInstance'
import SplashScreen from '../../shared/components/SplashScreen'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      staleTime: 30_000,
    },
  },
})

const PlatformStatusContext = createContext({ isInitialized: true, isLoading: false })

export function usePlatformStatus() {
  return useContext(PlatformStatusContext)
}

function PlatformStatusProvider({ children }) {
  const { data, isLoading } = useQuery({
    queryKey: ['platform', 'status'],
    queryFn: () => api.get('/platform/status').then((r) => r.data),
    staleTime: Infinity,
    retry: 1,
    gcTime: Infinity,
  })

  if (isLoading) return <SplashScreen />

  return (
    <PlatformStatusContext.Provider
      value={{ isInitialized: data?.isInitialized ?? true, isLoading }}
    >
      {children}
    </PlatformStatusContext.Provider>
  )
}

export default function Providers({ children }) {
  return (
    <BrowserRouter>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <QueryClientProvider client={queryClient}>
          <PlatformStatusProvider>
            <AuthProvider>
              {children}
            </AuthProvider>
          </PlatformStatusProvider>
        </QueryClientProvider>
      </ThemeProvider>
    </BrowserRouter>
  )
}
