import { useState } from 'react'
import {
  Box, Card, CardContent, Typography, Chip, IconButton, Tooltip, Button,
  Dialog, DialogTitle, DialogContent, DialogActions, TextField, Stack,
  Tabs, Tab, Menu, MenuItem, ListItemIcon, ListItemText,
  MenuItem as SelectMenuItem, Select, FormControl, InputLabel, CircularProgress,
} from '@mui/material'
import {
  Add as AddIcon,
  LockPerson as LockIcon,
  LockOpen as UnlockIcon,
  Security as MfaIcon,
  MoreVert as MoreVertIcon,
  PauseCircle as SuspendIcon,
  PlayCircle as ActivateIcon,
  LogoutOutlined as RevokeSessionsIcon,
  PhonelinkOff as DisableMfaIcon,
  Password as PasswordIcon,
  Close as CloseIcon,
  PersonOff as RevokeRoleIcon,
  Devices as DevicesIcon,
  History as AuditIcon,
  VpnKey as SessionsIcon,
  Shield as SecurityIcon,
  Person as PersonIcon,
  AdminPanelSettings as RolesIcon,
} from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, Controller } from 'react-hook-form'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import StatusChip from '../../../../shared/components/StatusChip'
import DataTable from '../../../../shared/components/DataTable'
import ConfirmDialog from '../../../../shared/components/ConfirmDialog'
import ProblemAlert from '../../../../shared/components/ProblemAlert'
import SeverityBadge from '../../../../shared/components/SeverityBadge'
import { useAuth } from '../../../auth/store/authStore'

function useUsers(page, pageSize) {
  return useQuery({
    queryKey: ['tenant', 'users', page, pageSize],
    queryFn: () => api.get(`/users?page=${page + 1}&pageSize=${pageSize}`).then(r => r.data),
  })
}

function useUserDetails(userId) {
  return useQuery({
    queryKey: ['tenant', 'users', 'detail', userId],
    queryFn: () => api.get(`/users/${userId}`).then(r => r.data),
    enabled: !!userId,
  })
}

function useUserDevices(userId) {
  return useQuery({
    queryKey: ['tenant', 'users', 'devices', userId],
    queryFn: () => api.get(`/users/${userId}/devices`).then(r => r.data),
    enabled: !!userId,
  })
}

function useUserSessions(userId) {
  return useQuery({
    queryKey: ['tenant', 'users', 'sessions', userId],
    queryFn: () => api.get(`/users/${userId}/sessions`).then(r => r.data),
    enabled: !!userId,
  })
}

function useUserAudit(userId) {
  return useQuery({
    queryKey: ['tenant', 'users', 'audit', userId],
    queryFn: () => api.get(`/users/${userId}/audit?pageSize=20`).then(r => r.data),
    enabled: !!userId,
  })
}

function useRoles(tenantId) {
  return useQuery({
    queryKey: ['tenant', 'roles', tenantId],
    queryFn: () => api.get(`/authorization/roles?tenantId=${tenantId}`).then(r => r.data),
    enabled: !!tenantId,
    staleTime: 30000,
  })
}

function fmt(d) {
  if (!d) return '—'
  return new Date(d).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' })
}

function fmtDate(d) {
  if (!d) return '—'
  return new Date(d).toLocaleDateString()
}

// ─── Tab panels ─────────────────────────────────────────────────────────────

function OverviewTab({ user }) {
  if (!user) return null
  const rows = [
    { label: 'Email', value: user.email },
    { label: 'Status', value: <StatusChip status={user.status} /> },
    { label: 'MFA', value: user.isMfaEnabled ? <Chip label={user.mfaMethod ?? 'Enabled'} size="small" color="success" /> : <Chip label="Disabled" size="small" color="default" /> },
    { label: 'Registered', value: fmtDate(user.registeredAtUtc) },
    { label: 'Last Login', value: fmt(user.lastLoginUtc) },
    { label: 'Email Verified', value: user.isEmailConfirmed ? 'Yes' : 'No' },
    { label: 'Locked Until', value: user.lockedUntilUtc ? fmt(user.lockedUntilUtc) : '—' },
  ]
  return (
    <Stack spacing={0}>
      <Box sx={{ mb: 2 }}>
        <Typography variant="h6" fontWeight={600}>{user.displayName}</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ fontFamily: 'monospace' }}>{user.email}</Typography>
      </Box>
      {rows.map(({ label, value }) => (
        <Box key={label} sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', py: 1, borderBottom: '1px solid', borderColor: 'divider' }}>
          <Typography variant="body2" color="text.secondary">{label}</Typography>
          <Box>{typeof value === 'string' ? <Typography variant="body2">{value}</Typography> : value}</Box>
        </Box>
      ))}
    </Stack>
  )
}

function RolesTab({ userId, tenantId }) {
  const queryClient = useQueryClient()
  const [assignOpen, setAssignOpen] = useState(false)
  const [revokeConfirm, setRevokeConfirm] = useState(null)
  const [selectedRoleId, setSelectedRoleId] = useState('')
  const [assignError, setAssignError] = useState(null)

  const { data: user } = useUserDetails(userId)
  const { data: roles } = useRoles(tenantId)

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['tenant', 'users', 'detail', userId] })
    queryClient.invalidateQueries({ queryKey: ['tenant', 'users'] })
  }

  const assignMutation = useMutation({
    mutationFn: (roleId) => api.post(`/users/${userId}/assign-role`, { roleId }),
    onSuccess: () => { invalidate(); setAssignOpen(false); setSelectedRoleId(''); setAssignError(null) },
    onError: (err) => setAssignError(err),
  })

  const revokeMutation = useMutation({
    mutationFn: (roleId) => api.post(`/users/${userId}/revoke-role`, { roleId }),
    onSuccess: () => { invalidate(); setRevokeConfirm(null) },
  })

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="subtitle2" color="text.secondary">Assigned Roles</Typography>
        <Button size="small" startIcon={<AddIcon />} onClick={() => { setAssignError(null); setSelectedRoleId(''); setAssignOpen(true) }}>
          Assign Role
        </Button>
      </Box>

      {!user?.roles?.length ? (
        <Typography variant="body2" color="text.secondary">No roles assigned.</Typography>
      ) : (
        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
          {user.roles.map((r) => (
            <Chip
              key={r.roleId}
              label={r.roleName}
              size="small"
              onDelete={!r.isSystem ? () => setRevokeConfirm(r) : undefined}
              deleteIcon={<RevokeRoleIcon />}
            />
          ))}
        </Box>
      )}

      <Dialog open={assignOpen} onClose={() => setAssignOpen(false)} maxWidth="xs" fullWidth PaperProps={{ sx: { borderRadius: 3 } }}>
        <DialogTitle fontWeight={700}>Assign Role</DialogTitle>
        <DialogContent>
          <ProblemAlert error={assignError} />
          <FormControl fullWidth sx={{ mt: 1 }}>
            <InputLabel>Role</InputLabel>
            <Select value={selectedRoleId} label="Role" onChange={(e) => setSelectedRoleId(e.target.value)}>
              {(roles ?? []).map((r) => (
                <SelectMenuItem key={r.id} value={r.id}>{r.name}</SelectMenuItem>
              ))}
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setAssignOpen(false)} color="inherit">Cancel</Button>
          <Button variant="contained" disabled={!selectedRoleId || assignMutation.isPending}
            onClick={() => assignMutation.mutate(selectedRoleId)}>
            {assignMutation.isPending ? 'Assigning…' : 'Assign'}
          </Button>
        </DialogActions>
      </Dialog>

      <ConfirmDialog
        open={!!revokeConfirm}
        title="Remove Role"
        message={`Remove role "${revokeConfirm?.roleName}" from this user?`}
        onConfirm={() => revokeMutation.mutate(revokeConfirm.roleId)}
        onCancel={() => setRevokeConfirm(null)}
      />
    </Box>
  )
}

function DevicesTab({ userId }) {
  const { data: devices, isLoading } = useUserDevices(userId)

  if (isLoading) return <Typography color="text.secondary">Loading…</Typography>
  if (!devices?.length) return <Typography variant="body2" color="text.secondary">No devices found.</Typography>

  return (
    <Stack spacing={1}>
      {devices.map((d) => (
        <Box key={d.id} sx={{ p: 1.5, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
              <DevicesIcon sx={{ fontSize: 18, color: 'text.secondary' }} />
              <Typography variant="body2" fontWeight={600}>{d.name}</Typography>
            </Box>
            <Chip label={d.status} size="small"
              color={d.status === 'Trusted' ? 'success' : d.status === 'Blocked' ? 'error' : d.status === 'Revoked' ? 'default' : 'warning'}
            />
          </Box>
          <Box sx={{ display: 'flex', gap: 2, mt: 0.5, flexWrap: 'wrap' }}>
            <Typography variant="caption" color="text.disabled">{d.browser} / {d.operatingSystem}</Typography>
            {d.ipAddress && <Typography variant="caption" color="text.disabled" sx={{ fontFamily: 'monospace' }}>{d.ipAddress}</Typography>}
            {d.lastSeenAtUtc && <Typography variant="caption" color="text.disabled">Last seen {fmt(d.lastSeenAtUtc)}</Typography>}
          </Box>
        </Box>
      ))}
    </Stack>
  )
}

function SessionStatusChip({ status, isExpired }) {
  if (isExpired) return <Chip label="Expired" size="small" color="default" />
  if (status === 1) return <Chip label="Active" size="small" color="success" />
  if (status === 3) return <Chip label="Revoked" size="small" color="error" />
  return <Chip label={String(status)} size="small" />
}

function SessionsTab({ userId }) {
  const queryClient = useQueryClient()
  const [revokeAllConfirm, setRevokeAllConfirm] = useState(false)
  const [revokeSessionConfirm, setRevokeSessionConfirm] = useState(null)

  const { data: sessions, isLoading } = useUserSessions(userId)

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['tenant', 'users', 'sessions', userId] })

  const revokeSessionMutation = useMutation({
    mutationFn: (sessionId) => api.post(`/users/${userId}/sessions/${sessionId}/revoke`),
    onSuccess: () => { invalidate(); setRevokeSessionConfirm(null) },
    onError: () => setRevokeSessionConfirm(null),
  })

  const revokeAllMutation = useMutation({
    mutationFn: () => api.post(`/users/${userId}/revoke-sessions`),
    onSuccess: () => { invalidate(); setRevokeAllConfirm(false) },
    onError: () => setRevokeAllConfirm(false),
  })

  if (isLoading) return <Typography color="text.secondary">Loading…</Typography>
  if (!sessions?.length) return <Typography variant="body2" color="text.secondary">No sessions found.</Typography>

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'flex-end', mb: 1.5 }}>
        <Button size="small" color="error" startIcon={<RevokeSessionsIcon />} onClick={() => setRevokeAllConfirm(true)}>
          Revoke All
        </Button>
      </Box>

      <Stack spacing={1}>
        {sessions.map((s) => {
          const isExpired = new Date(s.expiresAtUtc) <= new Date()
          const canRevoke = s.status === 1 && !isExpired
          return (
            <Box key={s.id} sx={{ p: 1.5, border: '1px solid', borderColor: 'divider', borderRadius: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Box>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                  <Typography variant="body2" fontWeight={500}>{s.browser} / {s.operatingSystem}</Typography>
                  <SessionStatusChip status={s.status} isExpired={isExpired} />
                </Box>
                <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                  {s.ipAddress && <Typography variant="caption" color="text.disabled" sx={{ fontFamily: 'monospace' }}>{s.ipAddress}</Typography>}
                  <Typography variant="caption" color="text.disabled">Created {fmt(s.createdAtUtc)}</Typography>
                  <Typography variant="caption" color="text.disabled">Expires {fmt(s.expiresAtUtc)}</Typography>
                </Box>
              </Box>
              {canRevoke && (
                <Tooltip title="Revoke this session">
                  <IconButton size="small" color="error" onClick={() => setRevokeSessionConfirm(s.id)}>
                    <RevokeSessionsIcon fontSize="small" />
                  </IconButton>
                </Tooltip>
              )}
            </Box>
          )
        })}
      </Stack>

      <ConfirmDialog
        open={!!revokeSessionConfirm}
        title="Revoke Session"
        message="Revoke this specific session? The user will be signed out on that device."
        onConfirm={() => revokeSessionMutation.mutate(revokeSessionConfirm)}
        onCancel={() => setRevokeSessionConfirm(null)}
        loading={revokeSessionMutation.isPending}
      />
      <ConfirmDialog
        open={revokeAllConfirm}
        title="Revoke All Sessions"
        message="Revoke all active sessions for this user? They will be signed out on all devices immediately."
        onConfirm={() => revokeAllMutation.mutate()}
        onCancel={() => setRevokeAllConfirm(false)}
        loading={revokeAllMutation.isPending}
      />
    </Box>
  )
}

function SecurityTab({ userId }) {
  const queryClient = useQueryClient()
  const [confirm, setConfirm] = useState(null)
  const [tempPassword, setTempPassword] = useState(null)
  const { hasPermission } = useAuth()
  const canManage = hasPermission('user.manage')

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['tenant', 'users'] })
    queryClient.invalidateQueries({ queryKey: ['tenant', 'users', 'detail', userId] })
  }

  const actionMap = {
    lock: () => api.post(`/users/${userId}/lock`),
    unlock: () => api.post(`/users/${userId}/unlock`),
    suspend: () => api.post(`/users/${userId}/suspend`),
    activate: () => api.post(`/users/${userId}/activate`),
    revokeSessions: () => api.post(`/users/${userId}/revoke-sessions`),
    disableMfa: () => api.post(`/users/${userId}/disable-mfa`),
  }

  const mutation = useMutation({
    mutationFn: ({ action }) => actionMap[action](),
    onSuccess: () => { invalidate(); setConfirm(null) },
  })

  const forceResetMutation = useMutation({
    mutationFn: () => api.post(`/users/${userId}/force-password-reset`),
    onSuccess: (res) => setTempPassword(res.data.temporaryPassword),
  })

  if (!canManage) return <Typography variant="body2" color="text.secondary">You do not have permission to manage users.</Typography>

  const actions = [
    { key: 'lock', label: 'Lock User (24h)', description: 'Temporarily lock this user for 24 hours.', icon: <LockIcon />, color: 'warning' },
    { key: 'unlock', label: 'Unlock User', description: 'Remove the temporary lock on this user.', icon: <UnlockIcon />, color: 'success' },
    { key: 'suspend', label: 'Suspend User', description: 'Suspend this user — they cannot log in.', icon: <SuspendIcon />, color: 'error' },
    { key: 'activate', label: 'Activate User', description: 'Re-activate this suspended user.', icon: <ActivateIcon />, color: 'success' },
    { key: 'revokeSessions', label: 'Revoke All Sessions', description: 'Invalidate all active sessions immediately.', icon: <RevokeSessionsIcon />, color: 'warning' },
    { key: 'disableMfa', label: 'Disable MFA', description: 'Remove multi-factor authentication from this user.', icon: <DisableMfaIcon />, color: 'error' },
  ]

  return (
    <Box>
      <Stack spacing={1}>
        {actions.map(({ key, label, description, icon, color }) => (
          <Box key={key} sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 1.5, border: '1px solid', borderColor: 'divider', borderRadius: 2 }}>
            <Box>
              <Typography variant="body2" fontWeight={500}>{label}</Typography>
              <Typography variant="caption" color="text.secondary">{description}</Typography>
            </Box>
            <Button size="small" color={color} variant="outlined"
              onClick={() => setConfirm({ action: key, label, description })}>
              {label.split(' ')[0]}
            </Button>
          </Box>
        ))}

        <Box sx={{ p: 1.5, border: '1px solid', borderColor: 'divider', borderRadius: 2, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
          <Box>
            <Typography variant="body2" fontWeight={500}>Force Password Reset</Typography>
            <Typography variant="caption" color="text.secondary">Generate a temporary password and invalidate current sessions.</Typography>
          </Box>
          <Button size="small" color="warning" variant="outlined" startIcon={<PasswordIcon />}
            disabled={forceResetMutation.isPending}
            onClick={() => setConfirm({ action: '__forceReset', label: 'Force Password Reset', description: 'Generate a new temporary password? Existing sessions will be invalidated.' })}>
            Reset
          </Button>
        </Box>
      </Stack>

      <ConfirmDialog
        open={!!confirm}
        title={confirm?.label ?? ''}
        message={confirm?.description ?? ''}
        onConfirm={() => {
          if (confirm.action === '__forceReset') {
            forceResetMutation.mutate()
            setConfirm(null)
          } else {
            mutation.mutate({ action: confirm.action })
          }
        }}
        onCancel={() => setConfirm(null)}
      />

      <Dialog open={!!tempPassword} onClose={() => setTempPassword(null)} maxWidth="xs" fullWidth PaperProps={{ sx: { borderRadius: 3 } }}>
        <DialogTitle fontWeight={700}>Temporary Password</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" mb={2}>
            Share this password securely with the user. It will not be shown again.
          </Typography>
          <Box sx={{ bgcolor: 'action.hover', borderRadius: 2, p: 2, textAlign: 'center' }}>
            <Typography variant="h6" sx={{ fontFamily: 'monospace', letterSpacing: 2 }}>{tempPassword}</Typography>
          </Box>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => navigator.clipboard.writeText(tempPassword)} variant="outlined">Copy</Button>
          <Button onClick={() => setTempPassword(null)} variant="contained">Done</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

function AuditTab({ userId }) {
  const { data: auditData, isLoading } = useUserAudit(userId)
  const logs = auditData?.items ?? []

  if (isLoading) return <Typography color="text.secondary">Loading…</Typography>
  if (!logs.length) return <Typography variant="body2" color="text.secondary">No audit events found.</Typography>

  return (
    <Stack spacing={0.75}>
      {logs.map((log) => (
        <Box
          key={log.id}
          sx={{
            display: 'flex', alignItems: 'center', gap: 2,
            px: 2, py: 1.25,
            bgcolor: 'rgba(148,163,184,0.04)',
            borderRadius: 1.5,
            borderLeft: '3px solid',
            borderColor: log.isSecurityCritical ? 'error.main' : 'primary.main',
          }}
        >
          <SeverityBadge severity={log.severity} />
          <Typography variant="body2" fontWeight={500} sx={{ flex: 1 }}>
            {log.eventType.replace(/([A-Z])/g, ' $1').trim()}
          </Typography>
          {log.ipAddress && (
            <Typography variant="caption" color="text.disabled" sx={{ fontFamily: 'monospace' }}>
              {log.ipAddress}
            </Typography>
          )}
          <Typography variant="caption" color="text.disabled" whiteSpace="nowrap">
            {fmt(log.occurredAtUtc)}
          </Typography>
        </Box>
      ))}
    </Stack>
  )
}

// ─── UserDetailDialog ────────────────────────────────────────────────────────

const TABS = [
  { label: 'Overview', icon: <PersonIcon sx={{ fontSize: 16 }} /> },
  { label: 'Roles', icon: <RolesIcon sx={{ fontSize: 16 }} /> },
  { label: 'Devices', icon: <DevicesIcon sx={{ fontSize: 16 }} /> },
  { label: 'Sessions', icon: <SessionsIcon sx={{ fontSize: 16 }} /> },
  { label: 'Security', icon: <SecurityIcon sx={{ fontSize: 16 }} /> },
  { label: 'Audit', icon: <AuditIcon sx={{ fontSize: 16 }} /> },
]

function UserDetailDialog({ userId, tenantId, onClose }) {
  const [tab, setTab] = useState(0)
  const { data: user, isLoading } = useUserDetails(userId)

  if (!userId) return null

  return (
    <Dialog
      open={!!userId}
      onClose={onClose}
      maxWidth="md"
      fullWidth
      PaperProps={{ sx: { borderRadius: 3, minHeight: '70vh', display: 'flex', flexDirection: 'column' } }}
    >
      <DialogTitle sx={{ pb: 0 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
          <Box>
            {isLoading ? (
              <Typography variant="h6" fontWeight={700}>Loading…</Typography>
            ) : (
              <>
                <Typography variant="h6" fontWeight={700}>{user?.displayName ?? 'User Details'}</Typography>
                <Typography variant="body2" color="text.secondary" sx={{ fontFamily: 'monospace' }}>{user?.email}</Typography>
              </>
            )}
          </Box>
          <IconButton onClick={onClose} size="small" sx={{ mt: -0.5 }}><CloseIcon /></IconButton>
        </Box>
        <Tabs
          value={tab}
          onChange={(_, v) => setTab(v)}
          sx={{ mt: 1.5, borderBottom: 1, borderColor: 'divider' }}
          variant="scrollable"
          scrollButtons="auto"
        >
          {TABS.map((t) => (
            <Tab key={t.label} label={t.label} iconPosition="start" icon={t.icon}
              sx={{ minHeight: 48, fontSize: '0.8125rem' }} />
          ))}
        </Tabs>
      </DialogTitle>

      <DialogContent sx={{ pt: 2, flex: 1 }}>
        {isLoading ? (
          <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
            <CircularProgress size={32} />
          </Box>
        ) : (
          <>
            {tab === 0 && <OverviewTab user={user} />}
            {tab === 1 && <RolesTab userId={userId} tenantId={tenantId} />}
            {tab === 2 && <DevicesTab userId={userId} />}
            {tab === 3 && <SessionsTab userId={userId} />}
            {tab === 4 && <SecurityTab userId={userId} />}
            {tab === 5 && <AuditTab userId={userId} />}
          </>
        )}
      </DialogContent>
    </Dialog>
  )
}

// ─── UserActionsMenu ─────────────────────────────────────────────────────────

function UserActionsMenu({ userId }) {
  const queryClient = useQueryClient()
  const [anchor, setAnchor] = useState(null)
  const [confirm, setConfirm] = useState(null)

  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ['tenant', 'users'] })
  }

  const actionMap = {
    lock: () => api.post(`/users/${userId}/lock`),
    unlock: () => api.post(`/users/${userId}/unlock`),
    suspend: () => api.post(`/users/${userId}/suspend`),
    activate: () => api.post(`/users/${userId}/activate`),
    revokeSessions: () => api.post(`/users/${userId}/revoke-sessions`),
    disableMfa: () => api.post(`/users/${userId}/disable-mfa`),
  }

  const mutation = useMutation({
    mutationFn: ({ action }) => actionMap[action](),
    onSuccess: () => { invalidate(); setConfirm(null) },
  })

  const open = (action, label, description) => {
    setAnchor(null)
    setConfirm({ action, label, description })
  }

  return (
    <>
      <IconButton size="small" onClick={(e) => { e.stopPropagation(); setAnchor(e.currentTarget) }}>
        <MoreVertIcon fontSize="small" />
      </IconButton>

      <Menu anchorEl={anchor} open={Boolean(anchor)} onClose={() => setAnchor(null)}
        PaperProps={{ sx: { minWidth: 200, borderRadius: 2 } }}>
        <MenuItem onClick={() => open('lock', 'Lock User', 'Temporarily lock this user for 24 hours.')}>
          <ListItemIcon><LockIcon fontSize="small" /></ListItemIcon>
          <ListItemText>Lock (24h)</ListItemText>
        </MenuItem>
        <MenuItem onClick={() => open('unlock', 'Unlock User', 'Remove the temporary lock on this user.')}>
          <ListItemIcon><UnlockIcon fontSize="small" /></ListItemIcon>
          <ListItemText>Unlock</ListItemText>
        </MenuItem>
        <MenuItem onClick={() => open('suspend', 'Suspend User', 'Suspend this user — they cannot log in.')}>
          <ListItemIcon><SuspendIcon fontSize="small" color="warning" /></ListItemIcon>
          <ListItemText>Suspend</ListItemText>
        </MenuItem>
        <MenuItem onClick={() => open('activate', 'Activate User', 'Re-activate this suspended user.')}>
          <ListItemIcon><ActivateIcon fontSize="small" color="success" /></ListItemIcon>
          <ListItemText>Activate</ListItemText>
        </MenuItem>
        <MenuItem onClick={() => open('revokeSessions', 'Revoke Sessions', 'Invalidate all active sessions for this user.')}>
          <ListItemIcon><RevokeSessionsIcon fontSize="small" /></ListItemIcon>
          <ListItemText>Revoke Sessions</ListItemText>
        </MenuItem>
        <MenuItem onClick={() => open('disableMfa', 'Disable MFA', 'Remove multi-factor authentication from this user.')}>
          <ListItemIcon><DisableMfaIcon fontSize="small" color="error" /></ListItemIcon>
          <ListItemText>Disable MFA</ListItemText>
        </MenuItem>
      </Menu>

      <ConfirmDialog
        open={!!confirm}
        title={confirm?.label ?? ''}
        message={confirm?.description ?? ''}
        onConfirm={() => mutation.mutate({ action: confirm.action })}
        onCancel={() => setConfirm(null)}
      />
    </>
  )
}

// ─── Main page ───────────────────────────────────────────────────────────────

export default function TenantUsersPage() {
  const { tenantId, hasPermission } = useAuth()
  const canCreateUsers = hasPermission('user.create')
  const canManageUsers = hasPermission('user.manage')
  const queryClient = useQueryClient()
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 20 })
  const [selectedUserId, setSelectedUserId] = useState(null)
  const [createOpen, setCreateOpen] = useState(false)
  const [createError, setCreateError] = useState(null)

  const { data, isLoading } = useUsers(paginationModel.page, paginationModel.pageSize)
  const { data: roles } = useRoles(tenantId)

  const { register, handleSubmit, reset, control, formState: { errors } } = useForm()

  const createMutation = useMutation({
    mutationFn: (body) => api.post('/users', body),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tenant', 'users'] })
      setCreateOpen(false)
      reset()
      setCreateError(null)
    },
    onError: (err) => setCreateError(err),
  })

  const columns = [
    {
      field: 'displayName', headerName: 'Name', flex: 1.2, minWidth: 140,
      renderCell: ({ value }) => (
        <Typography variant="body2" fontWeight={600}>{value ?? '—'}</Typography>
      ),
    },
    {
      field: 'email', headerName: 'Email', flex: 1.5, minWidth: 180,
    },
    {
      field: 'status', headerName: 'Status', width: 120,
      renderCell: ({ value }) => <StatusChip status={value} />,
    },
    {
      field: 'isMfaEnabled', headerName: 'MFA', width: 80,
      renderCell: ({ value, row }) => (
        <Tooltip title={value ? `MFA: ${row.mfaMethod}` : 'MFA disabled'}>
          <MfaIcon sx={{ color: value ? 'success.main' : 'text.disabled', fontSize: 20 }} />
        </Tooltip>
      ),
    },
    {
      field: 'lastLoginUtc', headerName: 'Last Login', width: 160,
      renderCell: ({ value }) => value
        ? new Date(value).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' })
        : '—',
    },
    ...(canManageUsers ? [{
      field: 'actions', headerName: '', width: 60, sortable: false,
      renderCell: ({ row }) => <UserActionsMenu userId={row.id} />,
    }] : []),
  ]

  return (
    <Box>
      <PageHeader
        title="Users"
        subtitle="Manage user accounts within your organization"
        action={canCreateUsers && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => { reset(); setCreateError(null); setCreateOpen(true) }}>
            New User
          </Button>
        )}
      />

      <Card>
        <CardContent sx={{ p: 0, '&:last-child': { pb: 0 } }}>
          <DataTable
            rows={data?.items ?? []}
            columns={columns}
            loading={isLoading}
            rowCount={data?.total ?? 0}
            paginationModel={paginationModel}
            onPaginationModelChange={setPaginationModel}
            onRowClick={({ row }) => setSelectedUserId(row.id)}
            sx={{
              minHeight: 400,
              '& .MuiDataGrid-row': { cursor: 'pointer' },
              '& .MuiDataGrid-row:hover': { bgcolor: 'rgba(6,182,212,0.04)' },
            }}
          />
        </CardContent>
      </Card>

      {/* Create User dialog */}
      <Dialog open={createOpen} onClose={() => { setCreateOpen(false); reset(); setCreateError(null) }}
        maxWidth="sm" fullWidth PaperProps={{ sx: { borderRadius: 3 } }}>
        <DialogTitle fontWeight={700}>Create User</DialogTitle>
        <Box component="form" onSubmit={handleSubmit((d) => createMutation.mutate(d))}>
          <DialogContent>
            <ProblemAlert error={createError} />
            <Stack spacing={2} sx={{ mt: 1 }}>
              <Stack direction="row" spacing={2}>
                <TextField label="First Name" {...register('firstName', { required: true })}
                  error={!!errors.firstName} helperText={errors.firstName && 'Required'}
                  fullWidth />
                <TextField label="Last Name" {...register('lastName', { required: true })}
                  error={!!errors.lastName} helperText={errors.lastName && 'Required'}
                  fullWidth />
              </Stack>
              <TextField label="Email" type="email" {...register('email', { required: true })}
                error={!!errors.email} helperText={errors.email && 'Required'}
                fullWidth />
              <TextField label="Password" type="password" {...register('password', { required: true, minLength: 8 })}
                error={!!errors.password}
                helperText={errors.password ? (errors.password.type === 'minLength' ? 'Minimum 8 characters' : 'Required') : ''}
                fullWidth />
              <Controller
                name="roleId"
                control={control}
                render={({ field }) => (
                  <FormControl fullWidth>
                    <InputLabel>Initial Role (optional)</InputLabel>
                    <Select {...field} value={field.value ?? ''} label="Initial Role (optional)">
                      <SelectMenuItem value=""><em>None</em></SelectMenuItem>
                      {(roles ?? []).map((r) => (
                        <SelectMenuItem key={r.id} value={r.id}>{r.name}</SelectMenuItem>
                      ))}
                    </Select>
                  </FormControl>
                )}
              />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => { setCreateOpen(false); reset(); setCreateError(null) }} color="inherit">Cancel</Button>
            <Button type="submit" variant="contained" disabled={createMutation.isPending}>
              {createMutation.isPending ? 'Creating…' : 'Create User'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      {/* User detail dialog */}
      <UserDetailDialog
        userId={selectedUserId}
        tenantId={tenantId}
        onClose={() => setSelectedUserId(null)}
      />
    </Box>
  )
}
