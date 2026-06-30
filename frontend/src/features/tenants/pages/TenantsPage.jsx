import { useState } from 'react'
import {
  Box, Card, Typography, Button, TextField,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Skeleton, Dialog, DialogTitle, DialogContent, DialogActions, Stack
} from '@mui/material'
import { Add as AddIcon, Business as BusinessIcon } from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { motion } from 'framer-motion'
import { pageVariants } from '../../../shared/utils/motionVariants'
import api from '../../../shared/api/axiosInstance'
import PageHeader from '../../../shared/components/PageHeader'
import StatusChip from '../../../shared/components/StatusChip'
import EmptyState from '../../../shared/components/EmptyState'
import ProblemAlert from '../../../shared/components/ProblemAlert'

const schema = z.object({
  name: z.string().min(1, 'Name is required').max(100),
  slug: z.string().regex(/^[a-z0-9][a-z0-9-]{1,48}[a-z0-9]$/, 'Slug must be lowercase letters, numbers, hyphens (3–50 chars)'),
  ownerUserId: z.string().uuid('Must be a valid user UUID'),
})

function useTenants() {
  return useQuery({
    queryKey: ['tenants'],
    queryFn: () => api.get('/tenants?pageSize=100').then(r => r.data),
  })
}

export default function TenantsPage() {
  const [dialogOpen, setDialogOpen] = useState(false)
  const [createError, setCreateError] = useState(null)
  const queryClient = useQueryClient()
  const { data, isLoading } = useTenants()

  const { register, handleSubmit, reset, formState: { errors } } = useForm({ resolver: zodResolver(schema) })

  const mutation = useMutation({
    mutationFn: (body) => api.post('/tenants', body),
    onSuccess: () => {
      queryClient.invalidateQueries(['tenants'])
      setDialogOpen(false)
      reset()
      setCreateError(null)
    },
    onError: (err) => setCreateError(err),
  })

  const handleClose = () => {
    setDialogOpen(false)
    reset()
    setCreateError(null)
  }

  const tenants = data?.items ?? []

  return (
    <motion.div variants={pageVariants} initial="initial" animate="animate">
      <PageHeader
        title="Tenants"
        subtitle="Manage your organizations"
        action={
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setDialogOpen(true)}>
            New Tenant
          </Button>
        }
      />

      <Card>
        <TableContainer>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Slug</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Members</TableCell>
                <TableCell>Created</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {isLoading ? (
                [...Array(5)].map((_, i) => (
                  <TableRow key={i}>
                    {[...Array(5)].map((_, j) => (
                      <TableCell key={j}><Skeleton variant="text" /></TableCell>
                    ))}
                  </TableRow>
                ))
              ) : tenants.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={5}>
                    <EmptyState icon={BusinessIcon} title="No tenants yet" subtitle="Create your first tenant to get started" />
                  </TableCell>
                </TableRow>
              ) : (
                tenants.map((t) => (
                  <TableRow key={t.id} hover>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>{t.name}</Typography>
                      <Typography variant="caption" sx={{ color: 'text.disabled' }}>{t.id}</Typography>
                    </TableCell>
                    <TableCell>
                      <Typography variant="body2" sx={{ fontFamily: 'monospace', color: 'primary.light' }}>
                        {t.slug}
                      </Typography>
                    </TableCell>
                    <TableCell><StatusChip status={t.status} /></TableCell>
                    <TableCell>{t.memberCount}</TableCell>
                    <TableCell>
                      <Typography variant="caption">{new Date(t.createdAtUtc).toLocaleDateString()}</Typography>
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      </Card>

      <Dialog open={dialogOpen} onClose={handleClose} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontWeight: 700 }}>Create Tenant</DialogTitle>
        <Box component="form" onSubmit={handleSubmit((d) => mutation.mutate(d))}>
          <DialogContent>
            <ProblemAlert error={createError} />
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField label="Tenant Name" {...register('name')} error={!!errors.name} helperText={errors.name?.message} fullWidth />
              <TextField label="Slug" placeholder="my-org" {...register('slug')} error={!!errors.slug} helperText={errors.slug?.message} fullWidth />
              <TextField label="Owner User ID" placeholder="UUID" {...register('ownerUserId')} error={!!errors.ownerUserId} helperText={errors.ownerUserId?.message} fullWidth />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={handleClose} color="inherit">Cancel</Button>
            <Button type="submit" variant="contained" disabled={mutation.isPending}>
              {mutation.isPending ? 'Creating…' : 'Create Tenant'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </motion.div>
  )
}
