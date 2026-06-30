import { useState } from 'react'
import {
  Box, Grid, Card, CardContent, Typography, Button, Chip, List,
  ListItem, ListItemText, Divider, Skeleton, Drawer, TextField, Stack, Select,
  MenuItem, FormControl, InputLabel
} from '@mui/material'
import { Add as AddIcon, AdminPanelSettings as RolesIcon, Key as KeyIcon } from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { motion } from 'framer-motion'
import { pageVariants } from '../../../shared/utils/motionVariants'
import api from '../../../shared/api/axiosInstance'
import PageHeader from '../../../shared/components/PageHeader'
import EmptyState from '../../../shared/components/EmptyState'
import ProblemAlert from '../../../shared/components/ProblemAlert'

function useRoles() {
  return useQuery({
    queryKey: ['roles'],
    queryFn: () => api.get('/authorization/roles').then(r => r.data),
  })
}

export default function RolesPage() {
  const [selected, setSelected] = useState(null)
  const [drawerOpen, setDrawerOpen] = useState(false)
  const [permDrawerOpen, setPermDrawerOpen] = useState(false)
  const [createError, setCreateError] = useState(null)
  const queryClient = useQueryClient()
  const { data: roles, isLoading } = useRoles()

  const { register: regRole, handleSubmit: hsRole, reset: resetRole, formState: { errors: roleErrors } } = useForm()
  const { register: regPerm, handleSubmit: hsPerm, reset: resetPerm } = useForm()

  const createRoleMutation = useMutation({
    mutationFn: (body) => api.post('/authorization/roles', body),
    onSuccess: () => { queryClient.invalidateQueries(['roles']); setDrawerOpen(false); resetRole() },
    onError: (err) => setCreateError(err),
  })

  const assignPermMutation = useMutation({
    mutationFn: ({ id, code }) => api.post(`/authorization/roles/${id}/permissions`, { permissionCode: code }),
    onSuccess: () => { queryClient.invalidateQueries(['roles']); setPermDrawerOpen(false); resetPerm() },
  })

  const selectedRole = roles?.find(r => r.id === selected)

  return (
    <motion.div variants={pageVariants} initial="initial" animate="animate">
      <PageHeader
        title="Roles & Permissions"
        subtitle="Manage access control for your organization"
        action={
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setDrawerOpen(true)}>
            New Role
          </Button>
        }
      />

      <Grid container spacing={3}>
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="subtitle2" sx={{ color: 'text.secondary', mb: 2, fontWeight: 600 }}>
                ROLES
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
                          bgcolor: selected === role.id ? 'rgba(99,102,241,0.12)' : 'transparent',
                          '&:hover': { bgcolor: 'rgba(99,102,241,0.08)' },
                          display: 'flex', justifyContent: 'space-between', alignItems: 'center',
                        }}
                      >
                        <Box>
                          <Typography variant="body2" sx={{ fontWeight: 600 }}>{role.name}</Typography>
                          <Typography variant="caption" sx={{ color: 'text.disabled' }}>{role.scope}</Typography>
                        </Box>
                        {role.isSystem && <Chip label="System" size="small" color="secondary" />}
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
                      <Typography variant="h6" sx={{ fontWeight: 700 }}>{selectedRole.name}</Typography>
                      <Typography variant="caption" sx={{ color: 'text.disabled' }}>
                        {selectedRole.scope} scope · {selectedRole.permissions.length} permissions
                      </Typography>
                    </Box>
                    {!selectedRole.isSystem && (
                      <Button size="small" startIcon={<AddIcon />} onClick={() => setPermDrawerOpen(true)}>
                        Add Permission
                      </Button>
                    )}
                  </Box>
                  <Divider sx={{ mb: 2 }} />
                  {selectedRole.permissions.length === 0 ? (
                    <Typography variant="body2" sx={{ color: 'text.secondary', textAlign: 'center', py: 3 }}>
                      No permissions assigned yet.
                    </Typography>
                  ) : (
                    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                      {selectedRole.permissions.map((p) => (
                        <Chip key={p} label={p} size="small" variant="outlined" color="primary" sx={{ fontFamily: 'monospace' }} />
                      ))}
                    </Box>
                  )}
                </>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      <Drawer anchor="right" open={drawerOpen} onClose={() => setDrawerOpen(false)}
        PaperProps={{ sx: { width: { xs: '100%', sm: 380 }, p: 3 } }}
      >
        <Typography variant="h6" sx={{ fontWeight: 700, mb: 2 }}>Create Role</Typography>
        <ProblemAlert error={createError} />
        <Box component="form" onSubmit={hsRole((d) => createRoleMutation.mutate(d))}>
          <Stack spacing={2}>
            <TextField label="Role Name" {...regRole('name', { required: true })} error={!!roleErrors.name} helperText={roleErrors.name && 'Required'} />
            <FormControl>
              <InputLabel>Scope</InputLabel>
              <Select defaultValue="Tenant" {...regRole('scope')} label="Scope">
                <MenuItem value="Global">Global</MenuItem>
                <MenuItem value="Tenant">Tenant</MenuItem>
                <MenuItem value="User">User</MenuItem>
              </Select>
            </FormControl>
            <TextField label="Tenant ID (optional)" {...regRole('tenantId')} placeholder="UUID" />
            <Button type="submit" variant="contained" size="large" disabled={createRoleMutation.isPending} fullWidth>
              {createRoleMutation.isPending ? 'Creating…' : 'Create Role'}
            </Button>
          </Stack>
        </Box>
      </Drawer>

      <Drawer anchor="right" open={permDrawerOpen} onClose={() => setPermDrawerOpen(false)}
        PaperProps={{ sx: { width: { xs: '100%', sm: 380 }, p: 3 } }}
      >
        <Typography variant="h6" sx={{ fontWeight: 700, mb: 2 }}>Assign Permission</Typography>
        <Box component="form" onSubmit={hsPerm((d) => assignPermMutation.mutate({ id: selected, code: d.permissionCode }))}>
          <Stack spacing={2}>
            <TextField label="Permission Code" placeholder="users.read" {...regPerm('permissionCode', { required: true })}
              helperText="Format: resource.action (e.g. users.read)" />
            <Button type="submit" variant="contained" disabled={assignPermMutation.isPending} fullWidth>
              {assignPermMutation.isPending ? 'Assigning…' : 'Assign Permission'}
            </Button>
          </Stack>
        </Box>
      </Drawer>
    </motion.div>
  )
}
