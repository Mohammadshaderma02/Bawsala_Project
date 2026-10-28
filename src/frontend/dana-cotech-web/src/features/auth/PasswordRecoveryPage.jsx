import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded';
import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded';
import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded';
import EmailRoundedIcon from '@mui/icons-material/EmailRounded';
import KeyRoundedIcon from '@mui/icons-material/KeyRounded';
import LanguageRoundedIcon from '@mui/icons-material/LanguageRounded';
import LockRoundedIcon from '@mui/icons-material/LockRounded';
import { Alert, Box, Button, CircularProgress, Container, InputAdornment, Paper, Stack, TextField, Typography } from '@mui/material';
import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { getDirection, oppositeLanguage } from '../../i18n/languages';
import { requestPasswordReset, resetPassword } from '../platform/platformApi';

const copy = {
  en: {
    back: 'Back to log in', language: 'العربية', eyebrow: 'Account recovery',
    forgotTitle: 'Reset your password', forgotBody: 'Enter your email and we’ll send you a secure link that expires in 30 minutes.',
    resetTitle: 'Choose a new password', resetBody: 'Create a strong password you have not used before.',
    email: 'Email address', password: 'New password', confirm: 'Confirm new password', send: 'Send reset link', save: 'Update password',
    emailRequired: 'Enter a valid email address.', passwordShort: 'Use at least 8 characters.', mismatch: 'Passwords do not match.',
    sent: 'If an account exists for this email, a password reset link has been sent.', changed: 'Your password has been changed successfully.',
    login: 'Continue to log in', devLink: 'Open development reset link', missingToken: 'This reset link is missing its security token.',
    privacy: 'For your privacy, we never reveal whether an email is registered.',
  },
  ar: {
    back: 'العودة لتسجيل الدخول', language: 'English', eyebrow: 'استعادة الحساب',
    forgotTitle: 'إعادة تعيين كلمة المرور', forgotBody: 'أدخل بريدك الإلكتروني وسنرسل لك رابطاً آمناً صالحاً لمدة 30 دقيقة.',
    resetTitle: 'اختر كلمة مرور جديدة', resetBody: 'أنشئ كلمة مرور قوية لم تستخدمها سابقاً.',
    email: 'البريد الإلكتروني', password: 'كلمة المرور الجديدة', confirm: 'تأكيد كلمة المرور', send: 'إرسال رابط الاستعادة', save: 'تحديث كلمة المرور',
    emailRequired: 'أدخل بريداً إلكترونياً صحيحاً.', passwordShort: 'استخدم 8 أحرف على الأقل.', mismatch: 'كلمتا المرور غير متطابقتين.',
    sent: 'إذا كان هناك حساب مرتبط بهذا البريد، فقد تم إرسال رابط الاستعادة.', changed: 'تم تغيير كلمة المرور بنجاح.',
    login: 'المتابعة لتسجيل الدخول', devLink: 'فتح رابط الاستعادة التجريبي', missingToken: 'رابط الاستعادة لا يحتوي على رمز الأمان.',
    privacy: 'لحماية خصوصيتك، لا نكشف إن كان البريد مسجلاً أم لا.',
  },
};

export function PasswordRecoveryPage({ language }) {
  const { i18n } = useTranslation();
  const text = copy[language] ?? copy.en;
  const direction = getDirection(language);
  const isReset = window.location.pathname.startsWith('/reset-password');
  const token = new URLSearchParams(window.location.search).get('token') ?? '';
  const [values, setValues] = useState({ email: '', password: '', confirm: '' });
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [developmentUrl, setDevelopmentUrl] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (event) => {
    event.preventDefault(); setError(''); setSuccess('');
    if (!isReset && !/^\S+@\S+\.\S+$/.test(values.email)) return setError(text.emailRequired);
    if (isReset && !token) return setError(text.missingToken);
    if (isReset && values.password.length < 8) return setError(text.passwordShort);
    if (isReset && values.password !== values.confirm) return setError(text.mismatch);
    setBusy(true);
    try {
      if (isReset) {
        await resetPassword(token, values.password);
        setSuccess(text.changed);
      } else {
        const result = await requestPasswordReset(values.email);
        setDevelopmentUrl(result?.resetUrl ?? '');
        setSuccess(text.sent);
      }
    } catch (requestError) {
      setError(requestError.response?.data?.message || requestError.message);
    } finally { setBusy(false); }
  };

  const nextIcon = direction === 'rtl' ? <ArrowBackRoundedIcon /> : <ArrowForwardRoundedIcon />;
  return (
    <Box dir={direction} sx={{ minHeight: '100vh', bgcolor: 'brand.navy', display: 'grid', placeItems: 'center', position: 'relative', overflow: 'hidden', py: 4 }}>
      <Box sx={{ position: 'absolute', inset: 0, background: 'radial-gradient(circle at 12% 16%, rgba(184,207,124,.22), transparent 32%), radial-gradient(circle at 88% 84%, rgba(216,189,116,.2), transparent 30%)' }} />
      <Box sx={{ position: 'absolute', width: 600, height: 600, borderRadius: '50%', border: '1px solid rgba(255,255,255,.08)', right: -250, top: -220 }} />
      <Container maxWidth="sm" sx={{ position: 'relative' }}>
        <Stack direction="row" justifyContent="space-between" sx={{ mb: 2 }}>
          <Button component="a" href="/login" sx={{ color: 'rgba(255,255,255,.78)' }} startIcon={direction === 'rtl' ? <ArrowForwardRoundedIcon /> : <ArrowBackRoundedIcon />}>{text.back}</Button>
          <Button onClick={() => i18n.changeLanguage(oppositeLanguage(language))} sx={{ color: 'rgba(255,255,255,.78)' }} startIcon={<LanguageRoundedIcon />}>{text.language}</Button>
        </Stack>
        <Paper elevation={24} sx={{ p: { xs: 3, sm: 5 }, borderRadius: 5 }}>
          <Box component="img" src="/assets/bawsala-logo.jpg" alt="Bawsala" sx={{ width: 170, height: 90, objectFit: 'cover', objectPosition: 'center 66%', mb: 4 }} />
          <Stack direction="row" spacing={1} alignItems="center" sx={{ color: 'primary.main', mb: 1.5 }}><KeyRoundedIcon /><Typography variant="overline" fontWeight={900}>{text.eyebrow}</Typography></Stack>
          <Typography variant="h3" sx={{ fontSize: { xs: 31, sm: 40 }, fontWeight: 900, letterSpacing: '-.035em' }}>{isReset ? text.resetTitle : text.forgotTitle}</Typography>
          <Typography color="text.secondary" sx={{ mt: 1.5, mb: 4, lineHeight: 1.8 }}>{isReset ? text.resetBody : text.forgotBody}</Typography>
          {error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}
          {success && <Alert icon={<CheckCircleRoundedIcon />} severity="success" sx={{ mb: 3 }}>{success}</Alert>}
          {!success ? <Box component="form" onSubmit={submit} noValidate><Stack spacing={2.2}>
            {!isReset ? <TextField autoFocus label={text.email} type="email" autoComplete="email" value={values.email} onChange={(event) => setValues({ ...values, email: event.target.value })} slotProps={{ input: { startAdornment: <InputAdornment position="start"><EmailRoundedIcon color="action" /></InputAdornment> } }} /> : <>
              <TextField autoFocus label={text.password} type="password" autoComplete="new-password" value={values.password} onChange={(event) => setValues({ ...values, password: event.target.value })} slotProps={{ input: { startAdornment: <InputAdornment position="start"><LockRoundedIcon color="action" /></InputAdornment> } }} />
              <TextField label={text.confirm} type="password" autoComplete="new-password" value={values.confirm} onChange={(event) => setValues({ ...values, confirm: event.target.value })} slotProps={{ input: { startAdornment: <InputAdornment position="start"><LockRoundedIcon color="action" /></InputAdornment> } }} />
            </>}
            <Button type="submit" disabled={busy} variant="contained" size="large" endIcon={nextIcon} sx={{ minHeight: 56, bgcolor: 'brand.navy' }}>{busy ? <CircularProgress size={22} color="inherit" /> : isReset ? text.save : text.send}</Button>
            {!isReset && <Typography variant="caption" color="text.secondary" textAlign="center">{text.privacy}</Typography>}
          </Stack></Box> : <Stack spacing={1.5}>
            {developmentUrl && <Button component="a" href={developmentUrl} variant="outlined" endIcon={nextIcon}>{text.devLink}</Button>}
            {(isReset || !developmentUrl) && <Button component="a" href="/login" variant="contained" endIcon={nextIcon} sx={{ bgcolor: 'brand.navy' }}>{text.login}</Button>}
          </Stack>}
        </Paper>
      </Container>
    </Box>
  );
}
