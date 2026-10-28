import { CacheProvider } from '@emotion/react';
import CssBaseline from '@mui/material/CssBaseline';
import { ThemeProvider } from '@mui/material/styles';
import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { BawsalaLandingPage } from '../features/landing/BawsalaLandingPage';
import { CmsPage } from '../features/cms/CmsPage';
import { LoginPage } from '../features/auth/LoginPage';
import { PasswordRecoveryPage } from '../features/auth/PasswordRecoveryPage';
import { RegistrationPage } from '../features/auth/RegistrationPage';
import { JourneyPortal } from '../features/platform/JourneyPortal';
import { getSession } from '../features/platform/platformApi';
import { getDirection, isSupportedLanguage } from '../i18n/languages';
import { createEmotionCache } from '../theme/createEmotionCache';
import { createAppTheme } from '../theme/theme';
import { getLandingContent } from '../features/landing/landingApi';

export function App() {
  const { i18n } = useTranslation();
  const language = isSupportedLanguage(i18n.language) ? i18n.language : 'en';
  const direction = getDirection(language);
  const [siteContent, setSiteContent] = useState(null);

  useEffect(() => {
    let active = true;
    getLandingContent(language).then((content) => { if (active) setSiteContent(content); }).catch(() => {});
    const update = (event) => { if (event.detail?.language === language) setSiteContent(event.detail.content); };
    window.addEventListener('site-content-updated', update);
    return () => { active = false; window.removeEventListener('site-content-updated', update); };
  }, [language]);

  const cache = useMemo(() => createEmotionCache(direction), [direction]);
  const theme = useMemo(() => createAppTheme(direction, siteContent?.appearance?.theme), [direction, siteContent?.appearance?.theme]);
  const isCms = window.location.pathname === '/cms' || window.location.pathname.startsWith('/cms/');
  const isLogin = window.location.pathname === '/login' || window.location.pathname.startsWith('/login/');
  const isPasswordRecovery = ['/forgot-password', '/reset-password'].some((path) => window.location.pathname === path || window.location.pathname.startsWith(`${path}/`));
  const isRegistration = ['/register', '/verify-email'].some((path) => window.location.pathname === path || window.location.pathname.startsWith(`${path}/`));
  const isJourney = ['/programs', '/dashboard', '/checkout', '/learn', '/booking', '/assessment', '/career-profile-print'].some((path) => window.location.pathname === path || window.location.pathname.startsWith(`${path}/`));
  const isAdminDashboard = window.location.pathname === '/dashboard' && getSession()?.role === 'Admin';

  return (
    <CacheProvider value={cache}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        {isCms || isAdminDashboard ? <CmsPage /> : isLogin ? <LoginPage language={language} content={siteContent} /> : isRegistration ? <RegistrationPage language={language} /> : isPasswordRecovery ? <PasswordRecoveryPage language={language} /> : isJourney ? <JourneyPortal language={language} /> : <BawsalaLandingPage language={language} />}
      </ThemeProvider>
    </CacheProvider>
  );
}
