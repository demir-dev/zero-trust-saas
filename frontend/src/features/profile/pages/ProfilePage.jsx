import { useState } from 'react'
import {
  Box, Card, CardContent, Typography, Avatar, Chip, Divider,
  Button, TextField, Alert, Stack, IconButton, Tooltip,
  Skeleton, FormControlLabel, Checkbox, Dialog, DialogTitle,
  DialogContent, DialogActions, Stepper, Step, StepLabel,
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
  ContentCopy as CopyIcon,
  Check as CheckIcon,
  QrCode2 as QrCodeIcon,
  VerifiedUser as VerifiedIcon,
  Key as KeyIcon,
} from '@mui/icons-material'
import { QRCodeSVG } from 'qrcode.react'
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

const WIZARD_STEPS = ['Scan QR Code', 'Verify Code', 'Save Recovery Codes']

function MfaSetupWizard({ open, onClose, onComplete }) {
  const [step, setStep] = useState(0)
  const [setupData, setSetupData] = useState(null)
  const [verifyCode, setVerifyCode] = useState('')
  const [recoveryCodes, setRecoveryCodes] = useState([])
  const [copied, setCopied] = useState(false)
  const [confirmed, setConfirmed] = useState(false)
  const [verifyError, setVerifyError] = useState(null)

  const setupQuery = useQuery({
    queryKey: ['mfa', 'setup'],
    queryFn: () => api.get('/auth/mfa/setup').then(r => r.data),
    enabled: open,
    staleTime: 0,
    gcTime: 0,
  })

  const verifyMutation = useMutation({
    mutationFn: ({ base32Secret, verificationCode }) =>
      api.post('/auth/mfa/verify-enable', { base32Secret, verificationCode }).then(r => r.data),
    onSuccess: (data) => {
      setRecoveryCodes(data.recoveryCodes)
      setVerifyError(null)
      setStep(2)
    },
    onError: (err) => setVerifyError(err),
  })

  const handleVerify = () => {
    if (!verifyCode.trim() || !setupQuery.data?.secret) return
    verifyMutation.mutate({
      base32Secret: setupQuery.data.secret,
      verificationCode: verifyCode.trim(),
    })
  }

  const handleCopyAll = () => {
    navigator.clipboard.writeText(recoveryCodes.join('\n'))
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  const handleDone = () => {
    onComplete()
    onClose()
  }

  const handleClose = () => {
    if (step < 2) {
      onClose()
    }
  }

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      maxWidth="sm"
      fullWidth
      PaperProps={{ sx: { borderRadius: 3 } }}
    >
      <DialogTitle sx={{ pb: 1 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          <MfaIcon sx={{ color: 'primary.main' }} />
          <Typography variant="h6" fontWeight={600}>Set Up Authenticator App</Typography>
        </Box>
      </DialogTitle>

      <DialogContent>
        <Stepper activeStep={step} sx={{ mb: 3 }}>
          {WIZARD_STEPS.map((label) => (
            <Step key={label}><StepLabel>{label}</StepLabel></Step>
          ))}
        </Stepper>

        {step === 0 && (
          <Stack spacing={2.5} alignItems="center">
            <Typography variant="body2" color="text.secondary" textAlign="center">
              Scan this QR code with Google Authenticator, Authy, or any TOTP-compatible app.
            </Typography>

            {setupQuery.isLoading ? (
              <Skeleton variant="rectangular" width={200} height={200} sx={{ borderRadius: 2 }} />
            ) : setupQuery.data ? (
              <Box sx={{ p: 2, bgcolor: '#fff', borderRadius: 2, boxShadow: 1 }}>
                <QRCodeSVG value={setupQuery.data.qrCodeUri} size={200} />
              </Box>
            ) : null}

            <Box sx={{ width: '100%' }}>
              <Typography variant="caption" color="text.secondary" display="block" mb={0.5}>
                Or enter this key manually:
              </Typography>
              <Box
                sx={{
                  fontFamily: 'monospace',
                  fontSize: '0.8rem',
                  letterSpacing: 2,
                  bgcolor: 'action.hover',
                  p: 1.5,
                  borderRadius: 1.5,
                  wordBreak: 'break-all',
                  color: 'text.primary',
                }}
              >
                {setupQuery.data?.secret ?? '—'}
              </Box>
            </Box>
          </Stack>
        )}

        {step === 1 && (
          <Stack spacing={2.5}>
            <Typography variant="body2" color="text.secondary">
              Enter the 6-digit code from your authenticator app to confirm the setup.
            </Typography>
            <ProblemAlert error={verifyError} />
            <TextField
              label="Verification Code"
              value={verifyCode}
              onChange={(e) => setVerifyCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
              inputProps={{ maxLength: 6, inputMode: 'numeric' }}
              placeholder="123456"
              autoFocus
              fullWidth
              size="small"
              onKeyDown={(e) => e.key === 'Enter' && handleVerify()}
            />
          </Stack>
        )}

        {step === 2 && (
          <Stack spacing={2.5}>
            <Alert severity="warning" icon={<KeyIcon />} sx={{ borderRadius: 2 }}>
              Save these recovery codes now. They will not be shown again. Each code can only be used once.
            </Alert>

            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: '1fr 1fr',
                gap: 1,
                p: 2,
                bgcolor: 'action.hover',
                borderRadius: 2,
              }}
            >
              {recoveryCodes.map((code) => (
                <Typography
                  key={code}
                  sx={{ fontFamily: 'monospace', fontSize: '0.875rem', letterSpacing: 1 }}
                >
                  {code}
                </Typography>
              ))}
            </Box>

            <Button
              variant="outlined"
              startIcon={copied ? <CheckIcon /> : <CopyIcon />}
              onClick={handleCopyAll}
              color={copied ? 'success' : 'inherit'}
              size="small"
            >
              {copied ? 'Copied!' : 'Copy All Codes'}
            </Button>

            <FormControlLabel
              control={
                <Checkbox
                  checked={confirmed}
                  onChange={(e) => setConfirmed(e.target.checked)}
                  color="primary"
                />
              }
              label={
                <Typography variant="body2">
                  I have saved my recovery codes in a safe place
                </Typography>
              }
            />
          </Stack>
        )}
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2.5 }}>
        {step === 0 && (
          <>
            <Button onClick={onClose} color="inherit">Cancel</Button>
            <Button
              variant="contained"
              onClick={() => setStep(1)}
              disabled={!setupQuery.data}
              startIcon={<QrCodeIcon />}
            >
              I've Scanned the Code
            </Button>
          </>
        )}

        {step === 1 && (
          <>
            <Button onClick={() => setStep(0)} color="inherit">Back</Button>
            <Button
              variant="contained"
              onClick={handleVerify}
              disabled={verifyMutation.isPending || verifyCode.length < 6}
              startIcon={<VerifiedIcon />}
            >
              {verifyMutation.isPending ? 'Verifying…' : 'Verify'}
            </Button>
          </>
        )}

        {step === 2 && (
          <Button
            variant="contained"
            onClick={handleDone}
            disabled={!confirmed}
            fullWidth
          >
            Done — MFA Is Now Active
          </Button>
        )}
      </DialogActions>
    </Dialog>
  )
}

function MfaSection({ me, loading }) {
  const queryClient = useQueryClient()
  const [wizardOpen, setWizardOpen] = useState(false)
  const [confirmDisableOpen, setConfirmDisableOpen] = useState(false)

  const isMfaEnabled = me?.isMfaEnabled ?? false

  const disableMutation = useMutation({
    mutationFn: () => api.post('/auth/mfa/disable', { userId: me?.id }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users', 'me'] })
      setConfirmDisableOpen(false)
    },
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
            {isMfaEnabled ? (
              <Alert severity="success" sx={{ mb: 2, borderRadius: 2 }}>
                MFA is enabled on your account (TOTP authenticator app).
              </Alert>
            ) : (
              <Alert severity="warning" sx={{ mb: 2, borderRadius: 2 }}>
                MFA is not enabled. Enable it to protect your account with a second factor.
              </Alert>
            )}

            {!isMfaEnabled && (
              <Button
                variant="contained"
                startIcon={<QrCodeIcon />}
                onClick={() => setWizardOpen(true)}
              >
                Set Up Authenticator App
              </Button>
            )}

            {isMfaEnabled && (
              <Button
                variant="outlined"
                color="error"
                onClick={() => setConfirmDisableOpen(true)}
              >
                Disable MFA
              </Button>
            )}

            <MfaSetupWizard
              open={wizardOpen}
              onClose={() => setWizardOpen(false)}
              onComplete={() => queryClient.invalidateQueries({ queryKey: ['users', 'me'] })}
            />

            <ConfirmDialog
              open={confirmDisableOpen}
              title="Disable MFA?"
              message="This will remove multi-factor authentication from your account. All active sessions will be revoked."
              onConfirm={() => disableMutation.mutate()}
              onCancel={() => setConfirmDisableOpen(false)}
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
