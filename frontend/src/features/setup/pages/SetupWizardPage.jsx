import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Box, Card, CardContent, Typography, Stepper, Step, StepLabel,
  TextField, Button, InputAdornment, IconButton, LinearProgress, Alert
} from '@mui/material'
import {
  Shield as ShieldIcon,
  Visibility, VisibilityOff,
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

const STEPS = ['Administrator', 'Review', 'Provisioning']

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

const slideVariants = {
  enter: (dir) => ({ x: dir > 0 ? 40 : -40, opacity: 0 }),
  center: { x: 0, opacity: 1 },
  exit: (dir) => ({ x: dir > 0 ? -40 : 40, opacity: 0 }),
}

// ─── Step 1: Administrator ───────────────────────────────────────────────────
function AdminStep({ data, onNext }) {
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
        <Typography variant="h6" fontWeight={600}>Create the platform owner account</Typography>
        <Typography variant="body2" color="text.secondary" mt={0.5}>
          This account will have full administrative control over the platform.
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

      <Button type="submit" variant="contained" size="large" endIcon={<ArrowForward />} sx={{ mt: 1 }}>
        Continue
      </Button>
    </Box>
  )
}

// ─── Step 2: Review ──────────────────────────────────────────────────────────
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
            <Typography variant="overline" color="text.secondary">Platform Owner</Typography>
            <Button size="small" onClick={() => onBack(0)}>Edit</Button>
          </Box>
          <Typography variant="body1" fontWeight={600}>
            {formData.firstName} {formData.lastName}
          </Typography>
          <Typography variant="body2" color="text.secondary">{formData.email}</Typography>
        </CardContent>
      </Card>

      <Alert severity="info" sx={{ fontSize: '0.8rem' }}>
        You will be able to create tenants and manage the platform after signing in.
      </Alert>

      <Box sx={{ display: 'flex', gap: 2, mt: 1 }}>
        <Button variant="outlined" startIcon={<ArrowBack />} onClick={() => onBack(0)} sx={{ flex: 1 }}>
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

// ─── Step 3: Provisioning ────────────────────────────────────────────────────
const STATUS_MESSAGES = [
  'Initializing platform…',
  'Creating platform owner…',
  'Configuring security defaults…',
  'Finalizing setup…',
]

function ProvisioningStep({ formData, onGoBack }) {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [statusIndex, setStatusIndex] = useState(0)

  const { mutate, isPending, isSuccess, isError, error } = useMutation({
    mutationFn: (data) =>
      api.post('/setup', {
        firstName: data.firstName,
        lastName: data.lastName,
        email: data.email,
        password: data.password,
      }),
    onSuccess: () => {
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

  const handleAdminNext = (data) => {
    setFormData((d) => ({ ...d, ...data }))
    goTo(1)
  }

  const handleReviewNext = () => goTo(2)

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
        <Box sx={{ textAlign: 'center', mb: 3 }}>
          <Box sx={{ display: 'inline-flex', p: 1.5, borderRadius: 3, bgcolor: 'rgba(99,102,241,0.12)', mb: 2 }}>
            <ShieldIcon sx={{ color: 'primary.main', fontSize: 36 }} />
          </Box>
          <Typography variant="h4" fontWeight={700}>ZeroTrust SaaS</Typography>
          <Typography variant="body2" color="text.secondary" mt={0.5}>
            Platform Setup Wizard
          </Typography>
        </Box>

        <Stepper activeStep={step} sx={{ mb: 3 }}>
          {STEPS.map((label) => (
            <Step key={label}>
              <StepLabel>{label}</StepLabel>
            </Step>
          ))}
        </Stepper>

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
                  <AdminStep data={formData} onNext={handleAdminNext} />
                )}
                {step === 1 && (
                  <ReviewStep
                    formData={formData}
                    onNext={handleReviewNext}
                    onBack={(s) => handleGoBack(s)}
                  />
                )}
                {step === 2 && (
                  <ProvisioningStep
                    formData={formData}
                    onGoBack={() => handleGoBack(1)}
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
