import { DataGrid } from '@mui/x-data-grid'
import { Box } from '@mui/material'

export default function DataTable({
  rows,
  columns,
  loading,
  rowCount,
  paginationModel,
  onPaginationModelChange,
  onRowClick,
  getRowId,
  sx,
  ...props
}) {
  return (
    <Box
      sx={{
        '& .MuiDataGrid-root': { border: 'none' },
        '& .MuiDataGrid-columnHeaders': {
          bgcolor: 'rgba(148,163,184,0.06)',
          borderBottom: '1px solid',
          borderColor: 'divider',
        },
        '& .MuiDataGrid-row:hover': { bgcolor: 'rgba(99,102,241,0.04)' },
        '& .MuiDataGrid-cell': {
          borderColor: 'divider',
          display: 'flex',
          alignItems: 'center',
        },
        '& .MuiDataGrid-footerContainer': { borderTop: '1px solid', borderColor: 'divider' },
        ...sx,
      }}
    >
      <DataGrid
        rows={rows ?? []}
        columns={columns}
        loading={loading}
        rowCount={rowCount ?? 0}
        paginationModel={paginationModel}
        onPaginationModelChange={onPaginationModelChange}
        paginationMode="server"
        pageSizeOptions={[10, 20, 50]}
        onRowClick={onRowClick}
        getRowId={getRowId ?? ((row) => row.id)}
        disableRowSelectionOnClick
        autoHeight
        sx={{ minHeight: 200 }}
        {...props}
      />
    </Box>
  )
}
