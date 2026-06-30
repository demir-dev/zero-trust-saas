import { useState } from 'react'
import {
  Box, Card, CardContent, Typography, TextField, MenuItem, Select,
  FormControl, InputLabel, Dialog, DialogTitle, DialogContent,
  DialogActions, Button, Chip, IconButton, Tooltip,
} from '@mui/material'
import { Info as InfoIcon } from '@mui/icons-material'
import { useQuery } from '@tanstack/react-query'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import SeverityBadge from '../../../../shared/components/SeverityBadge'
import DataTable from '../../../../shared/components/DataTable'

const SEVERITIES = ['All', 'Info', 'Warning', 'Critical']

function useAuditLogs(page, pageSize, severity) {
  return useQuery({
    queryKey: ['platform', 'audit', page, pageSize, severity],
    queryFn: () =>
      api.get(`/dashboard/audit?page=${page + 1}&pageSize=${pageSize}`).then(r => r.data),
  })
}

export default function GlobalAuditPage() {
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 20 })
  const [severityFilter, setSeverityFilter] = useState('All')
  const [selectedLog, setSelectedLog] = useState(null)

  const { data, isLoading } = useAuditLogs(paginationModel.page, paginationModel.pageSize, severityFilter)

  const filteredItems = severityFilter === 'All'
    ? (data?.items ?? [])
    : (data?.items ?? []).filter(l => l.severity === severityFilter)

  const columns = [
    {
      field: 'severity', headerName: 'Severity', width: 110,
      renderCell: ({ value }) => <SeverityBadge severity={value} />,
    },
    {
      field: 'eventType', headerName: 'Event', flex: 1.5, minWidth: 180,
      renderCell: ({ value }) => (
        <Typography variant="body2" fontWeight={500}>
          {value?.replace(/([A-Z])/g, ' $1').trim()}
        </Typography>
      ),
    },
    {
      field: 'isSecurityCritical', headerName: 'Critical', width: 90,
      renderCell: ({ value }) => value ? <Chip label="Yes" size="small" color="error" /> : null,
    },
    {
      field: 'ipAddress', headerName: 'IP Address', width: 140,
      renderCell: ({ value }) => value ? (
        <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>{value}</Typography>
      ) : '—',
    },
    {
      field: 'occurredAtUtc', headerName: 'Date', width: 160,
      renderCell: ({ value }) => value
        ? new Date(value).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' })
        : '—',
    },
    {
      field: 'details', headerName: '', width: 60, sortable: false,
      renderCell: ({ row }) => (
        <Tooltip title="View details">
          <IconButton size="small" onClick={(e) => { e.stopPropagation(); setSelectedLog(row) }}>
            <InfoIcon fontSize="small" />
          </IconButton>
        </Tooltip>
      ),
    },
  ]

  return (
    <Box>
      <PageHeader
        title="Global Audit Logs"
        subtitle="All security events across the platform"
      />

      <Card sx={{ mb: 2 }}>
        <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
          <Box sx={{ display: 'flex', gap: 2, alignItems: 'center', flexWrap: 'wrap' }}>
            <FormControl size="small" sx={{ minWidth: 140 }}>
              <InputLabel>Severity</InputLabel>
              <Select
                label="Severity"
                value={severityFilter}
                onChange={(e) => setSeverityFilter(e.target.value)}
              >
                {SEVERITIES.map(s => <MenuItem key={s} value={s}>{s}</MenuItem>)}
              </Select>
            </FormControl>
            {severityFilter !== 'All' && (
              <Chip
                label={`Filtered: ${severityFilter}`}
                onDelete={() => setSeverityFilter('All')}
                size="small"
              />
            )}
            <Typography variant="body2" color="text.secondary" sx={{ ml: 'auto' }}>
              {data?.total ?? 0} total events
            </Typography>
          </Box>
        </CardContent>
      </Card>

      <Card>
        <CardContent sx={{ p: 0, '&:last-child': { pb: 0 } }}>
          <DataTable
            rows={filteredItems}
            columns={columns}
            loading={isLoading}
            rowCount={data?.total ?? 0}
            paginationModel={paginationModel}
            onPaginationModelChange={setPaginationModel}
            onRowClick={({ row }) => setSelectedLog(row)}
            sx={{ minHeight: 400 }}
          />
        </CardContent>
      </Card>

      <Dialog
        open={!!selectedLog}
        onClose={() => setSelectedLog(null)}
        maxWidth="sm"
        fullWidth
        PaperProps={{ sx: { borderRadius: 3 } }}
      >
        <DialogTitle>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <SeverityBadge severity={selectedLog?.severity} />
            <Typography variant="h6" fontWeight={600}>
              {selectedLog?.eventType?.replace(/([A-Z])/g, ' $1').trim()}
            </Typography>
          </Box>
        </DialogTitle>
        <DialogContent>
          {selectedLog && (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
              {[
                { label: 'Occurred At', value: selectedLog.occurredAtUtc ? new Date(selectedLog.occurredAtUtc).toLocaleString() : '—' },
                { label: 'IP Address', value: selectedLog.ipAddress ?? '—' },
                { label: 'User Agent', value: selectedLog.userAgent ?? '—' },
                { label: 'Security Critical', value: selectedLog.isSecurityCritical ? 'Yes' : 'No' },
                { label: 'Metadata', value: selectedLog.metadata ?? '—' },
              ].map(({ label, value }) => (
                <Box key={label}>
                  <Typography variant="caption" color="text.secondary" sx={{ textTransform: 'uppercase', letterSpacing: 0.5 }}>
                    {label}
                  </Typography>
                  <Typography variant="body2" sx={{ mt: 0.25, wordBreak: 'break-all' }}>{value}</Typography>
                </Box>
              ))}
            </Box>
          )}
        </DialogContent>
        <DialogActions sx={{ px: 3, pb: 2 }}>
          <Button onClick={() => setSelectedLog(null)} variant="outlined">Close</Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
