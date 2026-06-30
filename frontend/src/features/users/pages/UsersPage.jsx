import {
  Box, Card, Typography, Button, Table, TableBody, TableCell,
  TableContainer, TableHead, TableRow, Skeleton, Tooltip, IconButton
} from '@mui/material'
import {
  LockPerson as LockIcon, LockOpen as UnlockIcon,
  People as PeopleIcon, Security as MfaIcon
} from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { motion } from 'framer-motion'
import { pageVariants } from '../../../shared/utils/motionVariants'
import api from '../../../shared/api/axiosInstance'
import PageHeader from '../../../shared/components/PageHeader'
import StatusChip from '../../../shared/components/StatusChip'
import EmptyState from '../../../shared/components/EmptyState'

function useUsers() {
  return useQuery({
    queryKey: ['users'],
    queryFn: () => api.get('/users?pageSize=100').then(r => r.data),
  })
}

export default function UsersPage() {
  const queryClient = useQueryClient()
  const { data, isLoading } = useUsers()

  const lockMutation = useMutation({
    mutationFn: (id) => api.post(`/users/${id}/lock`),
    onSuccess: () => queryClient.invalidateQueries(['users']),
  })

  const unlockMutation = useMutation({
    mutationFn: (id) => api.post(`/users/${id}/unlock`),
    onSuccess: () => queryClient.invalidateQueries(['users']),
  })

  const users = data?.items ?? []

  return (
    <motion.div variants={pageVariants} initial="initial" animate="animate">
      <PageHeader title="Users" subtitle="Manage user accounts within your tenant" />

      <Card>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Email</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>MFA</TableCell>
                <TableCell>Registered</TableCell>
                <TableCell>Last Login</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                [...Array(5)].map((_, i) => (
                  <TableRow key={i}>
                    {[...Array(6)].map((_, j) => <TableCell key={j}><Skeleton /></TableCell>)}
                  </TableRow>
                ))
              ) : users.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={6}>
                    <EmptyState icon={PeopleIcon} title="No users yet" subtitle="Users will appear here after registration" />
                  </TableCell>
                </TableRow>
              ) : (
                users.map((u) => (
                  <TableRow key={u.id} hover>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>{u.email}</Typography>
                      <Typography variant="caption" sx={{ color: 'text.disabled' }}>{u.id}</Typography>
                    </TableCell>
                    <TableCell><StatusChip status={u.status} /></TableCell>
                    <TableCell>
                      <Tooltip title={u.isMfaEnabled ? `MFA: ${u.mfaMethod}` : 'MFA disabled'}>
                        <MfaIcon sx={{ color: u.isMfaEnabled ? 'success.main' : 'text.disabled', fontSize: 18 }} />
                      </Tooltip>
                    </TableCell>
                    <TableCell>
                      <Typography variant="caption">{new Date(u.registeredAtUtc).toLocaleDateString()}</Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="caption">{u.lastLoginUtc ? new Date(u.lastLoginUtc).toLocaleString() : '—'}</Typography>
                    </TableCell>
                    <TableCell align="right">
                      {u.status === 'Locked' ? (
                        <Tooltip title="Unlock user">
                          <IconButton size="small" color="success" onClick={() => unlockMutation.mutate(u.id)}>
                            <UnlockIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      ) : (
                        <Tooltip title="Lock user (24h)">
                          <IconButton size="small" color="warning" onClick={() => lockMutation.mutate(u.id)}
                            disabled={u.status === 'Disabled' || u.status === 'Suspended'}>
                            <LockIcon fontSize="small" />
                          </IconButton>
                        </Tooltip>
                      )}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>
    </motion.div>
  )
}
