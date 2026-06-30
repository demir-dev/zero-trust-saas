import { useState } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import {
  Box, Card, CardContent, TextField, Button, Typography,
  InputAdornment, IconButton, CircularProgress, Divider, Alert,
  FormControlLabel, Checkbox,
} from '@mui/material'
import {
  Visibility, VisibilityOff, Shield as ShieldIcon,
  Lock as LockIcon, Domain as DomainIcon,
} from '@mui/icons-material'
import { motion, AnimatePresence } from 'framer-motion'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useAuth } from '../store/authStore'
import ProblemAlert from '../../../shared/components/ProblemAlert'

const credSchema = z.object({
  email: z.string().email('Invalid email address'),
  password: z.string().min(1, 'Password is required'),
  rememberMe: z.boolean().optional(),
})

const slugSchema = z.object({
  tenantSlug: z.string().min(1, 'Organization slug is required'),
})

const slideVariants = {
  enter: { x: 40, opacity: 0 },
  center: { x: 0, opacity: 1 },
  exit: { x: -40, opacity: 0 },
}

export default function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { login, loginWithTenant } = useAuth()
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState(null)
  const [loading, setLoading] = useState(false)
  const [phase, setPhase] = useState(0) // 0=credentials, 1=org-slug
  const [savedCreds, setSavedCreds] = useState(null)

  const initMessage = location.state?.message

  const credForm = useForm({
    resolver: zodResolver(credSchema),
    defaultValues: { email: '', password: '', rememberMe: false },
  })

  const slugForm = useForm({
    resolver: zodResolver(slugSchema),
    defaultValues: { tenantSlug: '' },
  })

  const deviceInfo = {
    deviceFingerprint: navigator.userAgent.substring(0, 50),
    country: 'Unknown',
    browser: navigator.userAgent.split(' ').slice(-1)[0] || 'Unknown',
    operatingSystem: navigator.platform || 'Unknown',
  }

  const onCredentialsSubmit = async (data) => {
    setLoading(true)
    setError(null)
    try {
      const result = await login(data.email, data.password, deviceInfo)
      if (result.result === 'InvalidCredentials') {
        setError({ response: { data: { title: 'Invalid credentials', detail: 'The email or password you entered is incorrect.' } } })
        return
      }
      if (result.result === 'MfaRequired') {
        setError({ response: { data: { title: 'MFA required', detail: 'Multi-factor authentication is required. Please contact your administrator.' } } })
        return
      }
      const { isPlatformUser, hasTenantContext } = window.__authContextRef ?? {}
      // Navigation is handled via auth context change, but we need to check here
      // We re-read from the updated auth store via the returned token
      const { parseJwtClaims } = await import('../../../shared/utils/jwt')
      const claims = parseJwtClaims(result.accessToken)
      if (claims.platformRoles?.length > 0) {
        navigate('/platform')
      } else if (claims.tenantId) {
        navigate('/tenant')
      } else {
        setSavedCreds({ email: data.email, password: data.password })
        setPhase(1)
      }
    } catch (err) {
      setError(err)
    } finally {
      setLoading(false)
    }
  }

  const onSlugSubmit = async (data) => {
    setLoading(true)
    setError(null)
    try {
      const result = await loginWithTenant(
        savedCreds.email, savedCreds.password, data.tenantSlug.trim(), deviceInfo
      )
      if (result.result === 'InvalidCredentials') {
        setError({ response: { data: { title: 'Invalid credentials', detail: 'The email, password, or organization slug is incorrect.' } } })
        return
      }
      navigate('/tenant')
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
        background: 'radial-gradient(ellipse at 50% 0%, rgba(99,102,241,0.1) 0%, transparent 70%)',
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
          <Typography variant="h4" fontWeight={700}>ZeroTrust SaaS</Typography>
          <Typography variant="body2" color="text.secondary" mt={0.5}>
            {phase === 0 ? 'Sign in to your account' : 'Enter your organization'}
          </Typography>
        </Box>

        <Card>
          <CardContent sx={{ p: 3 }}>
            {initMessage && (
              <Alert severity="success" sx={{ mb: 2 }}>{initMessage}</Alert>
            )}

            <ProblemAlert error={error} />

            <AnimatePresence mode="wait">
              {phase === 0 && (
                <motion.div
                  key="creds"
                  variants={slideVariants}
                  initial="enter"
                  animate="center"
                  exit="exit"
                  transition={{ duration: 0.2 }}
                >
                  <Typography variant="h6" fontWeight={600} mb={0.5}>Sign In</Typography>
                  <Typography variant="body2" color="text.secondary" mb={2.5}>
                    Enter your credentials to continue
                  </Typography>

                  <Box
                    component="form"
                    onSubmit={credForm.handleSubmit(onCredentialsSubmit)}
                    sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}
                  >
                    <TextField
                      label="Email"
                      type="email"
                      size="small"
                      {...credForm.register('email')}
                      error={!!credForm.formState.errors.email}
                      helperText={credForm.formState.errors.email?.message}
                      autoComplete="email"
                      autoFocus
                    />
                    <TextField
                      label="Password"
                      type={showPassword ? 'text' : 'password'}
                      size="small"
                      {...credForm.register('password')}
                      error={!!credForm.formState.errors.password}
                      helperText={credForm.formState.errors.password?.message}
                      autoComplete="current-password"
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
                    <FormControlLabel
                      control={<Checkbox size="small" {...credForm.register('rememberMe')} />}
                      label={<Typography variant="body2">Remember me</Typography>}
                      sx={{ mt: -0.5 }}
                    />
                    <Button
                      type="submit"
                      variant="contained"
                      size="large"
                      fullWidth
                      disabled={loading}
                      startIcon={loading ? <CircularProgress size={16} color="inherit" /> : <LockIcon />}
                    >
                      {loading ? 'Signing in…' : 'Sign In'}
                    </Button>
                  </Box>
                </motion.div>
              )}

              {phase === 1 && (
                <motion.div
                  key="slug"
                  variants={slideVariants}
                  initial="enter"
                  animate="center"
                  exit="exit"
                  transition={{ duration: 0.2 }}
                >
                  <Typography variant="h6" fontWeight={600} mb={0.5}>Select Organization</Typography>
                  <Typography variant="body2" color="text.secondary" mb={2.5}>
                    Your account belongs to a tenant. Enter your organization identifier to continue.
                  </Typography>

                  <Box
                    component="form"
                    onSubmit={slugForm.handleSubmit(onSlugSubmit)}
                    sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}
                  >
                    <TextField
                      label="Organization Slug"
                      placeholder="acme-corp"
                      size="small"
                      autoFocus
                      {...slugForm.register('tenantSlug')}
                      error={!!slugForm.formState.errors.tenantSlug}
                      helperText={slugForm.formState.errors.tenantSlug?.message ?? 'Your organization identifier, e.g. acme-corp'}
                      InputProps={{
                        startAdornment: (
                          <InputAdornment position="start">
                            <DomainIcon sx={{ color: 'text.secondary', fontSize: 18 }} />
                          </InputAdornment>
                        ),
                      }}
                    />
                    <Box sx={{ display: 'flex', gap: 1.5 }}>
                      <Button
                        variant="outlined"
                        onClick={() => { setPhase(0); setError(null) }}
                        sx={{ flex: 1 }}
                      >
                        Back
                      </Button>
                      <Button
                        type="submit"
                        variant="contained"
                        disabled={loading}
                        startIcon={loading ? <CircularProgress size={16} color="inherit" /> : <DomainIcon />}
                        sx={{ flex: 2 }}
                      >
                        {loading ? 'Signing in…' : 'Continue'}
                      </Button>
                    </Box>
                  </Box>
                </motion.div>
              )}
            </AnimatePresence>

            <Divider sx={{ my: 2.5 }} />
            <Typography variant="body2" textAlign="center" color="text.secondary">
              Need access? Contact your organization administrator.
            </Typography>
          </CardContent>
        </Card>
      </motion.div>
    </Box>
  )
}
