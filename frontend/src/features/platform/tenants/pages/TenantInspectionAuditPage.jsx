import { useState } from 'react'
import { useParams } from 'react-router-dom'
import {
  Box, Card, CardContent, Typography, Chip, IconButton, Tooltip,
  Dialog, DialogTitle, DialogContent, DialogActions, Button,
} from '@mui/material'
import { Info as InfoIcon } from '@mui/icons-material'
import { useQuery } from '@tanstack/react-query'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import SeverityBadge from '../../../../shared/components/SeverityBadge'
import DataTable from '../../../../shared/components/DataTable'

function useInspectionAudit(tenantId, page, pageSize) {
  return useQuery({
    queryKey: ['platform', 'inspection', tenantId, 'audit', page, pageSize],
    queryFn: () =>
      api.get(`/platform/tenants/${tenantId}/audit?page=${page + 1}&pageSize=${pageSize}`)
        .then(r => r.data),
  })
}

export default function TenantInspectionAuditPage() {
  const { tenantId } = useParams()
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 25 })
  const [selectedLog, setSelectedLog] = useState(null)

  const { data, isLoading } = useInspectionAudit(
    tenantId,
    paginationModel.page,
    paginationModel.pageSize,
  )

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
      renderCell: ({ value }) => value
        ? <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>{value}</Typography>
        : '—',
    },
    {
      field: 'occurredAtUtc', headerName: 'Date', width: 160,
      renderCell: ({ value }) => value
        ? new Date(value).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' })
        : '—',
    },
    {
      field: 'actions', headerName: '', width: 60, sortable: false,
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
      <PageHeader title="Audit Logs" subtitle="Security events for this tenant" />

      <Card>
        <CardContent sx={{ p: 0, '&:last-child': { pb: 0 } }}>
          <DataTable
            rows={data?.items ?? []}
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
