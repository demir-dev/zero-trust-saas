import { useState } from 'react'
import {
  Box, Card, CardContent, Typography, Chip, IconButton, Tooltip, Button,
  Dialog, DialogTitle, DialogContent, DialogActions, TextField, Stack,
  Drawer, Divider, Menu, MenuItem, ListItemIcon, ListItemText,
  MenuItem as SelectMenuItem, Select, FormControl, InputLabel,
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
  AdminPanelSettings as RolesIcon,
  Close as CloseIcon,
  PersonOff as RevokeRoleIcon,
} from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm, Controller } from 'react-hook-form'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import StatusChip from '../../../../shared/components/StatusChip'
import DataTable from '../../../../shared/components/DataTable'
import ConfirmDialog from '../../../../shared/components/ConfirmDialog'
import ProblemAlert from '../../../../shared/components/ProblemAlert'
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

function useRoles(tenantId) {
  return useQuery({
    queryKey: ['tenant', 'roles', tenantId],
    queryFn: () => api.get(`/authorization/roles?tenantId=${tenantId}`).then(r => r.data),
    enabled: !!tenantId,
    staleTime: 30000,
  })
}

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

function UserDetailDrawer({ userId, tenantId, onClose }) {
  const queryClient = useQueryClient()
  const [assignRoleOpen, setAssignRoleOpen] = useState(false)
  const [revokeRoleConfirm, setRevokeRoleConfirm] = useState(null)
  const [forceResetOpen, setForceResetOpen] = useState(false)
  const [tempPassword, setTempPassword] = useState(null)
  const [assignRoleError, setAssignRoleError] = useState(null)
  const [selectedRoleId, setSelectedRoleId] = useState('')

  const { data: user, isLoading } = useUserDetails(userId)
  const { data: roles } = useRoles(tenantId)

  const invalidateUsers = () => queryClient.invalidateQueries({ queryKey: ['tenant', 'users'] })
  const invalidateDetail = () => queryClient.invalidateQueries({ queryKey: ['tenant', 'users', 'detail', userId] })

  const assignRoleMutation = useMutation({
    mutationFn: (roleId) => api.post(`/users/${userId}/assign-role`, { roleId }),
    onSuccess: () => {
      invalidateDetail()
      invalidateUsers()
      setAssignRoleOpen(false)
      setSelectedRoleId('')
      setAssignRoleError(null)
    },
    onError: (err) => setAssignRoleError(err),
  })

  const revokeRoleMutation = useMutation({
    mutationFn: (roleId) => api.post(`/users/${userId}/revoke-role`, { roleId }),
    onSuccess: () => { invalidateDetail(); invalidateUsers(); setRevokeRoleConfirm(null) },
  })

  const forceResetMutation = useMutation({
    mutationFn: () => api.post(`/users/${userId}/force-password-reset`),
    onSuccess: (res) => {
      setTempPassword(res.data.temporaryPassword)
      setForceResetOpen(false)
    },
  })

  if (!userId) return null

  return (
    <Drawer anchor="right" open={!!userId} onClose={onClose}
      PaperProps={{ sx: { width: { xs: '100%', sm: 420 }, p: 3 } }}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
        <Typography variant="h6" fontWeight={700}>User Details</Typography>
        <IconButton onClick={onClose}><CloseIcon /></IconButton>
      </Box>

      {isLoading ? (
        <Typography color="text.secondary">Loading…</Typography>
      ) : user ? (
        <Stack spacing={2.5}>
          <Box>
            <Typography variant="h6" fontWeight={600}>{user.displayName}</Typography>
            <Typography variant="body2" color="text.secondary" sx={{ fontFamily: 'monospace' }}>{user.email}</Typography>
          </Box>

          <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
            <StatusChip status={user.status} />
            {user.isMfaEnabled && (
              <Chip label="MFA" size="small" color="success" icon={<MfaIcon />} />
            )}
          </Box>

          <Divider />

          <Box>
            <Typography variant="overline" color="text.secondary" display="block" mb={1}>
              Details
            </Typography>
            <Stack spacing={0.75}>
              {[
                { label: 'Registered', value: user.registeredAtUtc ? new Date(user.registeredAtUtc).toLocaleDateString() : '—' },
                { label: 'Last Login', value: user.lastLoginUtc ? new Date(user.lastLoginUtc).toLocaleString() : 'Never' },
                { label: 'MFA Method', value: user.mfaMethod ?? '—' },
                { label: 'Email Verified', value: user.isEmailConfirmed ? 'Yes' : 'No' },
              ].map(({ label, value }) => (
                <Box key={label} sx={{ display: 'flex', justifyContent: 'space-between' }}>
                  <Typography variant="caption" color="text.secondary">{label}</Typography>
                  <Typography variant="caption">{value}</Typography>
                </Box>
              ))}
            </Stack>
          </Box>

          <Divider />

          <Box>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
              <Typography variant="overline" color="text.secondary">Roles</Typography>
              <Button size="small" startIcon={<AddIcon />} onClick={() => { setAssignRoleError(null); setSelectedRoleId(''); setAssignRoleOpen(true) }}>
                Assign Role
              </Button>
            </Box>

            {!user.roles?.length ? (
              <Typography variant="body2" color="text.secondary">No roles assigned</Typography>
            ) : (
              <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                {user.roles.map((r) => (
                  <Chip
                    key={r.roleId}
                    label={r.roleName}
                    size="small"
                    onDelete={!r.isSystem ? () => setRevokeRoleConfirm(r) : undefined}
                    deleteIcon={<RevokeRoleIcon />}
                  />
                ))}
              </Box>
            )}
          </Box>

          <Divider />

          <Box>
            <Typography variant="overline" color="text.secondary" display="block" mb={1}>
              Actions
            </Typography>
            <Stack spacing={1}>
              <Button
                variant="outlined"
                size="small"
                startIcon={<PasswordIcon />}
                color="warning"
                onClick={() => setForceResetOpen(true)}
                fullWidth
              >
                Force Password Reset
              </Button>
            </Stack>
          </Box>
        </Stack>
      ) : (
        <Typography color="text.secondary">User not found</Typography>
      )}

      {/* Assign Role dialog */}
      <Dialog open={assignRoleOpen} onClose={() => setAssignRoleOpen(false)}
        maxWidth="xs" fullWidth PaperProps={{ sx: { borderRadius: 3 } }}>
        <DialogTitle fontWeight={700}>Assign Role</DialogTitle>
        <DialogContent>
          <ProblemAlert error={assignRoleError} />
          <FormControl fullWidth sx={{ mt: 1 }}>
            <InputLabel>Role</InputLabel>
            <Select
              value={selectedRoleId}
              label="Role"
              onChange={(e) => setSelectedRoleId(e.target.value)}
            >
              {(roles ?? []).map((r) => (
                <SelectMenuItem key={r.id} value={r.id}>{r.name}</SelectMenuItem>
              ))}
            </Select>
          </FormControl>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setAssignRoleOpen(false)} color="inherit">Cancel</Button>
          <Button
            variant="contained"
            disabled={!selectedRoleId || assignRoleMutation.isPending}
            onClick={() => assignRoleMutation.mutate(selectedRoleId)}
          >
            {assignRoleMutation.isPending ? 'Assigning…' : 'Assign'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Revoke role confirmation */}
      <ConfirmDialog
        open={!!revokeRoleConfirm}
        title="Remove Role"
        message={`Remove role "${revokeRoleConfirm?.roleName}" from this user?`}
        onConfirm={() => revokeRoleMutation.mutate(revokeRoleConfirm.roleId)}
        onCancel={() => setRevokeRoleConfirm(null)}
      />

      {/* Force password reset confirmation */}
      <ConfirmDialog
        open={forceResetOpen}
        title="Force Password Reset"
        message="Generate a new temporary password for this user? Their existing sessions will be invalidated."
        onConfirm={() => forceResetMutation.mutate()}
        onCancel={() => setForceResetOpen(false)}
      />

      {/* Temp password result dialog */}
      <Dialog open={!!tempPassword} onClose={() => setTempPassword(null)}
        maxWidth="xs" fullWidth PaperProps={{ sx: { borderRadius: 3 } }}>
        <DialogTitle fontWeight={700}>Temporary Password</DialogTitle>
        <DialogContent>
          <Typography variant="body2" color="text.secondary" mb={2}>
            Share this password securely with the user. It will not be shown again.
          </Typography>
          <Box sx={{ bgcolor: 'action.hover', borderRadius: 2, p: 2, textAlign: 'center' }}>
            <Typography variant="h6" sx={{ fontFamily: 'monospace', letterSpacing: 2 }}>
              {tempPassword}
            </Typography>
          </Box>
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => navigator.clipboard.writeText(tempPassword)} variant="outlined">Copy</Button>
          <Button onClick={() => setTempPassword(null)} variant="contained">Done</Button>
        </DialogActions>
      </Dialog>
    </Drawer>
  )
}

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
      renderCell: ({ value, row }) => (
        <Box>
          <Typography variant="body2" fontWeight={600}>{value ?? '—'}</Typography>
          <Typography variant="caption" color="text.disabled">{row.email}</Typography>
        </Box>
      ),
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

      {/* User detail drawer */}
      <UserDetailDrawer
        userId={selectedUserId}
        tenantId={tenantId}
        onClose={() => setSelectedUserId(null)}
      />
    </Box>
  )
}
