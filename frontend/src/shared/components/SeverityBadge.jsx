import { Chip } from '@mui/material'

const severityConfig = {
  Info: { color: 'info', label: 'Info' },
  Warning: { color: 'warning', label: 'Warning' },
  High: { sx: { bgcolor: '#f97316', color: '#fff' }, label: 'High' },
  Critical: { color: 'error', label: 'Critical' },
}

export default function SeverityBadge({ severity }) {
  const config = severityConfig[severity] || { color: 'default', label: severity }
  return <Chip label={config.label} color={config.color} size="small" sx={config.sx} />
}
