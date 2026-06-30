import { useState } from 'react'
import { useParams } from 'react-router-dom'
import {
  Box, Card, CardContent, Typography, Chip, IconButton, Tooltip,
  Menu, MenuItem, ListItemIcon, ListItemText,
} from '@mui/material'
import {
  LockPerson as LockIcon,
  LockOpen as UnlockIcon,
  PauseCircle as SuspendIcon,
  PlayCircle as ActivateIcon,
  LogoutOutlined as RevokeSessionsIcon,
  PhonelinkOff as DisableMfaIcon,
  MoreVert as MoreVertIcon,
  PhonelinkLock as MfaIcon,
} from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import StatusChip from '../../../../shared/components/StatusChip'
import DataTable from '../../../../shared/components/DataTable'
import ConfirmDialog from '../../../../shared/components/ConfirmDialog'

function useInspectionUsers(tenantId, page, pageSize) {
  return useQuery({
    queryKey: ['platform', 'inspection', tenantId, 'users', page, pageSize],
    queryFn: () =>
      api.get(`/platform/tenants/${tenantId}/users?page=${page + 1}&pageSize=${pageSize}`)
        .then(r => r.data),
  })
}

function UserActionsMenu({ userId, tenantId, userStatus }) {
  const queryClient = useQueryClient()
  const [anchor, setAnchor] = useState(null)
  const [confirm, setConfirm] = useState(null)

  const invalidate = () => queryClient.invalidateQueries({ queryKey: ['platform', 'inspection', tenantId, 'users'] })

  const actions = {
    lock: (id) => api.post(`/platform/tenants/${tenantId}/users/${id}/lock`),
    unlock: (id) => api.post(`/platform/tenants/${tenantId}/users/${id}/unlock`),
    suspend: (id) => api.post(`/platform/tenants/${tenantId}/users/${id}/suspend`),
    activate: (id) => api.post(`/platform/tenants/${tenantId}/users/${id}/activate`),
    revokeSessions: (id) => api.post(`/platform/tenants/${tenantId}/users/${id}/revoke-sessions`),
    disableMfa: (id) => api.post(`/platform/tenants/${tenantId}/users/${id}/disable-mfa`),
  }

  const mutation = useMutation({
    mutationFn: ({ action, id }) => actions[action](id),
    onSuccess: () => { invalidate(); setConfirm(null) },
  })

  const handleAction = (action, label, description) => {
    setAnchor(null)
    setConfirm({ action, label, description })
  }

  const isLocked = userStatus === 'Locked'
  const isSuspended = userStatus === 'Suspended'
  const isActive = userStatus === 'Active'

  return (
    <>
      <IconButton size="small" onClick={(e) => { e.stopPropagation(); setAnchor(e.currentTarget) }}>
        <MoreVertIcon fontSize="small" />
      </IconButton>

      <Menu
        anchorEl={anchor}
        open={Boolean(anchor)}
        onClose={() => setAnchor(null)}
        PaperProps={{ sx: { minWidth: 200, borderRadius: 2 } }}
      >
        {!isLocked && (
          <MenuItem onClick={() => handleAction('lock', 'Lock User', 'Temporarily lock this user for 24 hours.')}>
            <ListItemIcon><LockIcon fontSize="small" /></ListItemIcon>
            <ListItemText>Lock (24h)</ListItemText>
          </MenuItem>
        )}
        {isLocked && (
          <MenuItem onClick={() => handleAction('unlock', 'Unlock User', 'Remove the lock on this user.')}>
            <ListItemIcon><UnlockIcon fontSize="small" /></ListItemIcon>
            <ListItemText>Unlock</ListItemText>
          </MenuItem>
        )}
        {!isSuspended && (
          <MenuItem onClick={() => handleAction('suspend', 'Suspend User', 'Suspend this user — they cannot log in.')}>
            <ListItemIcon><SuspendIcon fontSize="small" color="warning" /></ListItemIcon>
            <ListItemText>Suspend</ListItemText>
          </MenuItem>
        )}
        {isSuspended && (
          <MenuItem onClick={() => handleAction('activate', 'Activate User', 'Re-activate this suspended user.')}>
            <ListItemIcon><ActivateIcon fontSize="small" color="success" /></ListItemIcon>
            <ListItemText>Activate</ListItemText>
          </MenuItem>
        )}
        <MenuItem onClick={() => handleAction('revokeSessions', 'Revoke Sessions', 'Invalidate all active sessions for this user.')}>
          <ListItemIcon><RevokeSessionsIcon fontSize="small" /></ListItemIcon>
          <ListItemText>Revoke Sessions</ListItemText>
        </MenuItem>
        <MenuItem onClick={() => handleAction('disableMfa', 'Disable MFA', 'Remove multi-factor authentication from this user.')}>
          <ListItemIcon><DisableMfaIcon fontSize="small" color="error" /></ListItemIcon>
          <ListItemText>Disable MFA</ListItemText>
        </MenuItem>
      </Menu>

      <ConfirmDialog
        open={!!confirm}
        title={confirm?.label ?? ''}
        message={confirm?.description ?? ''}
        onConfirm={() => mutation.mutate({ action: confirm.action, id: userId })}
        onCancel={() => setConfirm(null)}
      />
    </>
  )
}

export default function TenantInspectionUsersPage() {
  const { tenantId } = useParams()
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 20 })

  const { data, isLoading } = useInspectionUsers(
    tenantId,
    paginationModel.page,
    paginationModel.pageSize,
  )

  const columns = [
    {
      field: 'displayName', headerName: 'Name', flex: 1, minWidth: 150,
      renderCell: ({ value }) => (
        <Typography variant="body2" fontWeight={500}>{value}</Typography>
      ),
    },
    {
      field: 'email', headerName: 'Email', flex: 1.5, minWidth: 200,
      renderCell: ({ value }) => (
        <Typography variant="caption" sx={{ fontFamily: 'monospace', color: 'text.secondary' }}>
          {value}
        </Typography>
      ),
    },
    {
      field: 'status', headerName: 'Status', width: 120,
      renderCell: ({ value }) => <StatusChip status={value} />,
    },
    {
      field: 'isMfaEnabled', headerName: 'MFA', width: 80,
      renderCell: ({ value }) => value
        ? <Tooltip title="MFA Enabled"><MfaIcon fontSize="small" sx={{ color: 'success.main' }} /></Tooltip>
        : <Typography variant="caption" color="text.disabled">—</Typography>,
    },
    {
      field: 'lastLoginUtc', headerName: 'Last Login', width: 160,
      renderCell: ({ value }) => value
        ? new Date(value).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' })
        : 'Never',
    },
    {
      field: 'actions', headerName: '', width: 60, sortable: false,
      renderCell: ({ row }) => (
        <UserActionsMenu userId={row.id} tenantId={tenantId} userStatus={row.status} />
      ),
    },
  ]

  return (
    <Box>
      <PageHeader title="Users" subtitle="All members of this tenant" />

      <Card>
        <CardContent sx={{ p: 0, '&:last-child': { pb: 0 } }}>
          <DataTable
            rows={data?.items ?? []}
            columns={columns}
            loading={isLoading}
            rowCount={data?.total ?? 0}
            paginationModel={paginationModel}
            onPaginationModelChange={setPaginationModel}
            sx={{ minHeight: 400 }}
          />
        </CardContent>
      </Card>
    </Box>
  )
}
