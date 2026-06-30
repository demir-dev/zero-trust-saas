import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { Box, Card, CardContent, Typography } from '@mui/material'
import { useQuery } from '@tanstack/react-query'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import StatusChip from '../../../../shared/components/StatusChip'
import DataTable from '../../../../shared/components/DataTable'

function useInspectionDevices(tenantId, page, pageSize) {
  return useQuery({
    queryKey: ['platform', 'inspection', tenantId, 'devices', page, pageSize],
    queryFn: () =>
      api.get(`/platform/tenants/${tenantId}/devices?page=${page + 1}&pageSize=${pageSize}`)
        .then(r => r.data),
  })
}

export default function TenantInspectionDevicesPage() {
  const { tenantId } = useParams()
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 20 })

  const { data, isLoading } = useInspectionDevices(
    tenantId,
    paginationModel.page,
    paginationModel.pageSize,
  )

  const columns = [
    {
      field: 'name', headerName: 'Device', flex: 1, minWidth: 150,
      renderCell: ({ value }) => (
        <Typography variant="body2" fontWeight={500}>{value}</Typography>
      ),
    },
    {
      field: 'status', headerName: 'Status', width: 120,
      renderCell: ({ value }) => <StatusChip status={value} />,
    },
    {
      field: 'ipAddress', headerName: 'IP Address', width: 150,
      renderCell: ({ value }) => (
        <Typography variant="caption" sx={{ fontFamily: 'monospace', color: 'text.secondary' }}>
          {value || '—'}
        </Typography>
      ),
    },
    {
      field: 'country', headerName: 'Country', width: 120,
      renderCell: ({ value }) => value || '—',
    },
    {
      field: 'browser', headerName: 'Browser', width: 140,
      renderCell: ({ value }) => (
        <Typography variant="caption" color="text.secondary" noWrap>{value || '—'}</Typography>
      ),
    },
    {
      field: 'trustedAtUtc', headerName: 'Trusted At', width: 160,
      renderCell: ({ value }) => value
        ? new Date(value).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' })
        : '—',
    },
    {
      field: 'lastSeenAtUtc', headerName: 'Last Seen', width: 160,
      renderCell: ({ value }) => value
        ? new Date(value).toLocaleString(undefined, { dateStyle: 'short', timeStyle: 'short' })
        : '—',
    },
  ]

  const items = Array.isArray(data) ? data : (data?.items ?? [])

  return (
    <Box>
      <PageHeader title="Devices" subtitle="Trusted devices registered by tenant members" />

      <Card>
        <CardContent sx={{ p: 0, '&:last-child': { pb: 0 } }}>
          <DataTable
            rows={items}
            columns={columns}
            loading={isLoading}
            rowCount={items.length}
            paginationModel={paginationModel}
            onPaginationModelChange={setPaginationModel}
            sx={{ minHeight: 400 }}
          />
        </CardContent>
      </Card>
    </Box>
  )
}
