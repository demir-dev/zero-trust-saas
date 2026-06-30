import { Chip } from '@mui/material'

const statusConfig = {
  Active: { color: 'success', label: 'Active' },
  active: { color: 'success', label: 'Active' },
  Trusted: { color: 'success', label: 'Trusted' },
  Suspended: { color: 'warning', label: 'Suspended' },
  suspended: { color: 'warning', label: 'Suspended' },
  Locked: { color: 'warning', label: 'Locked' },
  Pending: { color: 'default', label: 'Pending' },
  PendingVerification: { color: 'default', label: 'Pending' },
  Revoked: { color: 'error', label: 'Revoked' },
  revoked: { color: 'error', label: 'Revoked' },
  Blocked: { color: 'error', label: 'Blocked' },
  Disabled: { color: 'error', label: 'Disabled' },
  disabled: { color: 'error', label: 'Disabled' },
}

export default function StatusChip({ status, size = 'small' }) {
  const config = statusConfig[status] || { color: 'default', label: status }
  return <Chip label={config.label} color={config.color} size={size} variant="filled" />
}
