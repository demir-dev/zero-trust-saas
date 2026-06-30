import { useState } from 'react'
import {
  Box, Card, CardContent, Typography, Avatar, Chip, Divider,
  Button, TextField, Alert, Stack, Grid, IconButton, Tooltip,
  Skeleton, Switch, FormControlLabel,
} from '@mui/material'
import {
  Person as PersonIcon,
  Email as EmailIcon,
  PhonelinkLock as MfaIcon,
  Devices as DevicesIcon,
  Block as BlockIcon,
  RemoveCircle as RevokeIcon,
  ArrowBack as BackIcon,
  Computer, Language, LocationOn,
} from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import api from '../../../shared/api/axiosInstance'
import PageHeader from '../../../shared/components/PageHeader'
import StatusChip from '../../../shared/components/StatusChip'
import ConfirmDialog from '../../../shared/components/ConfirmDialog'
import ProblemAlert from '../../../shared/components/ProblemAlert'
import { useAuth } from '../../auth/store/authStore'

function useCurrentUser() {
  return useQuery({
    queryKey: ['users', 'me'],
    queryFn: () => api.get('/users/me').then(r => r.data),
  })
}

function useDevices() {
  return useQuery({
    queryKey: ['profile', 'devices'],
    queryFn: () => api.get('/devices').then(r => r.data),
  })
}

function AccountSection({ me, loading }) {
  return (
    <Card>
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
          <PersonIcon sx={{ color: 'primary.main' }} />
          <Typography variant="h6" fontWeight={600}>Account Information</Typography>
        </Box>

        {loading ? (
          <>
            <Skeleton variant="circular" width={64} height={64} sx={{ mb: 2 }} />
            <Skeleton width="50%" /><Skeleton width="70%" />
          </>
        ) : me ? (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
              <Avatar sx={{ width: 64, height: 64, bgcolor: 'primary.main', fontSize: '1.5rem' }}>
                {me.displayName?.charAt(0)?.toUpperCase() ?? 'U'}
              </Avatar>
              <Box>
                <Typography variant="h6" fontWeight={600}>{me.displayName ?? '—'}</Typography>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                  <EmailIcon sx={{ fontSize: 14, color: 'text.secondary' }} />
                  <Typography variant="body2" color="text.secondary">{me.email}</Typography>
                </Box>
              </Box>
            </Box>

            <Divider />

            {[
              { label: 'Status', value: <StatusChip status={me.status} /> },
              { label: 'Email Verified', value: <Chip label={me.isEmailConfirmed ? 'Verified' : 'Unverified'} size="small" color={me.isEmailConfirmed ? 'success' : 'warning'} /> },
              { label: 'Registered', value: me.registeredAtUtc ? new Date(me.registeredAtUtc).toLocaleDateString() : '—' },
              { label: 'Last Login', value: me.lastLoginUtc ? new Date(me.lastLoginUtc).toLocaleString() : 'Never' },
            ].map(({ label, value }) => (
              <Box key={label} sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <Typography variant="body2" color="text.secondary">{label}</Typography>
                {typeof value === 'string'
                  ? <Typography variant="body2" fontWeight={500}>{value}</Typography>
                  : value}
              </Box>
            ))}
          </Box>
        ) : null}
      </CardContent>
    </Card>
  )
}

function MfaSection({ me, loading }) {
  const queryClient = useQueryClient()
  const [secret, setSecret] = useState('')
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [error, setError] = useState(null)

  const isMfaEnabled = me?.isMfaEnabled ?? false

  const enableMutation = useMutation({
    mutationFn: (data) => api.post('/auth/mfa/enable', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users', 'me'] })
      setError(null)
      setSecret('')
    },
    onError: setError,
  })

  const disableMutation = useMutation({
    mutationFn: () => api.post('/auth/mfa/disable', { userId: me?.id }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users', 'me'] })
      setConfirmOpen(false)
      setError(null)
    },
    onError: setError,
  })

  return (
    <Card>
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
          <MfaIcon sx={{ color: 'primary.main' }} />
          <Typography variant="h6" fontWeight={600}>Multi-Factor Authentication</Typography>
        </Box>

        {loading ? <Skeleton height={80} /> : (
          <>
            <ProblemAlert error={error} />

            {isMfaEnabled ? (
              <Alert severity="success" sx={{ mb: 2, borderRadius: 2 }}>
                MFA is enabled on your account ({me.mfaMethod}).
              </Alert>
            ) : (
              <Alert severity="warning" sx={{ mb: 2, borderRadius: 2 }}>
                MFA is not enabled. Add it for stronger account security.
              </Alert>
            )}

            {!isMfaEnabled && (
              <Stack spacing={2}>
                <TextField
                  label="TOTP Secret"
                  value={secret}
                  onChange={(e) => setSecret(e.target.value)}
                  placeholder="Base32 TOTP secret from your authenticator app"
                  size="small"
                  helperText="Generate a TOTP secret from your authenticator app (e.g. Google Authenticator, Authy)"
                />
                <Button
                  variant="contained"
                  onClick={() => enableMutation.mutate({ userId: me?.id, method: 1, secret })}
                  disabled={enableMutation.isPending || !me?.id || !secret}
                >
                  {enableMutation.isPending ? 'Enabling…' : 'Enable TOTP MFA'}
                </Button>
              </Stack>
            )}

            {isMfaEnabled && (
              <Button variant="outlined" color="error" onClick={() => setConfirmOpen(true)}>
                Disable MFA
              </Button>
            )}

            <ConfirmDialog
              open={confirmOpen}
              title="Disable MFA?"
              message="This will remove multi-factor authentication from your account. All active sessions may be revoked."
              onConfirm={() => disableMutation.mutate()}
              onCancel={() => setConfirmOpen(false)}
            />
          </>
        )}
      </CardContent>
    </Card>
  )
}

function DevicesSection() {
  const queryClient = useQueryClient()
  const { data: devices, isLoading } = useDevices()

  const revokeMutation = useMutation({
    mutationFn: (id) => api.post(`/devices/${id}/revoke`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['profile', 'devices'] }),
  })

  const blockMutation = useMutation({
    mutationFn: (id) => api.post(`/devices/${id}/block`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['profile', 'devices'] }),
  })

  return (
    <Card>
      <CardContent>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
          <DevicesIcon sx={{ color: 'primary.main' }} />
          <Typography variant="h6" fontWeight={600}>Trusted Devices</Typography>
        </Box>

        {isLoading ? (
          <Stack spacing={1}>{[...Array(3)].map((_, i) => <Skeleton key={i} height={64} sx={{ borderRadius: 2 }} />)}</Stack>
        ) : !devices?.length ? (
          <Typography variant="body2" color="text.secondary" textAlign="center" py={3}>
            No devices registered. Trust a device on next login.
          </Typography>
        ) : (
          <Stack spacing={1}>
            {devices.map((d) => (
              <Box
                key={d.id}
                sx={{
                  display: 'flex', alignItems: 'center', gap: 2,
                  px: 2, py: 1.5,
                  bgcolor: 'rgba(148,163,184,0.04)',
                  borderRadius: 2,
                  border: '1px solid',
                  borderColor: d.status === 'Trusted' ? 'rgba(34,197,94,0.15)' : d.status === 'Blocked' ? 'rgba(239,68,68,0.15)' : 'divider',
                }}
              >
                <Computer sx={{ color: 'text.secondary', flexShrink: 0 }} />
                <Box sx={{ flex: 1, minWidth: 0 }}>
                  <Typography variant="body2" fontWeight={600} noWrap>{d.name}</Typography>
                  <Box sx={{ display: 'flex', gap: 1.5, mt: 0.25, flexWrap: 'wrap' }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.4 }}>
                      <Language sx={{ fontSize: 12, color: 'text.disabled' }} />
                      <Typography variant="caption" color="text.disabled" sx={{ fontFamily: 'monospace' }}>{d.ipAddress}</Typography>
                    </Box>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.4 }}>
                      <LocationOn sx={{ fontSize: 12, color: 'text.disabled' }} />
                      <Typography variant="caption" color="text.disabled">{d.country}</Typography>
                    </Box>
                  </Box>
                </Box>
                <StatusChip status={d.status} />
                <Box sx={{ display: 'flex', gap: 0.5 }}>
                  {d.status !== 'Revoked' && (
                    <Tooltip title="Revoke">
                      <IconButton size="small" color="warning" onClick={() => revokeMutation.mutate(d.id)}>
                        <RevokeIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}
                  {d.status !== 'Blocked' && d.status !== 'Revoked' && (
                    <Tooltip title="Block">
                      <IconButton size="small" color="error" onClick={() => blockMutation.mutate(d.id)}>
                        <BlockIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  )}
                </Box>
              </Box>
            ))}
          </Stack>
        )}
      </CardContent>
    </Card>
  )
}

export default function ProfilePage() {
  const navigate = useNavigate()
  const { isPlatformUser, hasTenantContext } = useAuth()
  const { data: me, isLoading } = useCurrentUser()

  const backPath = isPlatformUser ? '/platform' : hasTenantContext ? '/tenant' : '/login'
  const backLabel = isPlatformUser ? 'Platform Dashboard' : 'Tenant Dashboard'

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default', p: { xs: 2, md: 4 } }}>
      <Box sx={{ maxWidth: 800, mx: 'auto' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 3 }}>
          <Button
            startIcon={<BackIcon />}
            onClick={() => navigate(backPath)}
            variant="text"
            color="inherit"
            size="small"
          >
            Back to {backLabel}
          </Button>
        </Box>

        <PageHeader
          title="Profile & Security"
          subtitle="Manage your account, MFA, and trusted devices"
        />

        <Stack spacing={3}>
          <AccountSection me={me} loading={isLoading} />
          <MfaSection me={me} loading={isLoading} />
          <DevicesSection />
        </Stack>
      </Box>
    </Box>
  )
}
