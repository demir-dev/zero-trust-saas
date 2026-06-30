import { useState, useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box, Card, CardContent, Typography, Stepper, Step, StepLabel,
  TextField, Button, InputAdornment, IconButton, LinearProgress,
  Alert, Divider, Chip
} from '@mui/material'
import {
  Shield as ShieldIcon,
  Visibility, VisibilityOff,
  Business as BusinessIcon,
  Person as PersonIcon,
  CheckCircle as CheckCircleIcon,
  Error as ErrorIcon,
  ArrowBack, ArrowForward
} from '@mui/icons-material'
import { motion, AnimatePresence } from 'framer-motion'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import api from '../../../shared/api/axiosInstance'
import ProblemAlert from '../../../shared/components/ProblemAlert'

const STEPS = ['Organization', 'Administrator', 'Review', 'Provisioning']

const orgSchema = z.object({
  organizationName: z.string().min(2, 'At least 2 characters').max(100, 'Max 100 characters'),
  organizationSlug: z
    .string()
    .regex(
      /^[a-z0-9][a-z0-9-]{1,48}[a-z0-9]$/,
      '3–50 chars, lowercase letters, numbers, hyphens only, cannot start/end with hyphen'
    ),
})

const adminSchema = z
  .object({
    firstName: z.string().min(1, 'First name is required').max(100),
    lastName: z.string().min(1, 'Last name is required').max(100),
    email: z.string().email('Invalid email address'),
    password: z
      .string()
      .min(8, 'At least 8 characters')
      .regex(/[A-Z]/, 'At least one uppercase letter')
      .regex(/[0-9]/, 'At least one number')
      .regex(/[^A-Za-z0-9]/, 'At least one special character'),
    confirmPassword: z.string(),
  })
  .refine((d) => d.password === d.confirmPassword, {
    message: 'Passwords do not match',
    path: ['confirmPassword'],
  })

function slugify(name) {
  return name
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 50)
}

const slideVariants = {
  enter: (dir) => ({ x: dir > 0 ? 40 : -40, opacity: 0 }),
  center: { x: 0, opacity: 1 },
  exit: (dir) => ({ x: dir > 0 ? -40 : 40, opacity: 0 }),
}

// ─── Step 1: Organization ───────────────────────────────────────────────────
function OrgStep({ data, onNext }) {
  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm({
    resolver: zodResolver(orgSchema),
    defaultValues: data,
  })

  const nameValue = watch('organizationName', '')
  const slugRef = useRef(false)

  useEffect(() => {
    if (!slugRef.current && nameValue) {
      setValue('organizationSlug', slugify(nameValue), { shouldValidate: false })
    }
  }, [nameValue, setValue])

  return (
    <Box component="form" onSubmit={handleSubmit(onNext)} sx={{ display: 'flex', flexDirection: 'column', gap: 2.5 }}>
      <Box>
        <Typography variant="h6" fontWeight={600}>Set up your organization</Typography>
        <Typography variant="body2" color="text.secondary" mt={0.5}>
          This is how your team will identify your workspace.
        </Typography>
      </Box>

      <TextField
        label="Organization Name"
        placeholder="Acme Corporation"
        {...register('organizationName')}
        error={!!errors.organizationName}
        helperText={errors.organizationName?.message}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <BusinessIcon sx={{ color: 'text.secondary', fontSize: 18 }} />
            </InputAdornment>
          ),
        }}
      />

      <TextField
        label="Organization Slug"
        placeholder="acme-corp"
        {...register('organizationSlug', {
          onChange: () => { slugRef.current = true },
        })}
        error={!!errors.organizationSlug}
        helperText={errors.organizationSlug?.message ?? 'Used to identify your organization (e.g. acme-corp.zerotrust.com)'}
      />

      <Button type="submit" variant="contained" size="large" endIcon={<ArrowForward />} sx={{ mt: 1 }}>
        Continue
      </Button>
    </Box>
  )
}

// ─── Step 2: Administrator ───────────────────────────────────────────────────
function AdminStep({ data, onNext, onBack }) {
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirm, setShowConfirm] = useState(false)
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm({ resolver: zodResolver(adminSchema), defaultValues: data })

  return (
    <Box component="form" onSubmit={handleSubmit(onNext)} sx={{ display: 'flex', flexDirection: 'column', gap: 2.5 }}>
      <Box>
        <Typography variant="h6" fontWeight={600}>Create the administrator account</Typography>
        <Typography variant="body2" color="text.secondary" mt={0.5}>
          This account will have full Owner access to your organization.
        </Typography>
      </Box>

      <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 2 }}>
        <TextField
          label="First Name"
          {...register('firstName')}
          error={!!errors.firstName}
          helperText={errors.firstName?.message}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <PersonIcon sx={{ color: 'text.secondary', fontSize: 18 }} />
              </InputAdornment>
            ),
          }}
        />
        <TextField
          label="Last Name"
          {...register('lastName')}
          error={!!errors.lastName}
          helperText={errors.lastName?.message}
        />
      </Box>

      <TextField
        label="Email"
        type="email"
        {...register('email')}
        error={!!errors.email}
        helperText={errors.email?.message}
      />

      <TextField
        label="Password"
        type={showPassword ? 'text' : 'password'}
        {...register('password')}
        error={!!errors.password}
        helperText={errors.password?.message ?? 'Min 8 chars, uppercase, number, special character'}
        InputProps={{
          endAdornment: (
            <InputAdornment position="end">
              <IconButton size="small" onClick={() => setShowPassword((p) => !p)}>
                {showPassword ? <VisibilityOff fontSize="small" /> : <Visibility fontSize="small" />}
              </IconButton>
            </InputAdornment>
          ),
        }}
      />

      <TextField
        label="Confirm Password"
        type={showConfirm ? 'text' : 'password'}
        {...register('confirmPassword')}
        error={!!errors.confirmPassword}
        helperText={errors.confirmPassword?.message}
        InputProps={{
          endAdornment: (
            <InputAdornment position="end">
              <IconButton size="small" onClick={() => setShowConfirm((p) => !p)}>
                {showConfirm ? <VisibilityOff fontSize="small" /> : <Visibility fontSize="small" />}
              </IconButton>
            </InputAdornment>
          ),
        }}
      />

      <Box sx={{ display: 'flex', gap: 2, mt: 1 }}>
        <Button variant="outlined" startIcon={<ArrowBack />} onClick={onBack} sx={{ flex: 1 }}>
          Back
        </Button>
        <Button type="submit" variant="contained" size="large" endIcon={<ArrowForward />} sx={{ flex: 2 }}>
          Continue
        </Button>
      </Box>
    </Box>
  )
}

// ─── Step 3: Review ──────────────────────────────────────────────────────────
function ReviewStep({ formData, onNext, onBack }) {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2.5 }}>
      <Box>
        <Typography variant="h6" fontWeight={600}>Review your configuration</Typography>
        <Typography variant="body2" color="text.secondary" mt={0.5}>
          Please confirm the details before initializing the platform.
        </Typography>
      </Box>

      <Card variant="outlined" sx={{ bgcolor: 'background.default' }}>
        <CardContent>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
            <Typography variant="overline" color="text.secondary">Organization</Typography>
            <Button size="small" onClick={() => onBack(0)}>Edit</Button>
          </Box>
          <Typography variant="body1" fontWeight={600}>{formData.organizationName}</Typography>
          <Chip
            label={formData.organizationSlug}
            size="small"
            icon={<BusinessIcon />}
            sx={{ mt: 0.5, fontSize: '0.75rem' }}
          />
        </CardContent>
      </Card>

      <Card variant="outlined" sx={{ bgcolor: 'background.default' }}>
        <CardContent>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
            <Typography variant="overline" color="text.secondary">Administrator</Typography>
            <Button size="small" onClick={() => onBack(1)}>Edit</Button>
          </Box>
          <Typography variant="body1" fontWeight={600}>
            {formData.firstName} {formData.lastName}
          </Typography>
          <Typography variant="body2" color="text.secondary">{formData.email}</Typography>
          <Chip label="Owner" size="small" color="primary" sx={{ mt: 0.5 }} />
        </CardContent>
      </Card>

      <Box sx={{ display: 'flex', gap: 2, mt: 1 }}>
        <Button variant="outlined" startIcon={<ArrowBack />} onClick={() => onBack(1)} sx={{ flex: 1 }}>
          Back
        </Button>
        <Button
          variant="contained"
          size="large"
          startIcon={<ShieldIcon />}
          onClick={onNext}
          sx={{ flex: 2 }}
        >
          Initialize Platform
        </Button>
      </Box>
    </Box>
  )
}

// ─── Step 4: Provisioning ────────────────────────────────────────────────────
const STATUS_MESSAGES = [
  'Creating organization…',
  'Seeding permission registry…',
  'Provisioning roles…',
  'Configuring security defaults…',
  'Activating tenant…',
]

function ProvisioningStep({ formData, onGoBack }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [statusIndex, setStatusIndex] = useState(0)

  const { mutate, isPending, isSuccess, isError, error } = useMutation({
    mutationFn: (data) =>
      api.post('/platform/initialize', {
        organizationName: data.organizationName,
        organizationSlug: data.organizationSlug,
        adminFirstName: data.firstName,
        adminLastName: data.lastName,
        adminEmail: data.email,
        adminPassword: data.password,
      }),
    onSuccess: () => {
      // Invalidate platform status so PlatformStatusProvider re-fetches
      queryClient.invalidateQueries({ queryKey: ['platform', 'status'] })
      setTimeout(() => {
        navigate('/login', {
          state: { message: 'Platform initialized successfully. You can now sign in.' },
        })
      }, 2000)
    },
  })

  useEffect(() => {
    mutate(formData)
  }, []) // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    if (!isPending) return
    const timer = setInterval(() => {
      setStatusIndex((i) => (i + 1) % STATUS_MESSAGES.length)
    }, 900)
    return () => clearInterval(timer)
  }, [isPending])

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3, alignItems: 'center', py: 2 }}>
      <AnimatePresence mode="wait">
        {isPending && (
          <motion.div
            key="loading"
            initial={{ opacity: 0, scale: 0.9 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0 }}
            style={{ width: '100%', textAlign: 'center' }}
          >
            <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2 }}>
              <Box sx={{ display: 'inline-flex', p: 2, borderRadius: '50%', bgcolor: 'rgba(99,102,241,0.12)' }}>
                <motion.div
                  animate={{ rotate: 360 }}
                  transition={{ duration: 2, repeat: Infinity, ease: 'linear' }}
                >
                  <ShieldIcon sx={{ color: 'primary.main', fontSize: 40 }} />
                </motion.div>
              </Box>
              <Typography variant="h6" fontWeight={600}>Initializing platform…</Typography>
              <Typography variant="body2" color="text.secondary">
                {STATUS_MESSAGES[statusIndex]}
              </Typography>
              <LinearProgress sx={{ width: '100%', mt: 1 }} />
            </Box>
          </motion.div>
        )}

        {isSuccess && (
          <motion.div
            key="success"
            initial={{ opacity: 0, scale: 0.8 }}
            animate={{ opacity: 1, scale: 1 }}
            transition={{ type: 'spring', stiffness: 200 }}
            style={{ textAlign: 'center' }}
          >
            <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 2 }}>
              <CheckCircleIcon sx={{ color: 'success.main', fontSize: 56 }} />
              <Typography variant="h6" fontWeight={600} color="success.main">
                Platform initialized!
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Redirecting to sign in…
              </Typography>
            </Box>
          </motion.div>
        )}

        {isError && (
          <motion.div
            key="error"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            style={{ width: '100%' }}
          >
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <ErrorIcon color="error" />
                <Typography variant="h6" fontWeight={600} color="error">
                  Initialization failed
                </Typography>
              </Box>
              <ProblemAlert error={error?.response?.data} />
              <Button variant="outlined" startIcon={<ArrowBack />} onClick={onGoBack}>
                Go Back and Try Again
              </Button>
            </Box>
          </motion.div>
        )}
      </AnimatePresence>
    </Box>
  )
}

// ─── Main Wizard ─────────────────────────────────────────────────────────────
export default function SetupWizardPage() {
  const [step, setStep] = useState(0)
  const [direction, setDirection] = useState(1)
  const [formData, setFormData] = useState({
    organizationName: '',
    organizationSlug: '',
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    confirmPassword: '',
  })

  const goTo = (nextStep) => {
    setDirection(nextStep > step ? 1 : -1)
    setStep(nextStep)
  }

  const handleOrgNext = (data) => {
    setFormData((d) => ({ ...d, ...data }))
    goTo(1)
  }

  const handleAdminNext = (data) => {
    setFormData((d) => ({ ...d, ...data }))
    goTo(2)
  }

  const handleReviewNext = () => goTo(3)

  const handleGoBack = (targetStep) => goTo(targetStep ?? step - 1)

  return (
    <Box
      sx={{
        minHeight: '100vh',
        bgcolor: 'background.default',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        p: 2,
        background: 'radial-gradient(ellipse at 50% 0%, rgba(99,102,241,0.12) 0%, transparent 70%)',
      }}
    >
      <Box sx={{ width: '100%', maxWidth: 520 }}>
        {/* Header */}
        <Box sx={{ textAlign: 'center', mb: 3 }}>
          <Box sx={{ display: 'inline-flex', p: 1.5, borderRadius: 3, bgcolor: 'rgba(99,102,241,0.12)', mb: 2 }}>
            <ShieldIcon sx={{ color: 'primary.main', fontSize: 36 }} />
          </Box>
          <Typography variant="h4" fontWeight={700}>ZeroTrust SaaS</Typography>
          <Typography variant="body2" color="text.secondary" mt={0.5}>
            Platform Setup Wizard
          </Typography>
        </Box>

        {/* Stepper */}
        <Stepper activeStep={step} sx={{ mb: 3 }}>
          {STEPS.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>

        {/* Card */}
        <Card>
          <CardContent sx={{ p: 3 }}>
            <AnimatePresence mode="wait" custom={direction}>
              <motion.div
                key={step}
                custom={direction}
                variants={slideVariants}
                initial="enter"
                animate="center"
                exit="exit"
                transition={{ duration: 0.25, ease: 'easeInOut' }}
              >
                {step === 0 && (
                  <OrgStep data={formData} onNext={handleOrgNext} />
                )}
                {step === 1 && (
                  <AdminStep data={formData} onNext={handleAdminNext} onBack={() => handleGoBack(0)} />
                )}
                {step === 2 && (
                  <ReviewStep
                    formData={formData}
                    onNext={handleReviewNext}
                    onBack={(s) => handleGoBack(s)}
                  />
                )}
                {step === 3 && (
                  <ProvisioningStep
                    formData={formData}
                    onGoBack={() => handleGoBack(2)}
                  />
                )}
              </motion.div>
            </AnimatePresence>
          </CardContent>
        </Card>

        <Typography variant="caption" color="text.disabled" sx={{ display: 'block', textAlign: 'center', mt: 2 }}>
          Step {step + 1} of {STEPS.length}
        </Typography>
      </Box>
    </Box>
  )
}
