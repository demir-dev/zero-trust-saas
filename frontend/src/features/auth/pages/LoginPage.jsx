import { useState } from 'react'
import { useNavigate, Link as RouterLink } from 'react-router-dom'
import {
  Box, Card, CardContent, TextField, Button, Typography,
  InputAdornment, IconButton, Link, CircularProgress, Divider
} from '@mui/material'
import { Visibility, VisibilityOff, Shield as ShieldIcon, Lock as LockIcon } from '@mui/icons-material'
import { motion } from 'framer-motion'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useAuth } from '../store/authStore'
import ProblemAlert from '../../../shared/components/ProblemAlert'

const schema = z.object({
  email: z.string().email('Invalid email'),
  password: z.string().min(1, 'Password is required'),
  tenantId: z.string().uuid('Must be a valid tenant UUID'),
  deviceFingerprint: z.string().optional(),
  country: z.string().optional(),
  browser: z.string().optional(),
  operatingSystem: z.string().optional(),
})

export default function LoginPage() {
  const navigate = useNavigate()
  const { login } = useAuth()
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState(null)
  const [loading, setLoading] = useState(false)

  const { register, handleSubmit, formState: { errors } } = useForm({
    resolver: zodResolver(schema),
    defaultValues: {
      deviceFingerprint: navigator.userAgent.substring(0, 50),
      country: 'Unknown',
      browser: navigator.userAgent.split(' ').slice(-1)[0] || 'Unknown',
      operatingSystem: navigator.platform || 'Unknown',
    },
  })

  const onSubmit = async (data) => {
    setLoading(true)
    setError(null)
    try {
      await login(data)
      navigate('/dashboard')
    } catch (err) {
      setError(err)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Box
      sx={{
        minHeight: '100vh',
        bgcolor: 'background.default',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        p: 2,
        background: 'radial-gradient(ellipse at 50% 0%, rgba(99,102,241,0.12) 0%, transparent 70%)',
      }}
    >
      <motion.div
        initial={{ opacity: 0, y: 32 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4, ease: 'easeOut' }}
        style={{ width: '100%', maxWidth: 420 }}
      >
        <Box sx={{ textAlign: 'center', mb: 3 }}>
          <Box sx={{ display: 'inline-flex', p: 1.5, borderRadius: 3, bgcolor: 'rgba(99,102,241,0.12)', mb: 2 }}>
            <ShieldIcon sx={{ color: 'primary.main', fontSize: 36 }} />
          </Box>
          <Typography variant="h4" sx={{ fontWeight: 700 }}>ZeroTrust SaaS</Typography>
          <Typography variant="body2" sx={{ color: 'text.secondary', mt: 0.5 }}>
            Secure access to your admin portal
          </Typography>
        </Box>

        <Card>
          <CardContent sx={{ p: 3 }}>
            <Typography variant="h6" sx={{ mb: 0.5, fontWeight: 600 }}>Sign In</Typography>
            <Typography variant="body2" sx={{ color: 'text.secondary', mb: 3 }}>
              Enter your credentials to continue
            </Typography>

            <ProblemAlert error={error} />

            <Box component="form" onSubmit={handleSubmit(onSubmit)} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <TextField
                label="Tenant ID"
                placeholder="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
                {...register('tenantId')}
                error={!!errors.tenantId}
                helperText={errors.tenantId?.message}
                size="small"
              />
              <TextField
                label="Email"
                type="email"
                {...register('email')}
                error={!!errors.email}
                helperText={errors.email?.message}
                size="small"
              />
              <TextField
                label="Password"
                type={showPassword ? 'text' : 'password'}
                {...register('password')}
                error={!!errors.password}
                helperText={errors.password?.message}
                size="small"
                InputProps={{
                  endAdornment: (
                    <InputAdornment position="end">
                      <IconButton size="small" onClick={() => setShowPassword((p) => !p)}>
                        {showPassword ? <VisibilityOff fontSize="small" /> : <Visibility fontSize="small" />}
                      </IconButton>
                    </InputAdornment>
                  ),
                }}
              />

              <Button
                type="submit"
                variant="contained"
                size="large"
                fullWidth
                disabled={loading}
                startIcon={loading ? <CircularProgress size={16} color="inherit" /> : <LockIcon />}
                sx={{ mt: 1 }}
              >
                {loading ? 'Signing in…' : 'Sign In'}
              </Button>
            </Box>

            <Divider sx={{ my: 2.5 }} />

            <Typography variant="body2" sx={{ textAlign: 'center', color: 'text.secondary' }}>
              No account?{' '}
              <Link component={RouterLink} to="/register" sx={{ fontWeight: 600 }}>
                Register here
              </Link>
            </Typography>
          </CardContent>
        </Card>
      </motion.div>
    </Box>
  )
}
