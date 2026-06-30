import { useState } from 'react'
import {
  Box, Grid, Card, CardContent, Typography, Button, Chip, List,
  ListItem, Divider, Skeleton, Dialog, DialogTitle, DialogContent,
  DialogActions, TextField, Stack,
} from '@mui/material'
import { Add as AddIcon, AdminPanelSettings as RolesIcon, Key as KeyIcon, Lock } from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import EmptyState from '../../../../shared/components/EmptyState'
import ProblemAlert from '../../../../shared/components/ProblemAlert'
import { useAuth } from '../../../auth/store/authStore'

function useRoles(tenantId) {
  return useQuery({
    queryKey: ['tenant', 'roles', tenantId],
    queryFn: () => api.get(`/authorization/roles?tenantId=${tenantId}`).then(r => r.data),
    enabled: !!tenantId,
  })
}

export default function TenantRolesPage() {
  const { tenantId } = useAuth()
  const [selected, setSelected] = useState(null)
  const [roleDialogOpen, setRoleDialogOpen] = useState(false)
  const [permDialogOpen, setPermDialogOpen] = useState(false)
  const [createError, setCreateError] = useState(null)
  const [permError, setPermError] = useState(null)
  const queryClient = useQueryClient()
  const { data: roles, isLoading } = useRoles(tenantId)

  const { register: regRole, handleSubmit: hsRole, reset: resetRole, formState: { errors: roleErrors } } = useForm()
  const { register: regPerm, handleSubmit: hsPerm, reset: resetPerm } = useForm()

  const createRoleMutation = useMutation({
    mutationFn: (body) => api.post('/authorization/roles', { ...body, tenantId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tenant', 'roles'] })
      setRoleDialogOpen(false)
      resetRole()
      setCreateError(null)
    },
    onError: (err) => setCreateError(err),
  })

  const assignPermMutation = useMutation({
    mutationFn: ({ id, code }) => api.post(`/authorization/roles/${id}/permissions`, { permissionCode: code }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tenant', 'roles'] })
      setPermDialogOpen(false)
      resetPerm()
      setPermError(null)
    },
    onError: (err) => setPermError(err),
  })

  const selectedRole = roles?.find(r => r.id === selected)

  const handleCloseRoleDialog = () => {
    setRoleDialogOpen(false)
    resetRole()
    setCreateError(null)
  }

  const handleClosePermDialog = () => {
    setPermDialogOpen(false)
    resetPerm()
    setPermError(null)
  }

  return (
    <Box>
      <PageHeader
        title="Roles & Permissions"
        subtitle="Manage access control for your organization"
        action={
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setRoleDialogOpen(true)}>
            New Role
          </Button>
        }
      />

      <Grid container spacing={3}>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary" mb={2} fontWeight={600} sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
                Roles
              </Typography>
              {isLoading ? (
                <Stack spacing={1}>{[...Array(4)].map((_, i) => <Skeleton key={i} height={44} />)}</Stack>
              ) : !roles?.length ? (
                <EmptyState icon={RolesIcon} title="No roles" subtitle="Create your first role" />
              ) : (
                <List dense disablePadding>
                  {roles.map((role) => (
                    <ListItem key={role.id} disablePadding>
                      <Box
                        onClick={() => setSelected(role.id)}
                        sx={{
                          width: '100%', p: 1.5, borderRadius: 2, cursor: 'pointer',
                          bgcolor: selected === role.id ? 'rgba(6,182,212,0.12)' : 'transparent',
                          '&:hover': { bgcolor: 'rgba(6,182,212,0.08)' },
                          display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                        }}
                      >
                        <Box>
                          <Typography variant="body2" fontWeight={600}>{role.name}</Typography>
                          <Typography variant="caption" color="text.disabled">{role.scope}</Typography>
                        </Box>
                        {role.isSystem && <Lock sx={{ fontSize: 14, color: 'text.disabled' }} />}
                      </Box>
                    </ListItem>
                  ))}
                </List>
              )}
            </CardContent>
          </Card>
        </Grid>

        <Grid item xs={12} md={8}>
          <Card sx={{ minHeight: 300 }}>
            <CardContent>
              {!selectedRole ? (
                <EmptyState icon={KeyIcon} title="Select a role" subtitle="Click a role to view its permissions" />
              ) : (
                <>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
                    <Box>
                      <Typography variant="h6" fontWeight={700}>{selectedRole.name}</Typography>
                      <Typography variant="caption" color="text.disabled">
                        {selectedRole.scope} scope · {selectedRole.permissions?.length ?? 0} permissions
                        {selectedRole.isSystem && ' · System role (read-only)'}
                      </Typography>
                    </Box>
                    {!selectedRole.isSystem && (
                      <Button size="small" startIcon={<AddIcon />} onClick={() => setPermDialogOpen(true)}>
                        Add Permission
                      </Button>
                    )}
                  </Box>
                  <Divider sx={{ mb: 2 }} />
                  {!selectedRole.permissions?.length ? (
                    <Typography variant="body2" color="text.secondary" textAlign="center" py={3}>
                      No permissions assigned yet.
                    </Typography>
                  ) : (
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                      {selectedRole.permissions.map((p) => (
                        <Chip
                          key={p}
                          label={p}
                          size="small"
                          variant="outlined"
                          color="secondary"
                          sx={{ fontFamily: 'monospace' }}
                        />
                      ))}
                    </Box>
                  )}
                </>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Dialog open={roleDialogOpen} onClose={handleCloseRoleDialog} maxWidth="sm" fullWidth PaperProps={{ sx: { borderRadius: 3 } }}>
        <DialogTitle fontWeight={700}>Create Role</DialogTitle>
        <Box component="form" onSubmit={hsRole((d) => createRoleMutation.mutate(d))}>
          <DialogContent>
            <ProblemAlert error={createError} />
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField label="Role Name" {...regRole('name', { required: true })} error={!!roleErrors.name} helperText={roleErrors.name && 'Required'} fullWidth />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={handleCloseRoleDialog} color="inherit">Cancel</Button>
            <Button type="submit" variant="contained" disabled={createRoleMutation.isPending}>
              {createRoleMutation.isPending ? 'Creating…' : 'Create Role'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      <Dialog open={permDialogOpen} onClose={handleClosePermDialog} maxWidth="sm" fullWidth PaperProps={{ sx: { borderRadius: 3 } }}>
        <DialogTitle fontWeight={700}>Assign Permission</DialogTitle>
        <Box component="form" onSubmit={hsPerm((d) => assignPermMutation.mutate({ id: selected, code: d.permissionCode }))}>
          <DialogContent>
            <ProblemAlert error={permError} />
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField
                label="Permission Code"
                placeholder="users.read"
                {...regPerm('permissionCode', { required: true })}
                helperText="Format: resource.action (e.g. users.read)"
                fullWidth
              />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={handleClosePermDialog} color="inherit">Cancel</Button>
            <Button type="submit" variant="contained" disabled={assignPermMutation.isPending}>
              {assignPermMutation.isPending ? 'Assigning…' : 'Assign Permission'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>
    </Box>
  )
}
