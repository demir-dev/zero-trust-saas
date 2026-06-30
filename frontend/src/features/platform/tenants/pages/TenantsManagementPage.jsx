import { useState, useEffect, useRef } from 'react'
import {
  Box, Button, Dialog, DialogTitle, DialogContent, DialogActions,
  TextField, MenuItem, Select, FormControl, InputLabel, Typography,
  Chip, IconButton, Tooltip, Stepper, Step, StepLabel,
  InputAdornment, LinearProgress, Card, CardContent, Alert,
} from '@mui/material'
import {
  Add as AddIcon, Pause as SuspendIcon, PlayArrow as ActivateIcon,
  CheckCircle as CheckIcon, Error as ErrorIcon, ArrowForward, ArrowBack,
  Visibility, VisibilityOff, Business as BusinessIcon,
} from '@mui/icons-material'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { motion, AnimatePresence } from 'framer-motion'
import api from '../../../../shared/api/axiosInstance'
import PageHeader from '../../../../shared/components/PageHeader'
import StatusChip from '../../../../shared/components/StatusChip'
import ConfirmDialog from '../../../../shared/components/ConfirmDialog'
import ProblemAlert from '../../../../shared/components/ProblemAlert'
import DataTable from '../../../../shared/components/DataTable'

// ─── Schemas ────────────────────────────────────────────────────────────────
const tenantInfoSchema = z.object({
  name: z.string().min(2, 'At least 2 characters').max(100),
  slug: z.string().regex(/^[a-z0-9][a-z0-9-]{1,48}[a-z0-9]$/, '3–50 chars, lowercase, hyphens only'),
  plan: z.enum(['Free', 'Standard', 'Professional', 'Enterprise']),
})

const ownerSchema = z.object({
  ownerFirstName: z.string().min(1, 'Required'),
  ownerLastName: z.string().min(1, 'Required'),
  ownerEmail: z.string().email('Invalid email'),
  ownerPassword: z
    .string()
    .min(8, 'At least 8 characters')
    .regex(/[A-Z]/, 'One uppercase letter')
    .regex(/[0-9]/, 'One number')
    .regex(/[^A-Za-z0-9]/, 'One special character'),
})

function slugify(name) {
  return name.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '').slice(0, 50)
}

const WIZARD_STEPS = ['Tenant Info', 'Owner Account', 'Review']
const PLAN_OPTIONS = ['Free', 'Standard', 'Professional', 'Enterprise']

// ─── Create Tenant Wizard ────────────────────────────────────────────────────
function CreateTenantWizard({ open, onClose }) {
  const queryClient = useQueryClient()
  const [step, setStep] = useState(0)
  const [showPassword, setShowPassword] = useState(false)
  const [mutError, setMutError] = useState(null)
  const slugManual = useRef(false)

  const [formData, setFormData] = useState({
    name: '', slug: '', plan: 'Free',
    ownerFirstName: '', ownerLastName: '', ownerEmail: '', ownerPassword: '',
  })

  const infoForm = useForm({ resolver: zodResolver(tenantInfoSchema), defaultValues: { name: '', slug: '', plan: 'Free' } })
  const ownerForm = useForm({ resolver: zodResolver(ownerSchema), defaultValues: { ownerFirstName: '', ownerLastName: '', ownerEmail: '', ownerPassword: '' } })

  const nameValue = infoForm.watch('name', '')
  useEffect(() => {
    if (!slugManual.current && nameValue) {
      infoForm.setValue('slug', slugify(nameValue), { shouldValidate: false })
    }
  }, [nameValue])

  const { mutate, isPending, isSuccess } = useMutation({
    mutationFn: (data) => api.post('/platform/tenants', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['platform', 'tenants'] })
      setTimeout(() => { handleClose() }, 2000)
    },
    onError: (err) => setMutError(err),
  })

  const handleClose = () => {
    setStep(0); setMutError(null); slugManual.current = false
    infoForm.reset(); ownerForm.reset()
    setFormData({ name: '', slug: '', plan: 'Free', ownerFirstName: '', ownerLastName: '', ownerEmail: '', ownerPassword: '' })
    onClose()
  }

  const onInfoNext = (data) => {
    setFormData(d => ({ ...d, ...data }))
    setStep(1)
  }

  const onOwnerNext = (data) => {
    setFormData(d => ({ ...d, ...data }))
    setStep(2)
  }

  const onCreate = () => {
    setMutError(null)
    mutate({
      name: formData.name,
      slug: formData.slug,
      plan: formData.plan,
      ownerFirstName: formData.ownerFirstName,
      ownerLastName: formData.ownerLastName,
      ownerEmail: formData.ownerEmail,
      ownerPassword: formData.ownerPassword,
    })
  }

  return (
    <Dialog open={open} onClose={handleClose} maxWidth="sm" fullWidth PaperProps={{ sx: { borderRadius: 3 } }}>
      <DialogTitle sx={{ pb: 1 }}>
        <Typography variant="h6" fontWeight={700}>Create New Tenant</Typography>
      </DialogTitle>

      <DialogContent sx={{ pt: 1 }}>
        <Stepper activeStep={step} sx={{ mb: 3 }}>
          {WIZARD_STEPS.map((label) => (
            <Step key={label}><StepLabel>{label}</StepLabel></Step>
          ))}
        </Stepper>

        <ProblemAlert error={mutError} />

        {step === 0 && (
          <Box component="form" id="info-form" onSubmit={infoForm.handleSubmit(onInfoNext)} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <TextField
              label="Organization Name"
              size="small"
              {...infoForm.register('name')}
              error={!!infoForm.formState.errors.name}
              helperText={infoForm.formState.errors.name?.message}
              InputProps={{ startAdornment: <InputAdornment position="start"><BusinessIcon sx={{ fontSize: 18, color: 'text.secondary' }} /></InputAdornment> }}
            />
            <TextField
              label="Slug"
              size="small"
              {...infoForm.register('slug', { onChange: () => { slugManual.current = true } })}
              error={!!infoForm.formState.errors.slug}
              helperText={infoForm.formState.errors.slug?.message ?? 'Used in login and as unique identifier'}
            />
            <FormControl size="small">
              <InputLabel>Subscription Plan</InputLabel>
              <Select label="Subscription Plan" defaultValue="Free" {...infoForm.register('plan')}>
                {PLAN_OPTIONS.map(p => <MenuItem key={p} value={p}>{p}</MenuItem>)}
              </Select>
            </FormControl>
          </Box>
        )}

        {step === 1 && (
          <Box component="form" id="owner-form" onSubmit={ownerForm.handleSubmit(onOwnerNext)} sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
              <TextField label="First Name" size="small" {...ownerForm.register('ownerFirstName')} error={!!ownerForm.formState.errors.ownerFirstName} helperText={ownerForm.formState.errors.ownerFirstName?.message} />
              <TextField label="Last Name" size="small" {...ownerForm.register('ownerLastName')} error={!!ownerForm.formState.errors.ownerLastName} helperText={ownerForm.formState.errors.ownerLastName?.message} />
            </Box>
            <TextField label="Email" type="email" size="small" {...ownerForm.register('ownerEmail')} error={!!ownerForm.formState.errors.ownerEmail} helperText={ownerForm.formState.errors.ownerEmail?.message} />
            <TextField
              label="Password"
              type={showPassword ? 'text' : 'password'}
              size="small"
              {...ownerForm.register('ownerPassword')}
              error={!!ownerForm.formState.errors.ownerPassword}
              helperText={ownerForm.formState.errors.ownerPassword?.message ?? 'Min 8 chars, uppercase, number, special char'}
              InputProps={{
                endAdornment: (
                  <InputAdornment position="end">
                    <IconButton size="small" onClick={() => setShowPassword(p => !p)}>
                      {showPassword ? <VisibilityOff fontSize="small" /> : <Visibility fontSize="small" />}
                    </IconButton>
                  </InputAdornment>
                ),
              }}
            />
          </Box>
        )}

        {step === 2 && (
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
            {isPending && (
              <Box sx={{ textAlign: 'center', py: 2 }}>
                <Typography variant="body2" color="text.secondary" mb={1}>Provisioning tenant…</Typography>
                <LinearProgress />
              </Box>
            )}
            {isSuccess && (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, justifyContent: 'center', py: 2 }}>
                <CheckIcon sx={{ color: 'success.main', fontSize: 32 }} />
                <Typography variant="h6" color="success.main" fontWeight={600}>Tenant created!</Typography>
              </Box>
            )}
            {!isPending && !isSuccess && (
              <>
                <Card variant="outlined" sx={{ bgcolor: 'background.default' }}>
                  <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
                    <Typography variant="overline" color="text.secondary">Tenant</Typography>
                    <Typography variant="body1" fontWeight={600}>{formData.name}</Typography>
                    <Box sx={{ display: 'flex', gap: 1, mt: 0.5 }}>
                      <Chip label={formData.slug} size="small" />
                      <Chip label={formData.plan} size="small" color="primary" />
                    </Box>
                  </CardContent>
                </Card>
                <Card variant="outlined" sx={{ bgcolor: 'background.default' }}>
                  <CardContent sx={{ py: 1.5, '&:last-child': { pb: 1.5 } }}>
                    <Typography variant="overline" color="text.secondary">Owner</Typography>
                    <Typography variant="body1" fontWeight={600}>{formData.ownerFirstName} {formData.ownerLastName}</Typography>
                    <Typography variant="body2" color="text.secondary">{formData.ownerEmail}</Typography>
                  </CardContent>
                </Card>
              </>
            )}
          </Box>
        )}
      </DialogContent>

      <DialogActions sx={{ px: 3, pb: 2.5, gap: 1 }}>
        {step === 0 && (
          <>
            <Button onClick={handleClose} variant="outlined">Cancel</Button>
            <Button type="submit" form="info-form" variant="contained" endIcon={<ArrowForward />}>Continue</Button>
          </>
        )}
        {step === 1 && (
          <>
            <Button onClick={() => setStep(0)} variant="outlined" startIcon={<ArrowBack />}>Back</Button>
            <Button type="submit" form="owner-form" variant="contained" endIcon={<ArrowForward />}>Continue</Button>
          </>
        )}
        {step === 2 && !isPending && !isSuccess && (
          <>
            <Button onClick={() => setStep(1)} variant="outlined" startIcon={<ArrowBack />}>Back</Button>
            <Button onClick={onCreate} variant="contained" color="primary">Create Tenant</Button>
          </>
        )}
        {(isPending || isSuccess) && <Box sx={{ flex: 1 }} />}
      </DialogActions>
    </Dialog>
  )
}

// ─── Main Page ───────────────────────────────────────────────────────────────
export default function TenantsManagementPage() {
  const queryClient = useQueryClient()
  const [paginationModel, setPaginationModel] = useState({ page: 0, pageSize: 10 })
  const [wizardOpen, setWizardOpen] = useState(false)
  const [confirmAction, setConfirmAction] = useState(null)

  const { data, isLoading } = useQuery({
    queryKey: ['platform', 'tenants', paginationModel],
    queryFn: () => api.get(`/platform/tenants?page=${paginationModel.page + 1}&pageSize=${paginationModel.pageSize}`).then(r => r.data),
  })

  const suspendMutation = useMutation({
    mutationFn: (id) => api.post(`/platform/tenants/${id}/suspend`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['platform', 'tenants'] }),
  })
  const activateMutation = useMutation({
    mutationFn: (id) => api.post(`/platform/tenants/${id}/activate`),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['platform', 'tenants'] }),
  })

  const columns = [
    { field: 'name', headerName: 'Name', flex: 1.5, minWidth: 140 },
    { field: 'slug', headerName: 'Slug', flex: 1, minWidth: 120,
      renderCell: ({ value }) => <Chip label={value} size="small" sx={{ fontFamily: 'monospace', fontSize: '0.75rem' }} /> },
    { field: 'plan', headerName: 'Plan', width: 120,
      renderCell: ({ value }) => <Chip label={value ?? 'Free'} size="small" color="primary" variant="outlined" /> },
    { field: 'status', headerName: 'Status', width: 120,
      renderCell: ({ value }) => <StatusChip status={value} /> },
    { field: 'memberCount', headerName: 'Members', width: 100, type: 'number' },
    { field: 'createdAtUtc', headerName: 'Created', width: 160,
      renderCell: ({ value }) => value ? new Date(value).toLocaleDateString() : '—' },
    {
      field: 'actions', headerName: '', width: 100, sortable: false,
      renderCell: ({ row }) => (
        <Box sx={{ display: 'flex', gap: 0.5 }}>
          {row.status === 'Active' ? (
            <Tooltip title="Suspend">
              <IconButton size="small" color="warning" onClick={() => setConfirmAction({ type: 'suspend', tenant: row })}>
                <SuspendIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          ) : (
            <Tooltip title="Activate">
              <IconButton size="small" color="success" onClick={() => setConfirmAction({ type: 'activate', tenant: row })}>
                <ActivateIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          )}
        </Box>
      ),
    },
  ]

  return (
    <Box>
      <PageHeader
        title="Tenant Management"
        subtitle="Create and manage all platform tenants"
        action={
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setWizardOpen(true)}>
            New Tenant
          </Button>
        }
      />

      <Card>
        <CardContent sx={{ p: 0, '&:last-child': { pb: 0 } }}>
          <DataTable
            rows={data?.items ?? []}
            columns={columns}
            loading={isLoading}
            rowCount={data?.total ?? 0}
            paginationModel={paginationModel}
            onPaginationModelChange={setPaginationModel}
            sx={{ minHeight: 400 }}
          />
        </CardContent>
      </Card>

      <CreateTenantWizard open={wizardOpen} onClose={() => setWizardOpen(false)} />

      <ConfirmDialog
        open={!!confirmAction}
        title={confirmAction?.type === 'suspend' ? 'Suspend Tenant' : 'Activate Tenant'}
        message={
          confirmAction?.type === 'suspend'
            ? `Suspend "${confirmAction?.tenant?.name}"? Users will be unable to log in.`
            : `Activate "${confirmAction?.tenant?.name}"? Users will regain access.`
        }
        confirmColor={confirmAction?.type === 'suspend' ? 'warning' : 'success'}
        onConfirm={() => {
          if (confirmAction.type === 'suspend') suspendMutation.mutate(confirmAction.tenant.id)
          else activateMutation.mutate(confirmAction.tenant.id)
          setConfirmAction(null)
        }}
        onCancel={() => setConfirmAction(null)}
      />
    </Box>
  )
}
