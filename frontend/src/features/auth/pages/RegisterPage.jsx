import { useState } from 'react'
import { useNavigate, Link as RouterLink } from 'react-router-dom'
import {
  Box, Card, CardContent, TextField, Button, Typography,
  InputAdornment, IconButton, Link, CircularProgress, Alert
} from '@mui/material'
import { Visibility, VisibilityOff, Shield as ShieldIcon, PersonAdd as PersonAddIcon } from '@mui/icons-material'
import { motion } from 'framer-motion'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import api from '../../../shared/api/axiosInstance'
import ProblemAlert from '../../../shared/components/ProblemAlert'

const schema = z.object({
  tenantId: z.string().uuid('Must be a valid tenant UUID'),
  email: z.string().email('Invalid email'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
})

export default function RegisterPage() {
  const navigate = useNavigate()
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState(null)
  const [loading, setLoading] = useState(false)
  const [success, setSuccess] = useState(false)

  const { register, handleSubmit, formState: { errors } } = useForm({ resolver: zodResolver(schema) })

  const onSubmit = async (data) => {
    setLoading(true)
    setError(null)
    try {
      await api.post('/auth/register', data)
      setSuccess(true)
      setTimeout(() => navigate('/login'), 2000)
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
        background: 'radial-gradient(ellipse at 50% 0%, rgba(34,211,238,0.08) 0%, transparent 70%)',
      }}
    >
      <motion.div
        initial={{ opacity: 0, y: 32 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.4, ease: 'easeOut' }}
        style={{ width: '100%', maxWidth: 420 }}
      >
        <Box sx={{ textAlign: 'center', mb: 3 }}>
          <Box sx={{ display: 'inline-flex', p: 1.5, borderRadius: 3, bgcolor: 'rgba(34,211,238,0.1)', mb: 2 }}>
            <ShieldIcon sx={{ color: 'secondary.main', fontSize: 36 }} />
          </Box>
          <Typography variant="h4" sx={{ fontWeight: 700 }}>Create Account</Typography>
          <Typography variant="body2" sx={{ color: 'text.secondary', mt: 0.5 }}>
            Register within an existing tenant
          </Typography>
        </Box>

        <Card>
          <CardContent sx={{ p: 3 }}>
            {success && (
              <Alert severity="success" sx={{ mb: 2, borderRadius: 2 }}>
                Account created! Redirecting to login…
              </Alert>
            )}

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
                disabled={loading || success}
                startIcon={loading ? <CircularProgress size={16} color="inherit" /> : <PersonAddIcon />}
                sx={{ mt: 1 }}
              >
                {loading ? 'Creating account…' : 'Create Account'}
              </Button>
            </Box>

            <Typography variant="body2" sx={{ textAlign: 'center', color: 'text.secondary', mt: 2.5 }}>
              Already have an account?{' '}
              <Link component={RouterLink} to="/login" sx={{ fontWeight: 600 }}>
                Sign in
              </Link>
            </Typography>
          </CardContent>
        </Card>
      </motion.div>
    </Box>
  )
}
