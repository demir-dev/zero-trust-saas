import { useState } from 'react'
import {
  Box, Card, CardContent, Typography, Chip, IconButton, Tooltip,
} from '@mui/material'
import {
  LockPerson as LockIcon, LockOpen as UnlockIcon,
  Security as MfaIcon,
} from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import StatusChip from '../../../../shared/components/StatusChip'
import DataTable from '../../../../shared/components/DataTable'

function useUsers(page, pageSize) {
  return useQuery({
    queryKey: ['tenant', 'users', page, pageSize],
    queryFn: () => api.get(`/users?page=${page + 1}&pageSize=${pageSize}`).then(r => r.data),
  })
}

export default function TenantUsersPage() {
  const queryClient = useQueryClient()
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 20 })

  const { data, isLoading } = useUsers(paginationModel.page, paginationModel.pageSize)

  const lockMutation = useMutation({
    mutationFn: (id) => api.post(`/users/${id}/lock`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tenant', 'users'] }),
  })

  const unlockMutation = useMutation({
    mutationFn: (id) => api.post(`/users/${id}/unlock`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tenant', 'users'] }),
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
      field: 'registeredAtUtc', headerName: 'Registered', width: 130,
      renderCell: ({ value }) => value ? new Date(value).toLocaleDateString() : '—',
    },
    {
      field: 'lastLoginUtc', headerName: 'Last Login', width: 160,
      renderCell: ({ value }) => value ? new Date(value).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' }) : '—',
    },
    {
      field: 'actions', headerName: '', width: 80, sortable: false,
      renderCell: ({ row }) => (
        row.status === 'Locked' ? (
          <Tooltip title="Unlock user">
            <IconButton size="small" color="success" onClick={(e) => { e.stopPropagation(); unlockMutation.mutate(row.id) }}>
              <UnlockIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        ) : (
          <Tooltip title="Lock user (24h)">
            <IconButton
              size="small"
              color="warning"
              disabled={row.status === 'Disabled' || row.status === 'Suspended'}
              onClick={(e) => { e.stopPropagation(); lockMutation.mutate(row.id) }}
            >
              <LockIcon fontSize="small" />
            </IconButton>
          </Tooltip>
        )
      ),
    },
  ]

  return (
    <Box>
      <PageHeader
        title="Users"
        subtitle="Manage user accounts within your organization"
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
            sx={{
              minHeight: 400,
              '& .MuiDataGrid-row:hover': { bgcolor: 'rgba(6,182,212,0.04)' },
            }}
          />
        </CardContent>
      </Card>
    </Box>
  )
}
