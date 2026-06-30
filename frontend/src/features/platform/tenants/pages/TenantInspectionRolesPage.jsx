import { useState } from 'react'
import { useParams } from 'react-router-dom'
import {
  Box, Card, CardContent, Typography, Chip, List, ListItem,
  ListItemButton, ListItemText, Divider,
} from '@mui/material'
import { Lock as LockIcon } from '@mui/icons-material'
import { useQuery } from '@tanstack/react-query'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'

function useInspectionRoles(tenantId) {
  return useQuery({
    queryKey: ['platform', 'inspection', tenantId, 'roles'],
    queryFn: () => api.get(`/platform/tenants/${tenantId}/roles`).then(r => r.data),
  })
}

export default function TenantInspectionRolesPage() {
  const { tenantId } = useParams()
  const { data: roles, isLoading } = useInspectionRoles(tenantId)
  const [selected, setSelected] = useState(null)

  const selectedRole = roles?.find(r => r.id === selected) ?? roles?.[0] ?? null

  return (
    <Box>
      <PageHeader
        title="Roles"
        subtitle="Read-only view of tenant role configuration"
      />

      <Box sx={{ display: 'flex', gap: 2, minHeight: 400 }}>
        {/* Left: role list */}
        <Card sx={{ width: 260, flexShrink: 0 }}>
          <List disablePadding dense>
            {(roles ?? []).map((role, idx) => (
              <Box key={role.id}>
                {idx > 0 && <Divider />}
                <ListItem disablePadding>
                  <ListItemButton
                    selected={selectedRole?.id === role.id}
                    onClick={() => setSelected(role.id)}
                    sx={{ py: 1 }}
                  >
                    <ListItemText
                      primary={
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                          <Typography variant="body2" fontWeight={selectedRole?.id === role.id ? 600 : 400}>
                            {role.name}
                          </Typography>
                          {role.isSystem && (
                            <LockIcon sx={{ fontSize: 12, color: 'text.disabled' }} />
                          )}
                        </Box>
                      }
                      secondary={`${role.permissionCount ?? 0} permissions`}
                    />
                  </ListItemButton>
                </ListItem>
              </Box>
            ))}
            {!isLoading && !roles?.length && (
              <ListItem>
                <Typography variant="body2" color="text.secondary">No roles found</Typography>
              </ListItem>
            )}
          </List>
        </Card>

        {/* Right: permissions */}
        <Card sx={{ flex: 1 }}>
          <CardContent>
            {selectedRole ? (
              <>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                  <Typography variant="h6" fontWeight={600}>{selectedRole.name}</Typography>
                  {selectedRole.isSystem && (
                    <Chip label="System" size="small" icon={<LockIcon />} />
                  )}
                </Box>

                {selectedRole.description && (
                  <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
                    {selectedRole.description}
                  </Typography>
                )}

                <Typography variant="overline" color="text.secondary" display="block" mb={1}>
                  Permissions
                </Typography>

                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1 }}>
                  {(selectedRole.permissions ?? []).map((perm) => (
                    <Chip
                      key={perm.code}
                      label={perm.code}
                      size="small"
                      sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }}
                    />
                  ))}
                  {!selectedRole.permissions?.length && (
                    <Typography variant="body2" color="text.secondary">No permissions assigned</Typography>
                  )}
                </Box>
              </>
            ) : (
              <Typography variant="body2" color="text.secondary">
                Select a role to view its permissions
              </Typography>
            )}
          </CardContent>
        </Card>
      </Box>
    </Box>
  )
}
