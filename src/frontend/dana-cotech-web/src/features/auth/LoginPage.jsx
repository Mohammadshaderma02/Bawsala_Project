import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded';
import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded';
import AutoAwesomeRoundedIcon from '@mui/icons-material/AutoAwesomeRounded';
import EmailRoundedIcon from '@mui/icons-material/EmailRounded';
import LanguageRoundedIcon from '@mui/icons-material/LanguageRounded';
import LockRoundedIcon from '@mui/icons-material/LockRounded';
import VisibilityOffRoundedIcon from '@mui/icons-material/VisibilityOffRounded';
import VisibilityRoundedIcon from '@mui/icons-material/VisibilityRounded';
import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded';
import ExploreRoundedIcon from '@mui/icons-material/ExploreRounded';
import {
  Alert,
  Box,
  Button,
  Checkbox,
  CircularProgress,
  Container,
  FormControlLabel,
  IconButton,
  InputAdornment,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { getDirection, oppositeLanguage } from '../../i18n/languages';
import { getSession, loginAccount, saveSession } from '../platform/platformApi';
import { BrandLogo } from '../landing/BawsalaLandingPage';

const copy = {
  en: {
    eyebrow: 'Welcome back', title: 'Continue your career journey.',
    subtitle: 'Access your programs, coaching sessions, progress, and career resources in one focused space.',
    formTitle: 'Log in to your account', formSubtitle: 'Enter the details associated with your account.',
    email: 'Email address', password: 'Password', remember: 'Remember me', forgot: 'Forgot password?',
    login: 'Log in', noAccount: 'New to Bawsala?', start: 'Explore the programs',
    emailRequired: 'Enter your email address.', emailInvalid: 'Enter a valid email address.',
    passwordRequired: 'Enter your password.', integration: 'The login screen is ready. Connect the authentication API to complete sign-in.',
    back: 'Back to home', language: 'العربية', featureOne: 'Your learning progress', featureTwo: 'Upcoming sessions', featureThree: 'Personal career resources',
    createTitle: 'Create your account', createSubtitle: 'Start your personalized career journey.', name: 'Full name', create: 'Create account', existing: 'Already have an account?', switchLogin: 'Log in', nameRequired: 'Enter your name.', passwordShort: 'Use at least 8 characters.',
  },
  ar: {
    eyebrow: 'مرحباً بعودتك', title: 'أكمل رحلتك المهنية.',
    subtitle: 'ادخل إلى برامجك وجلساتك وتقدمك ومواردك المهنية في مساحة واحدة واضحة.',
    formTitle: 'تسجيل الدخول إلى حسابك', formSubtitle: 'أدخل البيانات المرتبطة بحسابك.',
    email: 'البريد الإلكتروني', password: 'كلمة المرور', remember: 'تذكرني', forgot: 'نسيت كلمة المرور؟',
    login: 'تسجيل الدخول', noAccount: 'جديد في منصة بوصلة؟', start: 'استكشف البرامج',
    emailRequired: 'أدخل بريدك الإلكتروني.', emailInvalid: 'أدخل بريداً إلكترونياً صحيحاً.',
    passwordRequired: 'أدخل كلمة المرور.', integration: 'شاشة الدخول جاهزة. اربط واجهة المصادقة لإتمام تسجيل الدخول.',
    back: 'العودة للرئيسية', language: 'English', featureOne: 'تقدمك التعليمي', featureTwo: 'جلساتك القادمة', featureThree: 'مواردك المهنية الشخصية',
    createTitle: 'أنشئ حسابك', createSubtitle: 'ابدأ رحلتك المهنية المخصصة.', name: 'الاسم الكامل', create: 'إنشاء الحساب', existing: 'لديك حساب؟', switchLogin: 'تسجيل الدخول', nameRequired: 'أدخل اسمك.', passwordShort: 'استخدم 8 أحرف على الأقل.',
  },
};

export function LoginPage({ language, content }) {
  const { i18n } = useTranslation();
  const [showPassword, setShowPassword] = useState(false);
  const mode = 'login';
  const [values, setValues] = useState({ name: '', email: '', password: '', remember: true });
  const [errors, setErrors] = useState({});
  const [apiError, setApiError] = useState('');
  const [busy, setBusy] = useState(false);
  const cms = content?.loginPage;
  const text = cms ? {
    ...copy[language], eyebrow: cms.eyebrow, title: cms.title, subtitle: cms.subtitle,
    formTitle: cms.formTitle, formSubtitle: cms.formSubtitle, email: cms.emailLabel,
    password: cms.passwordLabel, remember: cms.rememberLabel, forgot: cms.forgotLabel,
    login: cms.loginLabel, back: cms.backLabel, featureOne: cms.featureOne,
    featureTwo: cms.featureTwo, featureThree: cms.featureThree,
  } : copy[language];
  const direction = getDirection(language);

  useEffect(() => {
    const existingSession = getSession();
    if (!existingSession?.token) return;
    const returnUrl = new URLSearchParams(window.location.search).get('returnUrl');
    const defaultUrl = existingSession.role === 'Admin' ? '/cms' : '/dashboard';
    const canUseReturnUrl = returnUrl?.startsWith('/') && (!returnUrl.startsWith('/cms') || existingSession.role === 'Admin');
    window.location.replace(canUseReturnUrl ? returnUrl : defaultUrl);
  }, []);

  const submit = async (event) => {
    event.preventDefault();
    const nextErrors = {};
    if (mode === 'register' && !values.name.trim()) nextErrors.name = text.nameRequired;
    if (!values.email.trim()) nextErrors.email = text.emailRequired;
    else if (!/^\S+@\S+\.\S+$/.test(values.email)) nextErrors.email = text.emailInvalid;
    if (!values.password) nextErrors.password = text.passwordRequired;
    else if (mode === 'register' && values.password.length < 8) nextErrors.password = text.passwordShort;
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length) return;
    setBusy(true); setApiError('');
    try {
      const session = await loginAccount(values.email, values.password);
      saveSession(session);
      const returnUrl = new URLSearchParams(window.location.search).get('returnUrl');
      const defaultUrl = '/dashboard';
      const canUseReturnUrl = returnUrl?.startsWith('/') && (!returnUrl.startsWith('/cms') || session.role === 'Admin');
      window.location.href = canUseReturnUrl ? returnUrl : defaultUrl;
    } catch (requestError) {
      setApiError(requestError.response?.data?.message || requestError.message);
    } finally { setBusy(false); }
  };

  return (
    <Box dir={direction} sx={{ minHeight: '100vh', bgcolor: '#fffdf8', display: 'grid', gridTemplateColumns: { xs: '1fr', md: 'minmax(520px, 1.05fr) minmax(520px, .95fr)' } }}>
      <Box
        sx={{
          display: { xs: 'none', md: 'flex' }, position: 'relative', overflow: 'hidden', m: 2, mr: direction === 'ltr' ? 0 : 2, ml: direction === 'rtl' ? 0 : 2,
          minHeight: 'calc(100vh - 32px)', borderRadius: 6, color: '#102d5b', p: { md: 6, lg: 8 }, flexDirection: 'column', justifyContent: 'space-between',
          background: 'linear-gradient(145deg,#eef3e9 0%,#fffaf0 48%,#ebe9f3 100%)', border: '1px solid #e1e3da',
        }}
      >
        <Box sx={{ position: 'absolute', width: 600, height: 600, borderRadius: '50%', border: '1px solid rgba(16,45,91,.09)', insetInlineEnd: -250, top: -170, boxShadow: '0 0 0 70px rgba(16,45,91,.025),0 0 0 140px rgba(198,154,85,.025)' }} />
        <Box sx={{ position: 'relative' }}><BrandLogo /></Box>

        <Stack spacing={3} sx={{ position: 'relative', maxWidth: 620 }}>
          <Stack direction="row" spacing={1.2} sx={{ alignItems: 'center' }}><AutoAwesomeRoundedIcon sx={{ color: '#c69a55' }} /><Typography variant="overline" sx={{ color: '#2f4f23', fontWeight: 900, letterSpacing: '.14em' }}>{text.eyebrow}</Typography></Stack>
          <Typography variant="h2" sx={{ fontFamily: 'Georgia,serif', fontWeight: 500, fontSize: { md: 50, lg: 66 }, lineHeight: 1.03, color: '#152013' }}>{text.title}</Typography>
          <Typography sx={{ color: '#59635b', fontSize: 18, lineHeight: 1.8, maxWidth: 610 }}>{text.subtitle}</Typography>
          <Stack spacing={1.4}>
            {[text.featureOne, text.featureTwo, text.featureThree].map((feature) => <Stack key={feature} direction="row" spacing={1.2} sx={{ alignItems: 'center' }}><CheckCircleRoundedIcon sx={{ color: '#2f4f23', fontSize: 20 }} /><Typography sx={{ color: '#324038', fontWeight: 700 }}>{feature}</Typography></Stack>)}
          </Stack>
          <Paper elevation={0} sx={{ mt: 1, p: 2.2, width: 'fit-content', display: 'flex', alignItems: 'center', gap: 1.5, borderRadius: 3, bgcolor: 'rgba(255,255,255,.72)', border: '1px solid rgba(16,45,91,.09)' }}><Box sx={{ width: 42, height: 42, borderRadius: 2, display: 'grid', placeItems: 'center', bgcolor: '#102d5b', color: 'white' }}><ExploreRoundedIcon /></Box><Box><Typography variant="caption" color="text.secondary">{language === 'ar' ? 'مساحتك المهنية الخاصة' : 'Your private career space'}</Typography><Typography fontWeight={900}>{language === 'ar' ? 'آمنة، واضحة، ومصممة حولك' : 'Secure, focused, built around you'}</Typography></Box></Paper>
        </Stack>
        <Typography variant="caption" sx={{ position: 'relative', color: '#758076' }}>© {new Date().getFullYear()} Bawsala Consultancy</Typography>
      </Box>

      <Box sx={{ display: 'grid', alignItems: 'center', py: { xs: 2, md: 4 }, px: { xs: 1, sm: 3 } }}>
        <Container maxWidth="sm">
          <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: { xs: 4, md: 6 } }}>
            <Button component="a" href="/" color="inherit" startIcon={direction === 'rtl' ? <ArrowForwardRoundedIcon /> : <ArrowBackRoundedIcon />}>{text.back}</Button>
            <Button onClick={() => i18n.changeLanguage(oppositeLanguage(language))} color="inherit" startIcon={<LanguageRoundedIcon />}>{text.language}</Button>
          </Stack>

          <Paper elevation={0} sx={{ maxWidth: 510, mx: 'auto', p: { xs: 2.5, sm: 4.5 }, bgcolor: 'white', border: '1px solid #e4e4dc', borderRadius: 5, boxShadow: '0 28px 80px rgba(16,45,91,.09)' }}>
            <Box sx={{ display: { xs: 'block', md: 'none' }, mb: 4 }}><BrandLogo /></Box>
            <Typography sx={{ color: '#a47d28', fontSize: 11, fontWeight: 900, letterSpacing: '.14em', textTransform: 'uppercase', mb: 1 }}>{language === 'ar' ? 'دخول آمن إلى بوصلة' : 'Secure access to Bawsala'}</Typography>
            <Typography variant="h3" sx={{ fontFamily: 'Georgia,serif', fontSize: { xs: 34, sm: 43 }, fontWeight: 500, letterSpacing: '-.035em', color: '#152013' }}>{mode === 'register' ? text.createTitle : text.formTitle}</Typography>
            <Typography color="text.secondary" sx={{ mt: 1.5, mb: 4 }}>{mode === 'register' ? text.createSubtitle : text.formSubtitle}</Typography>

            {apiError && <Alert severity="error" sx={{ mb: 3 }}>{apiError}</Alert>}
            <Box component="form" noValidate onSubmit={submit}>
              <Stack spacing={2.2}>
                {mode === 'register' && <TextField fullWidth label={text.name} autoComplete="name" value={values.name} error={Boolean(errors.name)} helperText={errors.name} onChange={(event) => { setValues({ ...values, name: event.target.value }); setErrors({ ...errors, name: '' }); }} />}
                <TextField
                  autoFocus fullWidth label={text.email} type="email" autoComplete="email" value={values.email}
                  error={Boolean(errors.email)} helperText={errors.email}
                  onChange={(event) => { setValues({ ...values, email: event.target.value }); setErrors({ ...errors, email: '' }); setApiError(''); }}
                  slotProps={{ input: { startAdornment: <InputAdornment position="start"><EmailRoundedIcon color="action" /></InputAdornment> } }}
                />
                <TextField
                  fullWidth label={text.password} type={showPassword ? 'text' : 'password'} autoComplete="current-password" value={values.password}
                  error={Boolean(errors.password)} helperText={errors.password}
                  onChange={(event) => { setValues({ ...values, password: event.target.value }); setErrors({ ...errors, password: '' }); setApiError(''); }}
                  slotProps={{ input: {
                    startAdornment: <InputAdornment position="start"><LockRoundedIcon color="action" /></InputAdornment>,
                    endAdornment: <InputAdornment position="end"><IconButton onClick={() => setShowPassword((current) => !current)} edge="end" aria-label="Toggle password visibility">{showPassword ? <VisibilityOffRoundedIcon /> : <VisibilityRoundedIcon />}</IconButton></InputAdornment>,
                  } }}
                />
                {mode === 'login' && <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                  <FormControlLabel control={<Checkbox checked={values.remember} onChange={(event) => setValues({ ...values, remember: event.target.checked })} sx={{ color: '#9aa39a', '&.Mui-checked': { color: '#2f4f23' } }} />} label={text.remember} />
                  <Button component="a" href="/forgot-password" variant="text" size="small">{text.forgot}</Button>
                </Stack>}
                <Button type="submit" disabled={busy} variant="contained" size="large" endIcon={direction === 'rtl' ? <ArrowBackRoundedIcon /> : <ArrowForwardRoundedIcon />} sx={{ minHeight: 58, bgcolor: '#102d5b', borderRadius: 2.5, boxShadow: '0 12px 28px rgba(16,45,91,.2)', '&:hover': { bgcolor: '#0b2145', boxShadow: '0 16px 34px rgba(16,45,91,.25)' } }}>{busy ? <CircularProgress size={22} color="inherit" /> : mode === 'register' ? text.create : text.login}</Button>
                <Button component="a" href="/register" variant="outlined" size="large" sx={{ minHeight: 54, color: '#2f4f23', borderColor: '#aab5a6', borderRadius: 2.5 }}>{language === 'ar' ? 'إنشاء حساب عميل جديد' : 'Create a new client account'}</Button>
                <Stack direction="row" spacing={1} sx={{ justifyContent: 'center', alignItems: 'center', pt: .5 }}><LockRoundedIcon sx={{ fontSize: 15, color: '#899188' }} /><Typography variant="caption" color="text.secondary">{language === 'ar' ? 'بياناتك محمية ولا تتم مشاركتها' : 'Your account data is encrypted and protected'}</Typography></Stack>
              </Stack>
            </Box>
          </Paper>
        </Container>
      </Box>
    </Box>
  );
}
