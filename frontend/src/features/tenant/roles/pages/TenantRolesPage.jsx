import { useState, useMemo } from 'react'
import {
  Box, Grid, Card, CardContent, Typography, Button, Chip, List,
  ListItem, Divider, Skeleton, Dialog, DialogTitle, DialogContent,
  DialogActions, TextField, Stack, IconButton, Tooltip, InputAdornment,
  Checkbox, FormControlLabel, FormGroup,
} from '@mui/material'
import {
  Add as AddIcon,
  Delete as DeleteIcon,
  Edit as EditIcon,
  ContentCopy as CloneIcon,
  Close as CloseIcon,
  Lock as LockIcon,
  AdminPanelSettings as RolesIcon,
  Key as KeyIcon,
  Search as SearchIcon,
} from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import EmptyState from '../../../../shared/components/EmptyState'
import ProblemAlert from '../../../../shared/components/ProblemAlert'
import ConfirmDialog from '../../../../shared/components/ConfirmDialog'
import { useAuth } from '../../../auth/store/authStore'

function useRoles(tenantId) {
  return useQuery({
    queryKey: ['tenant', 'roles', tenantId],
    queryFn: () => api.get(`/authorization/roles?tenantId=${tenantId}`).then(r => r.data),
    enabled: !!tenantId,
  })
}

function usePermissions() {
  return useQuery({
    queryKey: ['authorization', 'permissions'],
    queryFn: () => api.get('/authorization/permissions').then(r => r.data),
    staleTime: Infinity,
  })
}

export default function TenantRolesPage() {
  const { tenantId, hasPermission } = useAuth()
  const canManageRoles = hasPermission('role.manage')
  const [selected, setSelected] = useState(null)
  const queryClient = useQueryClient()

  const [createRoleOpen, setCreateRoleOpen] = useState(false)
  const [renameOpen, setRenameOpen] = useState(false)
  const [cloneOpen, setCloneOpen] = useState(false)
  const [addPermOpen, setAddPermOpen] = useState(false)
  const [deleteConfirm, setDeleteConfirm] = useState(false)
  const [removePerm, setRemovePerm] = useState(null)

  const [createError, setCreateError] = useState(null)
  const [renameError, setRenameError] = useState(null)
  const [cloneError, setCloneError] = useState(null)
  const [addPermError, setAddPermError] = useState(null)

  const [permSearch, setPermSearch] = useState('')
  const [selectedPermCodes, setSelectedPermCodes] = useState([])

  const { data: roles, isLoading } = useRoles(tenantId)
  const { data: allPermissions } = usePermissions()

  const { register: regCreate, handleSubmit: hsCreate, reset: resetCreate, formState: { errors: createErrors } } = useForm()
  const { register: regRename, handleSubmit: hsRename, reset: resetRename, formState: { errors: renameErrors } } = useForm()
  const { register: regClone, handleSubmit: hsClone, reset: resetClone, formState: { errors: cloneErrors } } = useForm()

  const invalidateRoles = () => queryClient.invalidateQueries({ queryKey: ['tenant', 'roles'] })

  const selectedRole = roles?.find(r => r.id === selected) ?? null

  const assignedCodes = useMemo(
    () => new Set((selectedRole?.permissions ?? []).map(p => typeof p === 'string' ? p : p.code)),
    [selectedRole],
  )

  const filteredPermissions = useMemo(() => {
    if (!allPermissions) return []
    const s = permSearch.toLowerCase()
    return allPermissions.filter(p => {
      const code = typeof p === 'string' ? p : (p.code ?? p)
      return code.toLowerCase().includes(s) && !assignedCodes.has(code)
    })
  }, [allPermissions, permSearch, assignedCodes])

  const createMutation = useMutation({
    mutationFn: (body) => api.post('/authorization/roles', { name: body.name, tenantId }),
    onSuccess: () => { invalidateRoles(); setCreateRoleOpen(false); resetCreate(); setCreateError(null) },
    onError: (err) => setCreateError(err),
  })

  const renameMutation = useMutation({
    mutationFn: (body) => api.put(`/authorization/roles/${selected}`, { name: body.name }),
    onSuccess: () => { invalidateRoles(); setRenameOpen(false); resetRename(); setRenameError(null) },
    onError: (err) => setRenameError(err),
  })

  const cloneMutation = useMutation({
    mutationFn: (body) => api.post(`/authorization/roles/${selected}/clone`, { name: body.name }),
    onSuccess: (res) => {
      invalidateRoles()
      setCloneOpen(false)
      resetClone()
      setCloneError(null)
      if (res.data?.id) setSelected(res.data.id)
    },
    onError: (err) => setCloneError(err),
  })

  const deleteMutation = useMutation({
    mutationFn: () => api.delete(`/authorization/roles/${selected}`),
    onSuccess: () => { invalidateRoles(); setDeleteConfirm(false); setSelected(null) },
  })

  const addPermsMutation = useMutation({
    mutationFn: (codes) =>
      Promise.all(codes.map(code =>
        api.post(`/authorization/roles/${selected}/permissions`, { permissionCode: code })
      )),
    onSuccess: () => {
      invalidateRoles()
      setAddPermOpen(false)
      setSelectedPermCodes([])
      setPermSearch('')
      setAddPermError(null)
    },
    onError: (err) => setAddPermError(err),
  })

  const removePermMutation = useMutation({
    mutationFn: (code) => api.delete(`/authorization/roles/${selected}/permissions/${encodeURIComponent(code)}`),
    onSuccess: () => { invalidateRoles(); setRemovePerm(null) },
  })

  const openAddPerm = () => {
    setSelectedPermCodes([])
    setPermSearch('')
    setAddPermError(null)
    setAddPermOpen(true)
  }

  const togglePermCode = (code) => {
    setSelectedPermCodes(prev =>
      prev.includes(code) ? prev.filter(c => c !== code) : [...prev, code]
    )
  }

  return (
    <Box>
      <PageHeader
        title="Roles & Permissions"
        subtitle="Manage access control for your organization"
        action={canManageRoles && (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateRoleOpen(true)}>
            New Role
          </Button>
        )}
      />

      <Grid container spacing={3}>
        {/* Left: role list */}
        <Grid item xs={12} md={4}>
          <Card>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary" mb={2} fontWeight={600}
                sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
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
                        <Box sx={{ flex: 1, minWidth: 0 }}>
                          <Typography variant="body2" fontWeight={600} noWrap>{role.name}</Typography>
                          <Typography variant="caption" color="text.disabled">{role.scope}</Typography>
                        </Box>
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, flexShrink: 0 }}>
                          {role.isSystem && (
                            <Tooltip title="System role — cannot be modified or deleted">
                              <LockIcon sx={{ fontSize: 14, color: 'text.disabled' }} />
                            </Tooltip>
                          )}
                          {!role.isSystem && canManageRoles && selected === role.id && (
                            <>
                              <Tooltip title="Rename">
                                <IconButton
                                  size="small"
                                  onClick={(e) => { e.stopPropagation(); resetRename(); setRenameError(null); setRenameOpen(true) }}
                                >
                                  <EditIcon sx={{ fontSize: 14 }} />
                                </IconButton>
                              </Tooltip>
                              <Tooltip title="Clone">
                                <IconButton
                                  size="small"
                                  onClick={(e) => { e.stopPropagation(); resetClone(); setCloneError(null); setCloneOpen(true) }}
                                >
                                  <CloneIcon sx={{ fontSize: 14 }} />
                                </IconButton>
                              </Tooltip>
                              <Tooltip title="Delete role">
                                <IconButton
                                  size="small"
                                  onClick={(e) => { e.stopPropagation(); setDeleteConfirm(true) }}
                                  color="error"
                                >
                                  <DeleteIcon sx={{ fontSize: 14 }} />
                                </IconButton>
                              </Tooltip>
                            </>
                          )}
                        </Box>
                      </Box>
                    </ListItem>
                  ))}
                </List>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Right: permissions */}
        <Grid item xs={12} md={8}>
          <Card sx={{ minHeight: 300 }}>
            <CardContent>
              {!selectedRole ? (
                <EmptyState icon={KeyIcon} title="Select a role" subtitle="Click a role to view its permissions" />
              ) : (
                <>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                    <Box>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="h6" fontWeight={700}>{selectedRole.name}</Typography>
                        {selectedRole.isSystem && (
                          <Chip label="System" size="small" icon={<LockIcon />} />
                        )}
                      </Box>
                      <Typography variant="caption" color="text.disabled">
                        {selectedRole.scope} scope · {selectedRole.permissions?.length ?? 0} permissions
                      </Typography>
                    </Box>
                    {!selectedRole.isSystem && canManageRoles && (
                      <Button size="small" variant="outlined" startIcon={<AddIcon />} onClick={openAddPerm}>
                        Add Permissions
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
                      {selectedRole.permissions.map((p) => {
                        const code = typeof p === 'string' ? p : p.code
                        return (
                          <Chip
                            key={code}
                            label={code}
                            size="small"
                            variant="outlined"
                            color="secondary"
                            sx={{ fontFamily: 'monospace' }}
                            {...(!selectedRole.isSystem && canManageRoles && {
                              onDelete: () => setRemovePerm(code),
                              deleteIcon: (
                                <Tooltip title="Remove permission">
                                  <CloseIcon />
                                </Tooltip>
                              ),
                            })}
                          />
                        )
                      })}
                    </Box>
                  )}
                </>
              )}
            </CardContent>
          </Card>
        </Grid>
      </Grid>

      {/* Create Role dialog */}
      <Dialog open={createRoleOpen} onClose={() => { setCreateRoleOpen(false); resetCreate(); setCreateError(null) }}
        maxWidth="sm" fullWidth PaperProps={{ sx: { borderRadius: 3 } }}>
        <DialogTitle fontWeight={700}>Create Role</DialogTitle>
        <Box component="form" onSubmit={hsCreate((d) => createMutation.mutate(d))}>
          <DialogContent>
            <ProblemAlert error={createError} />
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField label="Role Name" {...regCreate('name', { required: true })}
                error={!!createErrors.name} helperText={createErrors.name && 'Required'} fullWidth autoFocus />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => { setCreateRoleOpen(false); resetCreate(); setCreateError(null) }} color="inherit">Cancel</Button>
            <Button type="submit" variant="contained" disabled={createMutation.isPending}>
              {createMutation.isPending ? 'Creating…' : 'Create Role'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      {/* Rename Role dialog */}
      <Dialog open={renameOpen} onClose={() => { setRenameOpen(false); resetRename(); setRenameError(null) }}
        maxWidth="xs" fullWidth PaperProps={{ sx: { borderRadius: 3 } }}>
        <DialogTitle fontWeight={700}>Rename Role</DialogTitle>
        <Box component="form" onSubmit={hsRename((d) => renameMutation.mutate(d))}>
          <DialogContent>
            <ProblemAlert error={renameError} />
            <Stack spacing={2} sx={{ mt: 1 }}>
              <TextField label="New Name" defaultValue={selectedRole?.name}
                {...regRename('name', { required: true })}
                error={!!renameErrors.name} helperText={renameErrors.name && 'Required'}
                fullWidth autoFocus />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => { setRenameOpen(false); resetRename(); setRenameError(null) }} color="inherit">Cancel</Button>
            <Button type="submit" variant="contained" disabled={renameMutation.isPending}>
              {renameMutation.isPending ? 'Saving…' : 'Rename'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      {/* Clone Role dialog */}
      <Dialog open={cloneOpen} onClose={() => { setCloneOpen(false); resetClone(); setCloneError(null) }}
        maxWidth="xs" fullWidth PaperProps={{ sx: { borderRadius: 3 } }}>
        <DialogTitle fontWeight={700}>Clone Role</DialogTitle>
        <Box component="form" onSubmit={hsClone((d) => cloneMutation.mutate(d))}>
          <DialogContent>
            <ProblemAlert error={cloneError} />
            <Stack spacing={2} sx={{ mt: 1 }}>
              <Typography variant="body2" color="text.secondary">
                A copy of <strong>{selectedRole?.name}</strong> will be created with all its permissions.
              </Typography>
              <TextField label="New Role Name" defaultValue={`${selectedRole?.name ?? ''} (Copy)`}
                {...regClone('name', { required: true })}
                error={!!cloneErrors.name} helperText={cloneErrors.name && 'Required'}
                fullWidth autoFocus />
            </Stack>
          </DialogContent>
          <DialogActions sx={{ px: 3, pb: 2 }}>
            <Button onClick={() => { setCloneOpen(false); resetClone(); setCloneError(null) }} color="inherit">Cancel</Button>
            <Button type="submit" variant="contained" disabled={cloneMutation.isPending}>
              {cloneMutation.isPending ? 'Cloning…' : 'Clone Role'}
            </Button>
          </DialogActions>
        </Box>
      </Dialog>

      {/* Add Permissions dialog */}
      <Dialog open={addPermOpen} onClose={() => setAddPermOpen(false)}
        maxWidth="sm" fullWidth PaperProps={{ sx: { borderRadius: 3 } }}>
        <DialogTitle fontWeight={700}>Add Permissions</DialogTitle>
        <DialogContent>
          <ProblemAlert error={addPermError} />
          <TextField
            placeholder="Search permissions…"
            value={permSearch}
            onChange={(e) => setPermSearch(e.target.value)}
            fullWidth
            size="small"
            sx={{ mt: 1, mb: 2 }}
            InputProps={{
              startAdornment: (
                <InputAdornment position="start">
                  <SearchIcon fontSize="small" />
                </InputAdornment>
              ),
            }}
          />

          {filteredPermissions.length === 0 ? (
            <Typography variant="body2" color="text.secondary" textAlign="center" py={2}>
              {permSearch ? 'No permissions match your search.' : 'All permissions are already assigned.'}
            </Typography>
          ) : (
            <FormGroup>
              {filteredPermissions.map((p) => {
                const code = typeof p === 'string' ? p : (p.code ?? p)
                const desc = typeof p === 'string' ? null : p.description
                return (
                  <FormControlLabel
                    key={code}
                    control={
                      <Checkbox
                        size="small"
                        checked={selectedPermCodes.includes(code)}
                        onChange={() => togglePermCode(code)}
                      />
                    }
                    label={
                      <Box>
                        <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>{code}</Typography>
                        {desc && <Typography variant="caption" color="text.secondary">{desc}</Typography>}
                      </Box>
                    }
                  />
                )
              })}
            </FormGroup>
          )}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Typography variant="caption" color="text.secondary" sx={{ flex: 1 }}>
            {selectedPermCodes.length} selected
          </Typography>
          <Button onClick={() => setAddPermOpen(false)} color="inherit">Cancel</Button>
          <Button
            variant="contained"
            disabled={selectedPermCodes.length === 0 || addPermsMutation.isPending}
            onClick={() => addPermsMutation.mutate(selectedPermCodes)}
          >
            {addPermsMutation.isPending ? 'Assigning…' : `Assign ${selectedPermCodes.length > 0 ? `(${selectedPermCodes.length})` : ''}`}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Delete role confirmation */}
      <ConfirmDialog
        open={deleteConfirm}
        title="Delete Role"
        message={`Are you sure you want to delete "${selectedRole?.name}"? This action cannot be undone.`}
        onConfirm={() => deleteMutation.mutate()}
        onCancel={() => setDeleteConfirm(false)}
      />

      {/* Remove permission confirmation */}
      <ConfirmDialog
        open={!!removePerm}
        title="Remove Permission"
        message={`Remove permission "${removePerm}" from this role?`}
        onConfirm={() => removePermMutation.mutate(removePerm)}
        onCancel={() => setRemovePerm(null)}
      />
    </Box>
  )
}
