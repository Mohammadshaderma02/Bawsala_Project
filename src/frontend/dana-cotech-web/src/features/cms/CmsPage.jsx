import AddRoundedIcon from '@mui/icons-material/AddRounded';
import ArrowBackRoundedIcon from '@mui/icons-material/ArrowBackRounded';
import DeleteOutlineRoundedIcon from '@mui/icons-material/DeleteOutlineRounded';
import ExpandMoreRoundedIcon from '@mui/icons-material/ExpandMoreRounded';
import PublishRoundedIcon from '@mui/icons-material/PublishRounded';
import DashboardRoundedIcon from '@mui/icons-material/DashboardRounded';
import ArticleRoundedIcon from '@mui/icons-material/ArticleRounded';
import PeopleRoundedIcon from '@mui/icons-material/PeopleRounded';
import MenuRoundedIcon from '@mui/icons-material/MenuRounded';
import LogoutRoundedIcon from '@mui/icons-material/LogoutRounded';
import HomeRoundedIcon from '@mui/icons-material/HomeRounded';
import LanguageRoundedIcon from '@mui/icons-material/LanguageRounded';
import PaletteRoundedIcon from '@mui/icons-material/PaletteRounded';
import MenuBookRoundedIcon from '@mui/icons-material/MenuBookRounded';
import PaymentsRoundedIcon from '@mui/icons-material/PaymentsRounded';
import WorkspacePremiumRoundedIcon from '@mui/icons-material/WorkspacePremiumRounded';
import PreviewRoundedIcon from '@mui/icons-material/PreviewRounded';
import ChevronRightRoundedIcon from '@mui/icons-material/ChevronRightRounded';
import SearchRoundedIcon from '@mui/icons-material/SearchRounded';
import TrendingUpRoundedIcon from '@mui/icons-material/TrendingUpRounded';
import EventAvailableRoundedIcon from '@mui/icons-material/EventAvailableRounded';
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  AppBar,
  Box,
  Button,
  Chip,
  CircularProgress,
  Container,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Divider,
  Drawer,
  Grid,
  IconButton,
  MenuItem,
  List,
  ListItemButton,
  ListItemIcon,
  ListItemText,
  LinearProgress,
  Paper,
  Snackbar,
  Stack,
  Tab,
  Tabs,
  TextField,
  Switch,
  Toolbar,
  Typography,
} from '@mui/material';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { getCmsLandingContent, saveCmsLandingContent, uploadCmsAsset } from './cmsApi';
import { clearSession, createPlatformSession, createUser, getAdminPayments, getAdminSessions, getAdminUsers, getCourseStudents, getProgramClients, getSession, refundAdminPayment, reviewCareerProfile, updateUserRole } from '../platform/platformApi';
import { CareerClarityAssetsManager } from './CareerClarityAssetsManager';

const SIDEBAR_WIDTH = 272;
const DASHBOARD_COPY = {
  en: { dashboard: 'Admin Dashboard', welcome: 'Welcome', overview: 'Overview', content: 'Website content', courses: 'Courses', sessions: 'Sessions', theme: 'Theme & Colors', themeHint: 'Control the global project colors, corner style, and homepage section visibility.', users: 'Users', totalUsers: 'Total users', clients: 'Clients', admins: 'Administrators', quick: 'Quick actions', editWebsite: 'Edit website', manageUsers: 'Manage users', viewWebsite: 'View website', createUser: 'Create user', createHint: 'Only administrators can create client or admin accounts.', fullName: 'Full name', email: 'Email', password: 'Temporary password', role: 'Role', create: 'Create', allUsers: 'All users', joined: 'Joined', publish: 'Publish', publishing: 'Publishing', changes: 'Unpublished changes', logout: 'Log out', overviewHint: 'A quick view of your platform administration workspace.', english: 'English', arabic: 'العربية' },
  ar: { dashboard: 'لوحة تحكم الإدارة', welcome: 'مرحباً', overview: 'نظرة عامة', content: 'محتوى الموقع', courses: 'الدورات', sessions: 'الجلسات', theme: 'المظهر والألوان', themeHint: 'تحكم بألوان المشروع، شكل الحواف، وظهور أقسام الصفحة الرئيسية.', users: 'المستخدمون', totalUsers: 'إجمالي المستخدمين', clients: 'العملاء', admins: 'المسؤولون', quick: 'إجراءات سريعة', editWebsite: 'تعديل الموقع', manageUsers: 'إدارة المستخدمين', viewWebsite: 'عرض الموقع', createUser: 'إنشاء مستخدم', createHint: 'يمكن للمسؤولين فقط إنشاء حساب عميل أو مسؤول.', fullName: 'الاسم الكامل', email: 'البريد الإلكتروني', password: 'كلمة مرور مؤقتة', role: 'الصلاحية', create: 'إنشاء', allUsers: 'جميع المستخدمين', joined: 'تاريخ الانضمام', publish: 'نشر', publishing: 'جارٍ النشر', changes: 'تغييرات غير منشورة', logout: 'تسجيل الخروج', overviewHint: 'نظرة سريعة على المنصة ومساحة الإدارة.', english: 'English', arabic: 'العربية' },
};
DASHBOARD_COPY.en.payments = 'Payments';
DASHBOARD_COPY.ar.payments = 'الدفعات';
DASHBOARD_COPY.en.programClients = 'Assessment Results';
DASHBOARD_COPY.ar.programClients = 'نتائج التقييمات';
DASHBOARD_COPY.en.clarityAssets = 'Assessments & Media';
DASHBOARD_COPY.ar.clarityAssets = 'التقييمات والفيديوهات';

const SECTION_LABELS = {
  hero: 'Hero',
  featuredOffer: 'Featured offer',
  masterClass: 'Masterclass',
  programs: 'Programs',
  whyDana: 'Why Dana',
  dashboardModules: 'Dashboard modules',
  about: 'About Dana',
  testimonials: 'Testimonials',
  leadCapture: 'Lead capture',
  uiText: 'Navigation & section labels',
  appearance: 'Project theme & home visibility',
  loginPage: 'Login page',
  courseOffers: 'Course checkout offers',
  paymentTerms: 'Payment terms & conditions',
  stories: 'Video stories',
  customHtmlSections: 'Custom sections (HTML, image, video)',
};

export function CmsPage() {
  const { i18n } = useTranslation();
  const dashboardLanguage = i18n.language?.startsWith('ar') ? 'ar' : 'en';
  const d = DASHBOARD_COPY[dashboardLanguage];
  const dashboardDirection = dashboardLanguage === 'ar' ? 'rtl' : 'ltr';
  const session = getSession();
  const [language, setLanguage] = useState('en');
  const [content, setContent] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [dirty, setDirty] = useState(false);
  const [newUser, setNewUser] = useState({ name: '', email: '', password: '', role: 'Client' });
  const [creatingUser, setCreatingUser] = useState(false);
  const [activeView, setActiveView] = useState('overview');
  const [users, setUsers] = useState([]);
  const [loadingUsers, setLoadingUsers] = useState(false);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [selectedContentSection, setSelectedContentSection] = useState('hero');

  if (!session || session.role !== 'Admin') {
    window.location.replace(`/login?returnUrl=${encodeURIComponent(window.location.pathname)}`);
    return null;
  }

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    getCmsLandingContent(language)
      .then((data) => { if (active) { setContent(data); setDirty(false); } })
      .catch((requestError) => { if (active) setError(requestError.message); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [language]);

  const loadUsers = async () => {
    setLoadingUsers(true);
    try {
      setUsers(await getAdminUsers());
    } catch (requestError) {
      setError(requestError.response?.data?.message || requestError.message);
    } finally {
      setLoadingUsers(false);
    }
  };

  useEffect(() => { loadUsers(); }, []);

  const updateSection = (key, value) => {
    setContent((current) => ({ ...current, [key]: value }));
    setDirty(true);
  };

  const publish = async () => {
    setSaving(true);
    setError('');
    try {
      const saved = await saveCmsLandingContent(language, content);
      setContent(saved);
      window.dispatchEvent(new CustomEvent('site-content-updated', { detail: { language, content: saved } }));
      setDirty(false);
      setNotice(`${language.toUpperCase()} homepage published successfully.`);
    } catch (requestError) {
      setError(requestError.response?.data?.message || requestError.message);
    } finally {
      setSaving(false);
    }
  };

  const submitNewUser = async (event) => {
    event.preventDefault();
    setCreatingUser(true);
    setError('');
    try {
      const user = await createUser(newUser.name, newUser.email, newUser.password, newUser.role);
      setNewUser({ name: '', email: '', password: '', role: 'Client' });
      await loadUsers();
      setNotice(`${user.name} created as ${user.role}.`);
    } catch (requestError) {
      setError(requestError.response?.data?.message || requestError.message);
    } finally {
      setCreatingUser(false);
    }
  };

  const changeRole = async (userId, role) => {
    setError('');
    try {
      const updated = await updateUserRole(userId, role);
      setUsers((current) => current.map((user) => user.userId === userId ? updated : user));
      setNotice(`${updated.name} is now ${updated.role}.`);
    } catch (requestError) {
      setError(requestError.response?.data?.message || requestError.message);
    }
  };
  const exportUsers = () => { const rows = [['Name','Email','Role','Joined'], ...users.map((user) => [user.name,user.email,user.role,user.createdAtUtc])]; const csv=rows.map((row)=>row.map((value)=>`"${String(value??'').replaceAll('"','""')}"`).join(',')).join('\r\n'); const url=URL.createObjectURL(new Blob(['\ufeff'+csv],{type:'text/csv;charset=utf-8'})); const link=document.createElement('a');link.href=url;link.download='users.csv';link.click();URL.revokeObjectURL(url); };

  return (
    <Box dir={dashboardDirection} sx={{ minHeight: '100vh', bgcolor: '#f4f5f7' }}>
      <AppBar position="fixed" elevation={0} sx={{ width: { xs: '100%', md: `calc(100% - ${SIDEBAR_WIDTH}px)` }, left: { xs: 0, md: dashboardDirection === 'rtl' ? 0 : `${SIDEBAR_WIDTH}px` }, right: { xs: 0, md: dashboardDirection === 'rtl' ? `${SIDEBAR_WIDTH}px` : 0 }, bgcolor: 'rgba(255,255,255,.92)', color: 'text.primary', borderBottom: '1px solid', borderColor: 'divider', backdropFilter: 'blur(16px)' }}>
        <Container maxWidth={false}>
          <Toolbar disableGutters sx={{ gap: 2, minHeight: 76 }}>
            <IconButton onClick={() => setMobileMenuOpen(true)} aria-label="Open menu" sx={{ display: { md: 'none' } }}><MenuRoundedIcon /></IconButton>
            <Box sx={{ flexGrow: 1 }}>
              <Typography fontWeight={900}>{d.dashboard}</Typography>
              <Typography variant="caption" color="text.secondary">{d.welcome}, {session.name}</Typography>
            </Box>
            <Button color="inherit" startIcon={<LanguageRoundedIcon />} onClick={() => i18n.changeLanguage(dashboardLanguage === 'ar' ? 'en' : 'ar')} sx={{ minWidth: 0 }}>{dashboardLanguage === 'ar' ? 'EN' : 'AR'}</Button>
            {['content', 'theme', 'courses', 'clarity-assets'].includes(activeView) && dirty && <Chip label={d.changes} color="warning" variant="outlined" sx={{ display: { xs: 'none', sm: 'flex' } }} />}
            {['content', 'theme', 'courses', 'clarity-assets'].includes(activeView) && <Button variant="contained" onClick={publish} disabled={!content || saving || !dirty} startIcon={saving ? <CircularProgress size={18} color="inherit" /> : <PublishRoundedIcon />}>
              {saving ? d.publishing : d.publish}
            </Button>}
          </Toolbar>
        </Container>
      </AppBar>

      <Box component="nav">
        <Drawer anchor={dashboardDirection === 'rtl' ? 'right' : 'left'} variant="temporary" open={mobileMenuOpen} onClose={() => setMobileMenuOpen(false)} ModalProps={{ keepMounted: true }} sx={{ display: { xs: 'block', md: 'none' }, '& .MuiDrawer-paper': { width: SIDEBAR_WIDTH } }}><AdminSidebar activeView={activeView} setActiveView={setActiveView} close={() => setMobileMenuOpen(false)} d={d} /></Drawer>
        <Drawer anchor={dashboardDirection === 'rtl' ? 'right' : 'left'} variant="permanent" open sx={{ display: { xs: 'none', md: 'block' }, '& .MuiDrawer-paper': { width: SIDEBAR_WIDTH, boxSizing: 'border-box', border: 0 } }}><AdminSidebar activeView={activeView} setActiveView={setActiveView} d={d} /></Drawer>
      </Box>

      <Box component="main" sx={{ width: { xs: '100%', md: `calc(100% - ${SIDEBAR_WIDTH}px)` }, ml: { md: dashboardDirection === 'rtl' ? 0 : `${SIDEBAR_WIDTH}px` }, mr: { md: dashboardDirection === 'rtl' ? `${SIDEBAR_WIDTH}px` : 0 }, minWidth: 0 }}>
      <Toolbar sx={{ minHeight: '76px !important' }} />
      <Container maxWidth="lg" sx={{ py: { xs: 2.5, md: 4 } }}>
        <Paper sx={{ display: 'none', mb: 3, px: 1, borderRadius: 3 }}>
          <Tabs value={activeView} onChange={(_, value) => setActiveView(value)} variant="scrollable">
            <Tab icon={<DashboardRoundedIcon />} iconPosition="start" value="overview" label="Overview" />
            <Tab icon={<ArticleRoundedIcon />} iconPosition="start" value="content" label="Website content" />
            <Tab icon={<PeopleRoundedIcon />} iconPosition="start" value="users" label="Users" />
            <Tab value="courses" label="Courses" />
            <Tab value="sessions" label="Sessions" />
            <Tab value="payments" label="Payments" />
            <Tab value="program-clients" label="Career Clarity" />
            <Tab value="clarity-assets" label="Assessments & Media" />
            <Tab value="theme" label="Theme" />
          </Tabs>
        </Paper>

        {error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}

        {activeView === 'overview' && <ModernOverview users={users} language={dashboardLanguage} setActiveView={setActiveView} />}

        {activeView === 'theme' && content && <Stack spacing={3}>
          <Box><Typography variant="h4" fontWeight={900}>{d.theme}</Typography><Typography color="text.secondary">{d.themeHint}</Typography></Box>
          <Paper sx={{ p: { xs: 2, md: 3 }, borderRadius: 3 }}>
            <Typography variant="h6" fontWeight={900} color="primary" sx={{ mb: 2 }}>Colors & style</Typography>
            <RecursiveEditor value={content.appearance.theme} onChange={(theme) => updateSection('appearance', { ...content.appearance, theme })} path="appearance.theme" />
          </Paper>
          <Paper sx={{ p: { xs: 2, md: 3 }, borderRadius: 3 }}>
            <Typography variant="h6" fontWeight={900} color="primary" sx={{ mb: 2 }}>Homepage section visibility</Typography>
            <RecursiveEditor value={content.appearance.sections} onChange={(sections) => updateSection('appearance', { ...content.appearance, sections })} path="appearance.sections" />
          </Paper>
        </Stack>}

        {activeView === 'courses' && content && <Stack spacing={3}>
          <Stack direction={{ xs: 'column', sm: 'row' }} justifyContent="space-between" alignItems={{ sm: 'center' }} spacing={2}>
            <Box><Typography variant="h4" fontWeight={900}>{d.courses}</Typography><Typography color="text.secondary">Create, price, publish and organize courses into sections with video lessons.</Typography></Box>
            <Button component="a" href="/programs" target="_blank" variant="outlined" startIcon={<PreviewRoundedIcon />}>Preview catalog</Button>
          </Stack>
          <Paper sx={{ px: 2, borderRadius: 3 }}><Tabs value={language} onChange={(_, value) => setLanguage(value)}><Tab value="en" label="English" /><Tab value="ar" label="العربية" /></Tabs></Paper>
          <CourseManager courses={content.courseOffers ?? []} onChange={(value) => updateSection('courseOffers', value)} language={language} />
        </Stack>}

        {activeView === 'sessions' && <SessionManager language={dashboardLanguage} />}
        {activeView === 'payments' && <PaymentManager language={dashboardLanguage} />}
        {activeView === 'program-clients' && <ProgramClientsManager language={dashboardLanguage} />}
        {activeView === 'clarity-assets' && content && <CareerClarityAssetsManager courses={content.courseOffers ?? []} language={language} onChange={(value) => updateSection('courseOffers', value)} />}

        <Paper component="form" onSubmit={submitNewUser} sx={{ display: activeView === 'users' ? 'block' : 'none', mb: 3, p: 3, borderRadius: 3 }}>
          <Typography variant="h6" fontWeight={900}>{d.createUser}</Typography>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2.5 }}>{d.createHint}</Typography>
          <Grid container spacing={2} alignItems="center">
            <Grid size={{ xs: 12, md: 3 }}><TextField required fullWidth label={d.fullName} value={newUser.name} onChange={(event) => setNewUser({ ...newUser, name: event.target.value })} /></Grid>
            <Grid size={{ xs: 12, md: 3 }}><TextField required fullWidth type="email" label={d.email} value={newUser.email} onChange={(event) => setNewUser({ ...newUser, email: event.target.value })} /></Grid>
            <Grid size={{ xs: 12, md: 2 }}><TextField required fullWidth type="password" label={d.password} slotProps={{ htmlInput: { minLength: 8 } }} value={newUser.password} onChange={(event) => setNewUser({ ...newUser, password: event.target.value })} /></Grid>
            <Grid size={{ xs: 12, md: 2 }}><TextField select fullWidth label={d.role} value={newUser.role} onChange={(event) => setNewUser({ ...newUser, role: event.target.value })}><MenuItem value="Client">Client</MenuItem><MenuItem value="Admin">Admin</MenuItem></TextField></Grid>
            <Grid size={{ xs: 12, md: 2 }}><Button fullWidth type="submit" variant="contained" disabled={creatingUser} sx={{ minHeight: 56 }}>{creatingUser ? <CircularProgress size={20} color="inherit" /> : d.create}</Button></Grid>
          </Grid>
        </Paper>

        {activeView === 'users' && <Paper sx={{ mb: 3, p: 3, borderRadius: 3 }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}><Typography variant="h6" fontWeight={900}>{d.allUsers}</Typography><Button variant="outlined" onClick={exportUsers} disabled={!users.length}>Export CSV</Button></Stack>
          {loadingUsers ? <CircularProgress size={24} /> : <Stack spacing={1}>{users.map((user) => (
            <Stack key={user.userId} direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }} sx={{ p: 2, border: '1px solid', borderColor: 'divider', borderRadius: 2.5 }}>
              <Box sx={{ flexGrow: 1 }}><Typography fontWeight={900}>{user.name}</Typography><Typography variant="body2" color="text.secondary">{user.email} · {d.joined} {new Date(user.createdAtUtc).toLocaleDateString(dashboardLanguage === 'ar' ? 'ar-JO' : 'en-US')}</Typography></Box>
              <TextField select size="small" label={d.role} value={user.role} disabled={user.userId === session.userId} onChange={(event) => changeRole(user.userId, event.target.value)} sx={{ minWidth: 130 }}><MenuItem value="Client">Client</MenuItem><MenuItem value="Admin">Admin</MenuItem></TextField>
            </Stack>
          ))}</Stack>}
        </Paper>}

        <Paper sx={{ display: activeView === 'content' ? 'block' : 'none', mb: 3, px: 2, borderRadius: 3 }}>
          <Tabs value={language} onChange={(_, value) => setLanguage(value)}>
            <Tab value="en" label="English" />
            <Tab value="ar" label="العربية" />
          </Tabs>
        </Paper>

        {activeView === 'content' && loading && <Box sx={{ display: 'grid', placeItems: 'center', py: 12 }}><CircularProgress /></Box>}

        {activeView === 'content' && !loading && content && (
          <Stack spacing={3} dir={language === 'ar' ? 'rtl' : 'ltr'}>
            <Paper sx={{ px: { xs: 2, md: 3 }, py: 2, borderRadius: 3 }}>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }} justifyContent="space-between">
                <Box><Typography variant="h5" fontWeight={900}>Website content</Typography><Typography variant="body2" color="text.secondary">Choose a section, edit its content, preview, then publish.</Typography></Box>
                <Button component="a" href="/" target="_blank" variant="outlined" startIcon={<PreviewRoundedIcon />}>Preview website</Button>
              </Stack>
            </Paper>

            <TextField select fullWidth label="Content section" value={selectedContentSection} onChange={(event) => setSelectedContentSection(event.target.value)} sx={{ display: { md: 'none' }, bgcolor: 'background.paper' }}>
              {Object.keys(content).filter((key) => key !== 'appearance').map((key) => <MenuItem key={key} value={key}>{SECTION_LABELS[key] || humanize(key)}</MenuItem>)}
            </TextField>

            <Grid container spacing={3} alignItems="flex-start">
              <Grid size={{ xs: 12, md: 3 }} sx={{ display: { xs: 'none', md: 'block' } }}>
                <Paper sx={{ p: 1.5, borderRadius: 3, position: 'sticky', top: 100 }}>
                  <Typography variant="overline" color="text.secondary" fontWeight={900} sx={{ px: 1.5 }}>Content sections</Typography>
                  <List disablePadding sx={{ mt: 1 }}>{Object.keys(content).filter((key) => key !== 'appearance').map((key, index) => <ListItemButton key={key} selected={selectedContentSection === key} onClick={() => setSelectedContentSection(key)} sx={{ borderRadius: 2.5, mb: .5, py: 1.4, '&.Mui-selected': { bgcolor: 'primary.main', color: 'primary.contrastText' }, '&.Mui-selected:hover': { bgcolor: 'primary.dark' } }}><Chip label={String(index + 1).padStart(2, '0')} size="small" sx={{ mr: 1.25, bgcolor: selectedContentSection === key ? 'rgba(255,255,255,.18)' : undefined, color: 'inherit' }} /><ListItemText primary={SECTION_LABELS[key] || humanize(key)} slotProps={{ primary: { fontWeight: 800, fontSize: 14 } }} /><ChevronRightRoundedIcon fontSize="small" /></ListItemButton>)}</List>
                </Paper>
              </Grid>
              <Grid size={{ xs: 12, md: 9 }}>
                <Paper sx={{ borderRadius: 3, overflow: 'hidden' }}>
                  <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ px: { xs: 2.5, md: 3.5 }, py: 2.5, bgcolor: 'rgba(47,79,35,.055)', borderBottom: '1px solid', borderColor: 'divider' }}><Box><Typography variant="h5" fontWeight={900}>{SECTION_LABELS[selectedContentSection] || humanize(selectedContentSection)}</Typography><Typography variant="body2" color="text.secondary">Editing {language.toUpperCase()} content</Typography></Box>{dirty && <Chip label="Unsaved" color="warning" size="small" />}</Stack>
                  <Box sx={{ p: { xs: 2.5, md: 3.5 } }}><RecursiveEditor value={content[selectedContentSection]} onChange={(value) => updateSection(selectedContentSection, value)} path={selectedContentSection} /></Box>
                </Paper>
              </Grid>
            </Grid>
          </Stack>
        )}
      </Container>
      </Box>
      <Snackbar open={Boolean(notice)} autoHideDuration={4000} onClose={() => setNotice('')} message={notice} />
    </Box>
  );
}

function AdminSidebar({ activeView, setActiveView, close, d }) {
  const navigate = (view) => { setActiveView(view); close?.(); };
  const items = [
    { value: 'overview', label: d.overview, icon: <DashboardRoundedIcon /> },
    { value: 'content', label: d.content, icon: <ArticleRoundedIcon /> },
    { value: 'courses', label: d.courses, icon: <MenuBookRoundedIcon /> },
    { value: 'sessions', label: d.sessions, icon: <DashboardRoundedIcon /> },
    { value: 'payments', label: d.payments, icon: <PaymentsRoundedIcon /> },
    { value: 'program-clients', label: d.programClients, icon: <WorkspacePremiumRoundedIcon /> },
    { value: 'clarity-assets', label: d.clarityAssets, icon: <ArticleRoundedIcon /> },
    { value: 'theme', label: d.theme, icon: <PaletteRoundedIcon /> },
    { value: 'users', label: d.users, icon: <PeopleRoundedIcon /> },
  ];
  return <Stack sx={{ height: '100%', bgcolor: '#172044', color: 'white' }}>
    <Box sx={{ p: 3, minHeight: 100, display: 'grid', alignItems: 'center' }}><Box component="img" src="/assets/bawsala-logo-horizontal.png" alt="Bawsala Consultancy" sx={{ width: 210, maxWidth: '100%', bgcolor: 'white', borderRadius: 2, p: 1 }} /></Box>
    <Divider sx={{ borderColor: 'rgba(255,255,255,.1)' }} />
    <List sx={{ px: 1.5, py: 2 }}>{items.map((item) => <ListItemButton key={item.value} selected={activeView === item.value} onClick={() => navigate(item.value)} sx={{ mb: .75, borderRadius: 2.5, color: 'rgba(255,255,255,.72)', '&.Mui-selected': { bgcolor: 'rgba(184,207,124,.18)', color: '#d9edaa' }, '&.Mui-selected:hover': { bgcolor: 'rgba(184,207,124,.24)' } }}><ListItemIcon sx={{ minWidth: 42, color: 'inherit' }}>{item.icon}</ListItemIcon><ListItemText primary={item.label} slotProps={{ primary: { fontWeight: 800 } }} /></ListItemButton>)}</List>
    <Box sx={{ mt: 'auto', p: 1.5 }}><ListItemButton component="a" href="/" sx={{ borderRadius: 2.5, color: 'rgba(255,255,255,.72)' }}><ListItemIcon sx={{ minWidth: 42, color: 'inherit' }}><HomeRoundedIcon /></ListItemIcon><ListItemText primary={d.viewWebsite} /></ListItemButton><ListItemButton onClick={() => { clearSession(); window.location.href = '/login'; }} sx={{ borderRadius: 2.5, color: 'rgba(255,255,255,.72)' }}><ListItemIcon sx={{ minWidth: 42, color: 'inherit' }}><LogoutRoundedIcon /></ListItemIcon><ListItemText primary={d.logout} /></ListItemButton></Box>
  </Stack>;
}

function StatCard({ label, value, color }) {
  return (
    <Paper sx={{ p: 3, borderRadius: 3, borderTop: '4px solid', borderColor: color }}>
      <Typography color="text.secondary" fontWeight={700}>{label}</Typography>
      <Typography variant="h3" fontWeight={900} sx={{ mt: 1, color }}>{value}</Typography>
    </Paper>
  );
}

function ModernOverview({ users, language, setActiveView }) {
  const ar = language === 'ar';
  const [insights, setInsights] = useState({ payments: [], sessions: [], clients: [] });
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    let active = true;
    Promise.all([getAdminPayments(), getAdminSessions(), getProgramClients()])
      .then(([payments, sessions, clients]) => { if (active) setInsights({ payments, sessions, clients }); })
      .catch(() => { if (active) setInsights({ payments: [], sessions: [], clients: [] }); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, []);
  const clients = users.filter((user) => user.role === 'Client');
  const paid = insights.payments.filter((payment) => payment.status === 'paid');
  const revenue = paid.reduce((sum, payment) => sum + Number(payment.amount || 0), 0);
  const upcoming = insights.sessions.filter((session) => new Date(session.startsAtUtc) > new Date());
  const clarityClients = insights.clients.filter((client) => client.enrollments?.some((item) => item.productSlug === 'career-clarity-program'));
  const assessmentCounts = ['strengths', 'interests', 'values', 'purpose'].map((key) => clarityClients.filter((client) => client.assessments?.[key]).length);
  const maxAssessment = Math.max(1, ...assessmentCounts);
  const readyProfiles = clarityClients.filter((client) => client.profile?.status === 'ready').length;
  const completionRate = clarityClients.length ? Math.round(readyProfiles / clarityClients.length * 100) : 0;
  return <Stack spacing={3.2} sx={{ mb: 3 }}>
    <Paper sx={{ position: 'relative', overflow: 'hidden', p: { xs: 3, md: 4.5 }, borderRadius: 5, color: 'white', background: 'linear-gradient(125deg,#102d5b,#214b7c 70%,#a47d28 145%)', boxShadow: '0 28px 70px rgba(16,45,91,.2)' }}>
      <Box sx={{ position: 'absolute', width: 360, height: 360, borderRadius: '50%', border: '1px solid rgba(255,255,255,.1)', top: -220, insetInlineEnd: -50, boxShadow: '0 0 0 55px rgba(255,255,255,.025)' }} />
      <Typography sx={{ color: '#e5bf7b', fontSize: 11, fontWeight: 900, letterSpacing: '.14em', textTransform: 'uppercase' }}>{ar ? 'نبض منصة بوصلة' : 'Bawsala platform pulse'}</Typography>
      <Typography variant="h3" sx={{ mt: 1, maxWidth: 720, fontFamily: 'Georgia,serif', color: 'white' }}>{ar ? 'كل ما تحتاجه دانا لاتخاذ قرار أفضل، في لمحة واحدة.' : 'Everything Dana needs to make the next decision, at a glance.'}</Typography>
      <Typography sx={{ mt: 1.5, color: 'rgba(255,255,255,.68)' }}>{ar ? 'المؤشرات تتحدث تلقائياً مع نشاط العملاء والحجوزات والدفعات.' : 'Live indicators reflect client activity, bookings, payments, and Career Clarity progress.'}</Typography>
    </Paper>
    <Grid container spacing={2.2}>
      <Grid size={{ xs: 12, sm: 6, lg: 3 }}><OverviewKpi icon={<PeopleRoundedIcon />} label={ar ? 'العملاء' : 'Active clients'} value={clients.length} trend={ar ? 'إجمالي حسابات العملاء' : 'Total client accounts'} /></Grid>
      <Grid size={{ xs: 12, sm: 6, lg: 3 }}><OverviewKpi icon={<PaymentsRoundedIcon />} label={ar ? 'الإيراد' : 'Revenue'} value={`$${revenue.toLocaleString()}`} trend={`${paid.length} ${ar ? 'عملية ناجحة' : 'successful payments'}`} gold /></Grid>
      <Grid size={{ xs: 12, sm: 6, lg: 3 }}><OverviewKpi icon={<EventAvailableRoundedIcon />} label={ar ? 'الجلسات القادمة' : 'Upcoming sessions'} value={upcoming.length} trend={`${upcoming.reduce((sum, item) => sum + (item.attendees?.length ?? 0), 0)} ${ar ? 'حجز' : 'bookings'}`} /></Grid>
      <Grid size={{ xs: 12, sm: 6, lg: 3 }}><OverviewKpi icon={<WorkspacePremiumRoundedIcon />} label="Career Clarity" value={clarityClients.length} trend={`${completionRate}% ${ar ? 'ملفات جاهزة' : 'profiles ready'}`} gold /></Grid>
    </Grid>
    <Grid container spacing={2.5}>
      <Grid size={{ xs: 12, lg: 7 }}><Paper sx={{ p: { xs: 2.5, md: 3.5 }, borderRadius: 4, height: '100%', border: '1px solid #e3e2db' }}><Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}><Box><Typography variant="h6" fontWeight={900}>{ar ? 'تقدم التقييمات' : 'Assessment engagement'}</Typography><Typography variant="body2" color="text.secondary">{ar ? 'عدد العملاء الذين أكملوا كل مرحلة.' : 'Clients completing each Career Clarity stage.'}</Typography></Box><TrendingUpRoundedIcon sx={{ color: '#a47d28' }} /></Stack><Box sx={{ mt: 4, height: 245, display: 'flex', alignItems: 'end', gap: { xs: 1.5, sm: 3 }, px: 1, borderBottom: '1px solid #dddcd4' }}>{assessmentCounts.map((count, index) => <Box key={index} sx={{ flex: 1, textAlign: 'center' }}><Typography fontWeight={900} color="#102d5b" sx={{ mb: 1 }}>{count}</Typography><Box sx={{ height: `${Math.max(12, count / maxAssessment * 170)}px`, borderRadius: '12px 12px 3px 3px', background: index === 3 ? 'linear-gradient(#d4ad67,#a47d28)' : 'linear-gradient(#315d91,#102d5b)', transition: 'height .5s ease' }} /><Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1 }}>{[ar ? 'القوة' : 'Strengths', ar ? 'الميول' : 'Interests', ar ? 'القيم' : 'Values', ar ? 'الغاية' : 'Purpose'][index]}</Typography></Box>)}</Box></Paper></Grid>
      <Grid size={{ xs: 12, lg: 5 }}><Paper sx={{ p: { xs: 2.5, md: 3.5 }, borderRadius: 4, height: '100%', border: '1px solid #e3e2db' }}><Typography variant="h6" fontWeight={900}>{ar ? 'حالة برنامج الوضوح' : 'Career Clarity health'}</Typography><Box sx={{ my: 3.5, display: 'grid', placeItems: 'center' }}><Box sx={{ width: 178, height: 178, borderRadius: '50%', display: 'grid', placeItems: 'center', background: `conic-gradient(#c69a55 ${completionRate * 3.6}deg,#edf0f5 0)`, position: 'relative', '&:after': { content: '""', position: 'absolute', inset: 15, borderRadius: '50%', bgcolor: 'white' } }}><Box sx={{ position: 'relative', zIndex: 1, textAlign: 'center' }}><Typography variant="h3" color="#102d5b" fontWeight={900}>{completionRate}%</Typography><Typography variant="caption" color="text.secondary">{ar ? 'جاهزية الملفات' : 'profile readiness'}</Typography></Box></Box></Box><Stack spacing={1.2}><Button variant="contained" onClick={() => setActiveView('program-clients')}>{ar ? 'عرض عملاء البرنامج' : 'Review Career Clarity clients'}</Button><Button variant="outlined" onClick={() => setActiveView('sessions')}>{ar ? 'إدارة الجلسات' : 'Manage sessions'}</Button></Stack></Paper></Grid>
    </Grid>
    {loading && <LinearProgress sx={{ borderRadius: 99 }} />}
  </Stack>;
}

function OverviewKpi({ icon, label, value, trend, gold = false }) {
  return <Paper sx={{ p: 2.7, borderRadius: 4, border: '1px solid #e3e2db', boxShadow: '0 14px 35px rgba(16,45,91,.05)' }}><Stack direction="row" spacing={1.7} sx={{ alignItems: 'center' }}><Box sx={{ width: 48, height: 48, borderRadius: 2.5, display: 'grid', placeItems: 'center', bgcolor: gold ? '#f5ecdc' : '#edf2f8', color: gold ? '#a47d28' : '#102d5b' }}>{icon}</Box><Box><Typography variant="caption" color="text.secondary">{label}</Typography><Typography variant="h4" fontWeight={900} color="#102d5b">{value}</Typography></Box></Stack><Typography variant="caption" sx={{ display: 'block', mt: 1.5, color: '#718074' }}>{trend}</Typography></Paper>;
}

function ProgramClientsManager({ language }) {
  const ar = language === 'ar'; const [clients, setClients] = useState([]); const [loading, setLoading] = useState(true); const [error, setError] = useState(''); const [selected, setSelected] = useState(null); const [notes, setNotes] = useState(''); const [saving, setSaving] = useState(false);
  const [search, setSearch] = useState(''); const [status, setStatus] = useState('all'); const [completion, setCompletion] = useState('all');
  const load = () => { setLoading(true); getProgramClients().then(setClients).catch((e) => setError(e.response?.data?.message || e.message)).finally(() => setLoading(false)); };
  useEffect(load, []);
  const programClients = clients.filter((client) => client.enrollments.some((item) => item.productSlug === 'career-clarity-program'));
  const visibleClients = programClients.filter((client) => {
    const completed = Object.keys(client.assessments ?? {}).filter((key) => ['strengths','interests','values','purpose'].includes(key)).length;
    const matchesSearch = `${client.name} ${client.email}`.toLowerCase().includes(search.trim().toLowerCase());
    const matchesStatus = status === 'all' || (client.profile?.status ?? 'not-started') === status;
    const matchesCompletion = completion === 'all' || (completion === 'complete' ? completed === 4 : completed < 4);
    return matchesSearch && matchesStatus && matchesCompletion;
  });
  const open = (client) => { setSelected(client); setNotes(client.profile?.adminNotes ?? ''); };
  const save = async (release) => { setSaving(true); setError(''); try { const updated = await reviewCareerProfile(selected.userId, notes, release); setClients((items) => items.map((item) => item.userId === updated.userId ? updated : item)); setSelected(updated); } catch (e) { setError(e.response?.data?.message || e.message); } finally { setSaving(false); } };
  const exportCsv = () => { const rows = [['Name','Email','Status','Strengths','Interests','Values','Purpose'], ...programClients.map((c) => [c.name,c.email,c.profile?.status,...['strengths','interests','values','purpose'].map((key) => c.assessments[key]?.summary ?? '')])]; const csv = rows.map((row) => row.map((value) => `"${String(value ?? '').replaceAll('"','""')}"`).join(',')).join('\r\n'); const url = URL.createObjectURL(new Blob(['\ufeff'+csv], { type: 'text/csv;charset=utf-8' })); const link = document.createElement('a'); link.href=url; link.download='career-clarity-clients.csv'; link.click(); URL.revokeObjectURL(url); };
  return <Stack spacing={3}>
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ justifyContent: 'space-between', alignItems: { sm: 'center' } }}>
      <Box><Typography variant="h4" fontWeight={900}>{ar ? 'عملاء برنامج وضوح المسار' : 'Career Clarity clients'}</Typography><Typography color="text.secondary">{ar ? 'راجع التقييمات، أضف ملاحظاتك، ثم اعتمد الملف للعميل.' : 'Review assessment results, add coach notes, then release the profile to the client.'}</Typography></Box>
      <Button variant="outlined" onClick={exportCsv} disabled={!programClients.length}>Export CSV</Button>
    </Stack>
    {error && <Alert severity="error">{error}</Alert>}
    <Paper sx={{ p: 2.2, borderRadius: 3.5, border: '1px solid #e3e2db' }}><Grid container spacing={1.5}><Grid size={{ xs: 12, md: 6 }}><TextField fullWidth size="small" label={ar ? 'ابحث بالاسم أو البريد' : 'Search name or email'} value={search} onChange={(event) => setSearch(event.target.value)} slotProps={{ input: { startAdornment: <SearchRoundedIcon sx={{ mr: 1, color: 'text.disabled' }} /> } }} /></Grid><Grid size={{ xs: 6, md: 3 }}><TextField select fullWidth size="small" label={ar ? 'حالة الملف' : 'Profile status'} value={status} onChange={(event) => setStatus(event.target.value)}><MenuItem value="all">{ar ? 'كل الحالات' : 'All statuses'}</MenuItem><MenuItem value="not-started">Not started</MenuItem><MenuItem value="in-review">In review</MenuItem><MenuItem value="ready">Ready</MenuItem></TextField></Grid><Grid size={{ xs: 6, md: 3 }}><TextField select fullWidth size="small" label={ar ? 'التقييمات' : 'Assessments'} value={completion} onChange={(event) => setCompletion(event.target.value)}><MenuItem value="all">{ar ? 'الكل' : 'All'}</MenuItem><MenuItem value="complete">{ar ? 'مكتملة' : 'Completed 4/4'}</MenuItem><MenuItem value="pending">{ar ? 'غير مكتملة' : 'Still in progress'}</MenuItem></TextField></Grid></Grid><Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1.2 }}>{visibleClients.length} {ar ? 'نتيجة' : 'matching clients'}</Typography></Paper>
    <Grid container spacing={2.5}>{loading ? <Grid size={{ xs: 12 }}><CircularProgress /></Grid> : visibleClients.map((client) => {
      const completed = Object.keys(client.assessments).filter((key) => ['strengths','interests','values','purpose'].includes(key)).length;
      return <Grid key={client.userId} size={{ xs: 12, md: 6 }}><Paper sx={{ p: 3, borderRadius: 3, height: '100%', borderTop: '4px solid', borderColor: client.profile?.status === 'ready' ? 'success.main' : completed === 4 ? 'warning.main' : 'divider' }}><Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start' }}><Box><Typography variant="h6" fontWeight={900}>{client.name}</Typography><Typography variant="body2" color="text.secondary">{client.email}</Typography></Box><Chip label={client.profile?.status ?? 'not-started'} color={client.profile?.status === 'ready' ? 'success' : completed === 4 ? 'warning' : 'default'} /></Stack><Typography sx={{ mt: 2 }} fontWeight={800}>{completed}/4 {ar ? 'تقييمات مكتملة' : 'assessments completed'}</Typography><LinearProgress variant="determinate" value={completed * 25} sx={{ mt: 1, height: 8, borderRadius: 5 }} /><Button fullWidth variant="contained" sx={{ mt: 2.5 }} onClick={() => open(client)}>{ar ? 'فتح الملف' : 'Open client profile'}</Button></Paper></Grid>;
    })}</Grid>
    {!loading && !visibleClients.length && <Alert severity="info">{programClients.length ? (ar ? 'لا توجد نتائج مطابقة للفلاتر.' : 'No clients match the selected filters.') : (ar ? 'لا يوجد عملاء مسجلون في البرنامج بعد.' : 'No clients are enrolled in the program yet.')}</Alert>}
    <Dialog open={Boolean(selected)} onClose={() => !saving && setSelected(null)} fullWidth maxWidth="md"><DialogTitle><Typography variant="h5" fontWeight={900}>{selected?.name}</Typography><Typography variant="body2" color="text.secondary">{selected?.email}</Typography></DialogTitle><DialogContent><Stack spacing={2.5}>{selected && ['strengths','interests','values','purpose'].map((key) => <Paper key={key} variant="outlined" sx={{ p: 2.5 }}><Typography color="primary" fontWeight={900}>{humanize(key)}</Typography>{selected.assessments[key] ? <><Typography sx={{ mt: 1 }} fontWeight={800}>{selected.assessments[key].summary}</Typography><Divider sx={{ my: 1.5 }} />{Object.entries(selected.assessments[key].answers).map(([answerKey, value]) => <Typography key={answerKey} variant="body2" color="text.secondary"><b>{humanize(answerKey)}:</b> {value}</Typography>)}</> : <Typography color="text.secondary" sx={{ mt: 1 }}>Not completed</Typography>}</Paper>)}<TextField multiline minRows={4} label={ar ? 'ملاحظات دانا للعميل' : 'Dana’s notes for the client'} value={notes} onChange={(e) => setNotes(e.target.value)} /></Stack></DialogContent><DialogActions><Button onClick={() => setSelected(null)}>{ar ? 'إغلاق' : 'Close'}</Button><Button disabled={saving || Object.keys(selected?.assessments ?? {}).length < 4} onClick={() => save(false)} variant="outlined">{ar ? 'حفظ الملاحظات' : 'Save notes'}</Button><Button disabled={saving || Object.keys(selected?.assessments ?? {}).length < 4} onClick={() => save(true)} variant="contained" color="success">{saving ? <CircularProgress size={20} /> : (ar ? 'اعتماد وإرسال الملف' : 'Release profile')}</Button></DialogActions></Dialog>
  </Stack>;
}

function PaymentManager({ language }) {
  const [payments, setPayments] = useState([]); const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  const [status, setStatus] = useState('all'); const [search, setSearch] = useState(''); const [selected, setSelected] = useState(null); const [reason, setReason] = useState(''); const [refunding, setRefunding] = useState(false);
  const ar = language === 'ar';
  const load = () => { setLoading(true); getAdminPayments().then(setPayments).catch((e) => setError(e.response?.data?.message || e.message)).finally(() => setLoading(false)); };
  useEffect(load, []);
  const visible = payments.filter((payment) => (status === 'all' || payment.status === status) && `${payment.customerName} ${payment.customerEmail} ${payment.productTitle} ${payment.providerReference}`.toLowerCase().includes(search.toLowerCase()));
  const paid = payments.filter((payment) => payment.status === 'paid'); const refunded = payments.filter((payment) => payment.status === 'refunded');
  const money = (amount, currency = 'USD') => new Intl.NumberFormat(ar ? 'ar-JO' : 'en-US', { style: 'currency', currency }).format(amount);
  const refund = async () => { if (!reason.trim()) return; setRefunding(true); setError(''); try { const updated = await refundAdminPayment(selected.id, reason); setPayments((items) => items.map((item) => item.id === updated.id ? updated : item)); setSelected(null); setReason(''); } catch (e) { setError(e.response?.data?.message || e.message); } finally { setRefunding(false); } };
  const exportCsv = () => { const rows = [['Reference','Customer','Email','Service','Product','Amount','Currency','Status','Created','Refund reason'], ...visible.map((p) => [p.providerReference,p.customerName,p.customerEmail,p.serviceType,p.productTitle,p.amount,p.currency,p.status,p.createdAtUtc,p.refundReason])]; const csv=rows.map((row)=>row.map((value)=>`"${String(value??'').replaceAll('"','""')}"`).join(',')).join('\r\n'); const url=URL.createObjectURL(new Blob(['\ufeff'+csv],{type:'text/csv;charset=utf-8'})); const link=document.createElement('a');link.href=url;link.download='payments.csv';link.click();URL.revokeObjectURL(url); };
  return <Stack spacing={3}>
    <Stack direction={{ xs:'column', sm:'row' }} justifyContent="space-between" alignItems={{ sm:'center' }} spacing={2}><Box><Typography variant="h4" fontWeight={900}>{ar ? 'الدفعات والمعاملات' : 'Payments & transactions'}</Typography><Typography color="text.secondary">{ar ? 'تابع الإيرادات، تفاصيل العمليات، والاستردادات من مكان واحد.' : 'Track revenue, inspect every transaction, and manage refunds in one place.'}</Typography></Box><Button variant="outlined" onClick={exportCsv} disabled={!visible.length}>Export CSV</Button></Stack>
    {error && <Alert severity="error">{error}</Alert>}
    <Grid container spacing={2.5}><Grid size={{ xs: 12, md: 4 }}><StatCard label={ar ? 'الإيراد الصافي' : 'Net revenue'} value={money(paid.reduce((sum, p) => sum + p.amount, 0))} color="#2f4f23" /></Grid><Grid size={{ xs: 12, md: 4 }}><StatCard label={ar ? 'عمليات ناجحة' : 'Successful payments'} value={paid.length} color="#20295b" /></Grid><Grid size={{ xs: 12, md: 4 }}><StatCard label={ar ? 'مبالغ مستردة' : 'Refunded'} value={money(refunded.reduce((sum, p) => sum + p.amount, 0))} color="#a47d28" /></Grid></Grid>
    <Paper sx={{ p: 2.5, borderRadius: 3 }}><Stack direction={{ xs: 'column', md: 'row' }} spacing={2}><TextField fullWidth label={ar ? 'بحث بالعميل أو المرجع أو الخدمة' : 'Search customer, reference, or service'} value={search} onChange={(e) => setSearch(e.target.value)} /><TextField select label={ar ? 'الحالة' : 'Status'} value={status} onChange={(e) => setStatus(e.target.value)} sx={{ minWidth: 180 }}><MenuItem value="all">{ar ? 'الكل' : 'All'}</MenuItem><MenuItem value="paid">{ar ? 'مدفوع' : 'Paid'}</MenuItem><MenuItem value="refunded">{ar ? 'مسترد' : 'Refunded'}</MenuItem></TextField></Stack></Paper>
    {loading ? <CircularProgress /> : <Stack spacing={1.5}>{visible.map((payment) => <Paper key={payment.id} sx={{ p: 2.5, borderRadius: 3, borderInlineStart: '5px solid', borderColor: payment.status === 'paid' ? 'success.main' : 'warning.main' }}><Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems={{ md: 'center' }}><Box sx={{ flex: 1 }}><Stack direction="row" spacing={1} alignItems="center"><Typography fontWeight={900}>{payment.customerName}</Typography><Chip size="small" label={payment.status} color={payment.status === 'paid' ? 'success' : 'warning'} /></Stack><Typography variant="body2" color="text.secondary">{payment.customerEmail} · {payment.productTitle}</Typography><Typography variant="caption" color="text.secondary">{payment.providerReference} · {new Date(payment.createdAtUtc).toLocaleString(ar ? 'ar-JO' : 'en-US')}</Typography></Box><Box sx={{ minWidth: 130 }}><Typography variant="h6" fontWeight={900}>{money(payment.amount, payment.currency)}</Typography><Typography variant="caption" color="text.secondary">{payment.provider} · {payment.serviceType}</Typography></Box>{payment.status === 'paid' && <Button color="warning" variant="outlined" onClick={() => setSelected(payment)}>{ar ? 'استرداد' : 'Refund'}</Button>}</Stack>{payment.refundReason && <Alert severity="warning" sx={{ mt: 2 }}>{ar ? 'سبب الاسترداد: ' : 'Refund reason: '}{payment.refundReason}</Alert>}</Paper>)}</Stack>}
    {!loading && !visible.length && <Alert severity="info">{ar ? 'لا توجد معاملات مطابقة.' : 'No matching transactions.'}</Alert>}
    <Dialog open={Boolean(selected)} onClose={() => !refunding && setSelected(null)} fullWidth maxWidth="sm"><DialogTitle fontWeight={900}>{ar ? 'تأكيد استرداد الدفعة' : 'Confirm payment refund'}</DialogTitle><DialogContent><Alert severity="warning" sx={{ mb: 2 }}>{ar ? 'سيتم إلغاء وصول العميل للخدمة بعد الاسترداد.' : 'The client’s access to this service will be revoked after the refund.'}</Alert><Typography fontWeight={800} sx={{ mb: 2 }}>{selected?.customerName} · {selected && money(selected.amount, selected.currency)}</Typography><TextField autoFocus required fullWidth multiline minRows={3} label={ar ? 'سبب الاسترداد' : 'Refund reason'} value={reason} onChange={(e) => setReason(e.target.value)} /></DialogContent><DialogActions><Button onClick={() => setSelected(null)}>{ar ? 'إلغاء' : 'Cancel'}</Button><Button color="warning" variant="contained" disabled={!reason.trim() || refunding} onClick={refund}>{refunding ? <CircularProgress size={20} color="inherit" /> : (ar ? 'تأكيد الاسترداد' : 'Confirm refund')}</Button></DialogActions></Dialog>
  </Stack>;
}

function SessionManager({ language }) {
  const [sessions, setSessions] = useState([]); const [loading, setLoading] = useState(true); const [saving, setSaving] = useState(false); const [error, setError] = useState('');
  const [form, setForm] = useState({ sessionType: 'coaching', titleEn: '', titleAr: '', startsAtUtc: '', endsAtUtc: '', capacity: 10, meetingUrl: '' });
  const [search, setSearch] = useState(''); const [view, setView] = useState('upcoming'); const [typeFilter, setTypeFilter] = useState('all');
  const load = () => { setLoading(true); getAdminSessions().then(setSessions).catch((e) => setError(e.message)).finally(() => setLoading(false)); };
  useEffect(load, []);
  const submit = async (event) => { event.preventDefault(); setError(''); const starts = new Date(form.startsAtUtc); const ends = new Date(form.endsAtUtc); if (!form.startsAtUtc || !form.endsAtUtc || ends <= starts) { setError(language === 'ar' ? 'يجب أن يكون وقت النهاية بعد وقت البداية.' : 'Session end time must be after its start time.'); return; } if (form.meetingUrl && !/^https?:\/\//i.test(form.meetingUrl)) { setError(language === 'ar' ? 'رابط الاجتماع يجب أن يبدأ بـ http أو https.' : 'Meeting URL must start with http:// or https://.'); return; } setSaving(true); try { await createPlatformSession({ ...form, startsAtUtc: starts.toISOString(), endsAtUtc: ends.toISOString(), capacity: form.capacity ? Number(form.capacity) : null }); setForm({ sessionType: 'coaching', titleEn: '', titleAr: '', startsAtUtc: '', endsAtUtc: '', capacity: 10, meetingUrl: '' }); load(); } catch (e) { setError(e.response?.data?.message || e.message); } finally { setSaving(false); } };
  const upcoming = sessions.filter((item) => new Date(item.startsAtUtc) > new Date());
  const past = sessions.filter((item) => new Date(item.endsAtUtc) <= new Date());
  const visibleSessions = sessions.filter((item) => {
    const now = new Date(); const isUpcoming = new Date(item.startsAtUtc) > now; const isPast = new Date(item.endsAtUtc) <= now; const isLive = !isUpcoming && !isPast;
    const matchesView = view === 'all' || (view === 'upcoming' && isUpcoming) || (view === 'live' && isLive) || (view === 'past' && isPast);
    const matchesType = typeFilter === 'all' || item.sessionType === typeFilter;
    const matchesSearch = `${item.titleEn} ${item.titleAr} ${item.sessionType}`.toLowerCase().includes(search.trim().toLowerCase());
    return matchesView && matchesType && matchesSearch;
  }).sort((left, right) => new Date(left.startsAtUtc) - new Date(right.startsAtUtc));
  return <Stack spacing={3}><Box><Typography variant="h4" fontWeight={900}>{language === 'ar' ? 'إدارة الجلسات' : 'Session management'}</Typography><Typography color="text.secondary">{language === 'ar' ? 'أنشئ المواعيد المتاحة وتابع العملاء المسجلين في كل جلسة.' : 'Create available times and see every client booked into each session.'}</Typography></Box>{error && <Alert severity="error">{error}</Alert>}
    <Paper component="form" onSubmit={submit} sx={{ p: { xs: 2.5, md: 3.5 }, borderRadius: 3 }}><Typography variant="h6" fontWeight={900} sx={{ mb: 2.5 }}>{language === 'ar' ? 'جلسة جديدة' : 'Create a session'}</Typography><Grid container spacing={2}><Grid size={{ xs: 12, md: 3 }}><TextField select fullWidth label="Type" value={form.sessionType} onChange={(e) => setForm({ ...form, sessionType: e.target.value })}>{['coaching', 'group', 'workshop', 'webinar'].map((value) => <MenuItem key={value} value={value}>{humanize(value)}</MenuItem>)}</TextField></Grid><Grid size={{ xs: 12, md: 4.5 }}><TextField required fullWidth label="English title" value={form.titleEn} onChange={(e) => setForm({ ...form, titleEn: e.target.value })} /></Grid><Grid size={{ xs: 12, md: 4.5 }}><TextField required fullWidth label="العنوان العربي" value={form.titleAr} onChange={(e) => setForm({ ...form, titleAr: e.target.value })} /></Grid><Grid size={{ xs: 12, md: 3 }}><TextField required fullWidth type="datetime-local" label="Starts" value={form.startsAtUtc} onChange={(e) => setForm({ ...form, startsAtUtc: e.target.value })} slotProps={{ inputLabel: { shrink: true } }} /></Grid><Grid size={{ xs: 12, md: 3 }}><TextField required fullWidth type="datetime-local" label="Ends" value={form.endsAtUtc} onChange={(e) => setForm({ ...form, endsAtUtc: e.target.value })} slotProps={{ inputLabel: { shrink: true } }} /></Grid><Grid size={{ xs: 12, md: 2 }}><TextField fullWidth type="number" label="Capacity" value={form.capacity} onChange={(e) => setForm({ ...form, capacity: e.target.value })} slotProps={{ htmlInput: { min: 1 } }} /></Grid><Grid size={{ xs: 12, md: 4 }}><TextField fullWidth label="Meeting URL" value={form.meetingUrl} onChange={(e) => setForm({ ...form, meetingUrl: e.target.value })} /></Grid><Grid size={{ xs: 12 }}><Button type="submit" variant="contained" disabled={saving} startIcon={saving ? <CircularProgress size={18} color="inherit" /> : <AddRoundedIcon />}>{language === 'ar' ? 'إنشاء الجلسة' : 'Create session'}</Button></Grid></Grid></Paper>
    <Grid container spacing={2}><Grid size={{ xs: 12, sm: 4 }}><StatCard label={language === 'ar' ? 'القادمة' : 'Upcoming'} value={upcoming.length} color="#102d5b" /></Grid><Grid size={{ xs: 12, sm: 4 }}><StatCard label={language === 'ar' ? 'إجمالي الحجوزات' : 'Total bookings'} value={sessions.reduce((sum, item) => sum + (item.attendees?.length ?? 0), 0)} color="#a47d28" /></Grid><Grid size={{ xs: 12, sm: 4 }}><StatCard label={language === 'ar' ? 'المنتهية' : 'Completed'} value={past.length} color="#66736a" /></Grid></Grid>
    <Paper sx={{ p: 2.2, borderRadius: 3.5 }}><Grid container spacing={1.5}><Grid size={{ xs: 12, md: 5 }}><TextField fullWidth size="small" label={language === 'ar' ? 'ابحث عن جلسة' : 'Search sessions'} value={search} onChange={(event) => setSearch(event.target.value)} slotProps={{ input: { startAdornment: <SearchRoundedIcon sx={{ mr: 1, color: 'text.disabled' }} /> } }} /></Grid><Grid size={{ xs: 6, md: 3.5 }}><TextField select fullWidth size="small" label={language === 'ar' ? 'الوقت' : 'Schedule'} value={view} onChange={(event) => setView(event.target.value)}><MenuItem value="upcoming">Upcoming</MenuItem><MenuItem value="live">In progress</MenuItem><MenuItem value="past">Past</MenuItem><MenuItem value="all">All sessions</MenuItem></TextField></Grid><Grid size={{ xs: 6, md: 3.5 }}><TextField select fullWidth size="small" label={language === 'ar' ? 'النوع' : 'Type'} value={typeFilter} onChange={(event) => setTypeFilter(event.target.value)}><MenuItem value="all">All types</MenuItem>{['coaching','group','workshop','webinar'].map((value) => <MenuItem key={value} value={value}>{humanize(value)}</MenuItem>)}</TextField></Grid></Grid></Paper>
    <Box><Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 2 }}><Typography variant="h5" fontWeight={900}>{language === 'ar' ? 'جدول الجلسات' : 'Session schedule'}</Typography><Chip label={`${visibleSessions.length} results`} /></Stack>{loading ? <CircularProgress /> : visibleSessions.length ? <Grid container spacing={2.5}>{visibleSessions.map((session) => { const remaining = session.capacity == null ? null : Math.max(0, session.capacity - session.attendees.length); const full = remaining === 0; return <Grid key={session.id} size={{ xs: 12, md: 6 }}><Paper sx={{ p: 3, borderRadius: 3.5, height: '100%', borderTop: '4px solid', borderColor: full ? 'warning.main' : '#102d5b', boxShadow: '0 14px 35px rgba(16,45,91,.06)' }}><Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center' }}><Chip label={session.sessionType} size="small" color="primary" variant="outlined" /><Chip label={full ? (language === 'ar' ? 'مكتملة' : 'Full') : `${remaining ?? '∞'} ${language === 'ar' ? 'متاح' : 'available'}`} size="small" color={full ? 'warning' : 'success'} /></Stack><Typography variant="h5" fontWeight={900} sx={{ mt: 2 }}>{language === 'ar' ? session.titleAr : session.titleEn}</Typography><Typography color="text.secondary" sx={{ mt: .7 }}>{new Date(session.startsAtUtc).toLocaleString(language === 'ar' ? 'ar-JO' : 'en-US')} – {new Date(session.endsAtUtc).toLocaleTimeString(language === 'ar' ? 'ar-JO' : 'en-US', { hour: '2-digit', minute: '2-digit' })}</Typography>{session.meetingUrl && <Button component="a" href={session.meetingUrl} target="_blank" size="small" sx={{ mt: 1.2, px: 0 }}>{language === 'ar' ? 'فتح رابط الاجتماع' : 'Open meeting link'}</Button>}<Divider sx={{ my: 2 }} /><Typography fontWeight={900} sx={{ mb: 1 }}>{language === 'ar' ? 'المسجلون' : `Booked clients (${session.attendees.length})`}</Typography>{session.attendees.length ? <Stack spacing={1}>{session.attendees.map((person) => <Box key={person.userId} sx={{ p: 1.5, borderRadius: 2, bgcolor: '#f5f6f2' }}><Typography fontWeight={800}>{person.name}</Typography><Typography variant="caption" color="text.secondary">{person.email}</Typography></Box>)}</Stack> : <Typography variant="body2" color="text.secondary">{language === 'ar' ? 'لا يوجد حجوزات بعد.' : 'No bookings yet.'}</Typography>}</Paper></Grid>; })}</Grid> : <Alert severity="info">No sessions match the selected filters.</Alert>}</Box>
  </Stack>;
}

function CourseManager({ courses, onChange, language }) {
  const [selectedIndex, setSelectedIndex] = useState(null);
  const [tab, setTab] = useState('details');
  const [students, setStudents] = useState([]);
  const [loadingStudents, setLoadingStudents] = useState(false);
  const [search, setSearch] = useState('');
  const [publishFilter, setPublishFilter] = useState('all');
  const [typeFilter, setTypeFilter] = useState('all');
  const course = selectedIndex === null ? null : courses[selectedIndex];
  const visibleCourses = courses.map((item, index) => ({ item, index })).filter(({ item }) => {
    const matchesSearch = `${item.title} ${item.slug} ${item.description ?? ''}`.toLowerCase().includes(search.trim().toLowerCase());
    const matchesPublish = publishFilter === 'all' || (publishFilter === 'published' ? item.isPublished : !item.isPublished);
    const matchesType = typeFilter === 'all' || item.type === typeFilter;
    return matchesSearch && matchesPublish && matchesType;
  });
  const updateCourse = (next) => onChange(courses.map((item, index) => index === selectedIndex ? next : item));

  useEffect(() => {
    if (tab !== 'students' || !course?.slug) return;
    setLoadingStudents(true);
    getCourseStudents(course.slug).then(setStudents).catch(() => setStudents([])).finally(() => setLoadingStudents(false));
  }, [tab, course?.slug]);

  if (!course) return <Stack spacing={3}>
    <Alert severity="info">Use the same slug in English and Arabic. Only published courses appear to clients.</Alert>
    <Paper sx={{ p: 2.2, borderRadius: 3.5, border: '1px solid #e3e2db' }}><Grid container spacing={1.5}><Grid size={{ xs: 12, md: 6 }}><TextField fullWidth size="small" label="Search courses" value={search} onChange={(event) => setSearch(event.target.value)} slotProps={{ input: { startAdornment: <SearchRoundedIcon sx={{ mr: 1, color: 'text.disabled' }} /> } }} /></Grid><Grid size={{ xs: 6, md: 3 }}><TextField select fullWidth size="small" label="Publishing" value={publishFilter} onChange={(event) => setPublishFilter(event.target.value)}><MenuItem value="all">All</MenuItem><MenuItem value="published">Published</MenuItem><MenuItem value="draft">Draft</MenuItem></TextField></Grid><Grid size={{ xs: 6, md: 3 }}><TextField select fullWidth size="small" label="Course type" value={typeFilter} onChange={(event) => setTypeFilter(event.target.value)}><MenuItem value="all">All types</MenuItem>{['course','cohort','library','coaching'].map((value) => <MenuItem key={value} value={value}>{humanize(value)}</MenuItem>)}</TextField></Grid></Grid><Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 1.2 }}>{visibleCourses.length} of {courses.length} courses</Typography></Paper>
    <Grid container spacing={2.5}>{visibleCourses.map(({ item, index }) => <Grid key={`${item.slug}-${index}`} size={{ xs: 12, md: 6 }}>
      <Paper variant="outlined" sx={{ overflow: 'hidden', borderRadius: 3, height: '100%' }}>
        <Box sx={{ height: 170, bgcolor: 'brand.navy', backgroundImage: item.imageUrl ? `linear-gradient(180deg, transparent, rgba(8,12,31,.55)), url(${item.imageUrl})` : 'none', backgroundSize: 'cover', backgroundPosition: 'center' }} />
        <Stack spacing={1.5} sx={{ p: 2.5 }}><Stack direction="row" justifyContent="space-between"><Chip size="small" label={item.isPublished ? 'Published' : 'Draft'} color={item.isPublished ? 'success' : 'default'} /><Typography fontWeight={900}>{item.price} {item.currency}</Typography></Stack><Typography variant="h5" fontWeight={900}>{item.title}</Typography><Typography color="text.secondary">{item.sections?.length ?? 0} sections · {(item.sections ?? []).reduce((sum, section) => sum + (section.lessons?.length ?? 0), 0)} lessons</Typography><Button variant="contained" onClick={() => { setSelectedIndex(index); setTab('details'); }}>Manage course</Button></Stack>
      </Paper>
    </Grid>)}</Grid>
    {!visibleCourses.length && <Alert severity="info">No courses match the selected search and filters.</Alert>}
    <Button variant="outlined" startIcon={<AddRoundedIcon />} onClick={() => { onChange([...courses, newArrayItem('courseOffers', null, courses.length)]); setSelectedIndex(courses.length); }} sx={{ alignSelf: 'flex-start' }}>Create new course</Button>
  </Stack>;

  const sections = course.sections ?? [];
  return <Paper sx={{ borderRadius: 3, overflow: 'hidden' }} dir={language === 'ar' ? 'rtl' : 'ltr'}>
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }} sx={{ p: 3, bgcolor: 'brand.navy', color: 'white' }}>
      <Button color="secondary" onClick={() => setSelectedIndex(null)} startIcon={<ArrowBackRoundedIcon />}>All courses</Button><Box sx={{ flexGrow: 1 }}><Typography variant="h5" fontWeight={900}>{course.title || 'Untitled course'}</Typography><Typography sx={{ opacity: .65 }}>{course.slug}</Typography></Box><Chip label={course.isPublished ? 'Published' : 'Draft'} color={course.isPublished ? 'success' : 'default'} />
    </Stack>
    <Tabs value={tab} onChange={(_, value) => setTab(value)} sx={{ px: 2, borderBottom: '1px solid', borderColor: 'divider' }}><Tab value="details" label="Course details" /><Tab value="curriculum" label={`Lessons (${sections.reduce((sum, section) => sum + (section.lessons?.length ?? 0), 0)})`} /><Tab value="students" label="Students" /></Tabs>
    <Box sx={{ p: { xs: 2.5, md: 4 } }}>
      {tab === 'details' && <Grid container spacing={2.5}>
        <Grid size={{ xs: 12, md: 8 }}><TextField fullWidth label="Course title" value={course.title} onChange={(e) => updateCourse({ ...course, title: e.target.value })} /></Grid>
        <Grid size={{ xs: 12, md: 4 }}><TextField fullWidth label="Slug" value={course.slug} onChange={(e) => updateCourse({ ...course, slug: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '-') })} /></Grid>
        <Grid size={{ xs: 12 }}><TextField fullWidth multiline minRows={3} label="Description" value={course.description} onChange={(e) => updateCourse({ ...course, description: e.target.value })} /></Grid>
        <Grid size={{ xs: 12, md: 3 }}><TextField select fullWidth label="Type" value={course.type ?? 'course'} onChange={(e) => updateCourse({ ...course, type: e.target.value })}>{['course', 'cohort', 'library', 'coaching'].map((value) => <MenuItem key={value} value={value}>{humanize(value)}</MenuItem>)}</TextField></Grid>
        <Grid size={{ xs: 6, md: 2 }}><TextField fullWidth type="number" label="Price" value={course.price ?? 0} onChange={(e) => updateCourse({ ...course, price: Number(e.target.value) })} /></Grid>
        <Grid size={{ xs: 6, md: 2 }}><TextField fullWidth label="Currency" value={course.currency ?? 'USD'} onChange={(e) => updateCourse({ ...course, currency: e.target.value.toUpperCase() })} /></Grid>
        <Grid size={{ xs: 8, md: 3 }}><TextField fullWidth label="Duration" value={course.duration ?? ''} onChange={(e) => updateCourse({ ...course, duration: e.target.value })} /></Grid>
        <Grid size={{ xs: 4, md: 2 }}><TextField fullWidth type="number" label="Order" value={course.sortOrder ?? 0} onChange={(e) => updateCourse({ ...course, sortOrder: Number(e.target.value) })} /></Grid>
        <Grid size={{ xs: 12 }}><Typography variant="overline" fontWeight={900} color="primary">Cover image</Typography><FieldEditor fieldKey="imageUrl" value={course.imageUrl ?? ''} onChange={(value) => updateCourse({ ...course, imageUrl: value })} /></Grid>
        <Grid size={{ xs: 12 }}><Paper variant="outlined" sx={{ p: 2 }}><Stack direction="row" justifyContent="space-between" alignItems="center"><Box><Typography fontWeight={900}>Publish course</Typography><Typography variant="body2" color="text.secondary">Published courses are visible in the client catalog and checkout.</Typography></Box><Switch checked={course.isPublished ?? true} onChange={(e) => updateCourse({ ...course, isPublished: e.target.checked })} /></Stack></Paper></Grid>
      </Grid>}
      {tab === 'curriculum' && <Stack spacing={2.5}>{sections.map((section, sectionIndex) => <Accordion key={section.key} defaultExpanded={sectionIndex === 0} disableGutters sx={{ border: '1px solid', borderColor: 'divider', borderRadius: '12px !important', '&:before': { display: 'none' } }}>
        <AccordionSummary expandIcon={<ExpandMoreRoundedIcon />}><Box sx={{ flexGrow: 1 }}><Typography fontWeight={900}>{section.title}</Typography><Typography variant="caption" color="text.secondary">{section.lessons?.length ?? 0} lessons</Typography></Box></AccordionSummary>
        <AccordionDetails><Stack spacing={2}>
          <Grid container spacing={2}><Grid size={{ xs: 12, md: 7 }}><TextField fullWidth label="Section title" value={section.title} onChange={(e) => updateCourse({ ...course, sections: sections.map((item, i) => i === sectionIndex ? { ...item, title: e.target.value } : item) })} /></Grid><Grid size={{ xs: 12, md: 3 }}><TextField fullWidth label="Section key" value={section.key} onChange={(e) => updateCourse({ ...course, sections: sections.map((item, i) => i === sectionIndex ? { ...item, key: e.target.value } : item) })} /></Grid><Grid size={{ xs: 12, md: 2 }}><TextField fullWidth type="number" label="Order" value={section.sortOrder} onChange={(e) => updateCourse({ ...course, sections: sections.map((item, i) => i === sectionIndex ? { ...item, sortOrder: Number(e.target.value) } : item) })} /></Grid><Grid size={{ xs: 12 }}><TextField fullWidth label="Description" value={section.description} onChange={(e) => updateCourse({ ...course, sections: sections.map((item, i) => i === sectionIndex ? { ...item, description: e.target.value } : item) })} /></Grid></Grid>
          {(section.lessons ?? []).map((lesson, lessonIndex) => <Paper key={lesson.key} variant="outlined" sx={{ p: 2.5, borderRadius: 3 }}><Stack direction="row" justifyContent="space-between" sx={{ mb: 2 }}><Typography fontWeight={900}>Lesson {lessonIndex + 1}</Typography><IconButton color="error" onClick={() => updateCourse({ ...course, sections: sections.map((item, i) => i === sectionIndex ? { ...item, lessons: item.lessons.filter((_, li) => li !== lessonIndex) } : item) })}><DeleteOutlineRoundedIcon /></IconButton></Stack><Grid container spacing={2}><Grid size={{ xs: 12, md: 6 }}><TextField fullWidth label="Lesson title" value={lesson.title} onChange={(e) => updateLesson(course, sections, sectionIndex, lessonIndex, { title: e.target.value }, updateCourse)} /></Grid><Grid size={{ xs: 7, md: 4 }}><TextField fullWidth label="Lesson key" value={lesson.key} onChange={(e) => updateLesson(course, sections, sectionIndex, lessonIndex, { key: e.target.value }, updateCourse)} /></Grid><Grid size={{ xs: 5, md: 2 }}><TextField fullWidth type="number" label="Minutes" value={lesson.durationMinutes} onChange={(e) => updateLesson(course, sections, sectionIndex, lessonIndex, { durationMinutes: Number(e.target.value) }, updateCourse)} /></Grid><Grid size={{ xs: 12 }}><FieldEditor fieldKey="videoUrl" value={lesson.videoUrl ?? ''} onChange={(value) => updateLesson(course, sections, sectionIndex, lessonIndex, { videoUrl: value }, updateCourse)} /></Grid></Grid></Paper>)}
          <Stack direction="row" spacing={1}><Button startIcon={<AddRoundedIcon />} variant="outlined" onClick={() => updateCourse({ ...course, sections: sections.map((item, i) => i === sectionIndex ? { ...item, lessons: [...(item.lessons ?? []), newArrayItem('course.sections.lessons')] } : item) })}>Add lesson</Button><Button color="error" onClick={() => updateCourse({ ...course, sections: sections.filter((_, i) => i !== sectionIndex) })}>Delete section</Button></Stack>
        </Stack></AccordionDetails>
      </Accordion>)}<Button startIcon={<AddRoundedIcon />} variant="contained" onClick={() => updateCourse({ ...course, sections: [...sections, newArrayItem('course.sections', null, sections.length)] })} sx={{ alignSelf: 'flex-start' }}>Add section</Button></Stack>}
      {tab === 'students' && (loadingStudents ? <CircularProgress /> : students.length ? <Stack spacing={1.5}>{students.map((student) => <Paper key={student.userId} variant="outlined" sx={{ p: 2.5, borderRadius: 3 }}><Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }}><Box sx={{ flexGrow: 1 }}><Typography fontWeight={900}>{student.name}</Typography><Typography variant="body2" color="text.secondary">{student.email} · Joined {new Date(student.joinedAtUtc).toLocaleDateString()}</Typography></Box><Box sx={{ minWidth: 180 }}><Stack direction="row" justifyContent="space-between"><Typography variant="caption">{student.completedLessons}/{student.totalLessons} lessons</Typography><Typography variant="caption" fontWeight={900}>{student.progressPercent}%</Typography></Stack><Box sx={{ mt: 1, height: 7, borderRadius: 5, bgcolor: 'divider', overflow: 'hidden' }}><Box sx={{ width: `${student.progressPercent}%`, height: 1, bgcolor: 'primary.main' }} /></Box></Box></Stack></Paper>)}</Stack> : <Alert severity="info">No clients have joined this course yet.</Alert>)}
    </Box>
  </Paper>;
}

function updateLesson(course, sections, sectionIndex, lessonIndex, changes, updateCourse) {
  updateCourse({ ...course, sections: sections.map((section, index) => index === sectionIndex ? { ...section, lessons: section.lessons.map((lesson, li) => li === lessonIndex ? { ...lesson, ...changes } : lesson) } : section) });
}

function RecursiveEditor({ value, onChange, path }) {
  if (Array.isArray(value)) {
    return (
      <Stack spacing={2}>
        {value.map((item, index) => (
          <Paper key={`${path}-${index}`} variant="outlined" sx={{ p: 2.5, borderRadius: 3, position: 'relative' }}>
            <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 2 }}>
              <Typography variant="subtitle2" fontWeight={900}>{humanize(path.split('.').at(-1))} #{index + 1}</Typography>
              <IconButton color="error" size="small" disabled={value.length === 1} onClick={() => onChange(value.filter((_, itemIndex) => itemIndex !== index))} aria-label="Remove item"><DeleteOutlineRoundedIcon /></IconButton>
            </Stack>
            <RecursiveEditor value={item} onChange={(nextItem) => onChange(value.map((current, itemIndex) => itemIndex === index ? nextItem : current))} path={`${path}.${index}`} />
          </Paper>
        ))}
        {path === 'customHtmlSections' ? <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
          {['html', 'image', 'video'].map((type) => <Button key={type} variant="outlined" startIcon={<AddRoundedIcon />} onClick={() => onChange([...value, newCustomSection(type)])}>Add {type} section</Button>)}
        </Stack> : <Button variant="outlined" startIcon={<AddRoundedIcon />} onClick={() => onChange([...value, newArrayItem(path, value[0], value.length)])} sx={{ alignSelf: 'flex-start' }}>Add item</Button>}
      </Stack>
    );
  }

  if (value && typeof value === 'object') {
    return (
      <Grid container spacing={2}>
        {Object.entries(value).map(([key, childValue]) => {
          const complex = childValue && typeof childValue === 'object';
          return (
            <Grid key={key} size={{ xs: 12, md: complex ? 12 : isLongField(key) ? 12 : 6 }}>
              {complex ? (
                <Box sx={{ mt: 1 }}><Typography variant="overline" color="primary" fontWeight={900}>{humanize(key)}</Typography><RecursiveEditor value={childValue} onChange={(nextValue) => onChange({ ...value, [key]: nextValue })} path={`${path}.${key}`} /></Box>
              ) : (
                <FieldEditor fieldKey={key} value={childValue} onChange={(nextValue) => onChange({ ...value, [key]: nextValue })} />
              )}
            </Grid>
          );
        })}
      </Grid>
    );
  }

  return <FieldEditor fieldKey={path.split('.').at(-1)} value={value} onChange={onChange} />;
}

function FieldEditor({ fieldKey, value, onChange }) {
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState('');
  const lowerKey = fieldKey.toLowerCase();
  const isImage = lowerKey.includes('imageurl');
  const isMedia = isImage || lowerKey.includes('videourl') || lowerKey.includes('mediaurl') || lowerKey.includes('posterurl');
  const isColor = ['primary', 'secondary', 'background', 'paper', 'textPrimary', 'textSecondary', 'navy', 'forest', 'cream', 'gold'].includes(fieldKey);

  if (typeof value === 'boolean') {
    return (
      <Paper variant="outlined" sx={{ px: 2, py: 1.2, borderRadius: 2.5 }}>
        <Stack direction="row" alignItems="center" justifyContent="space-between">
          <Box><Typography fontWeight={800}>{humanize(fieldKey)}</Typography><Typography variant="caption" color="text.secondary">Show this section on the homepage</Typography></Box>
          <Switch checked={value} onChange={(event) => onChange(event.target.checked)} />
        </Stack>
      </Paper>
    );
  }

  if (fieldKey === 'type') {
    return <TextField select fullWidth label="Section type" value={value} onChange={(event) => onChange(event.target.value)}><MenuItem value="html">HTML</MenuItem><MenuItem value="image">Image</MenuItem><MenuItem value="video">Video</MenuItem></TextField>;
  }

  if (fieldKey === 'layout') {
    return <TextField select fullWidth label="Layout" value={value} onChange={(event) => onChange(event.target.value)}><MenuItem value="media-right">Media right</MenuItem><MenuItem value="media-left">Media left</MenuItem><MenuItem value="full-bleed">Full width cinematic</MenuItem></TextField>;
  }

  if (fieldKey === 'theme') {
    return <TextField select fullWidth label="Section theme" value={value} onChange={(event) => onChange(event.target.value)}><MenuItem value="light">Light</MenuItem><MenuItem value="cream">Cream</MenuItem><MenuItem value="navy">Navy</MenuItem><MenuItem value="forest">Forest</MenuItem></TextField>;
  }

  if (fieldKey === 'audience') {
    return <TextField select fullWidth label="Visible on" value={value} onChange={(event) => onChange(event.target.value)}><MenuItem value="home">Home page</MenuItem><MenuItem value="client">Client page</MenuItem><MenuItem value="both">Home and client pages</MenuItem></TextField>;
  }

  const upload = async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;
    setUploading(true);
    setUploadError('');
    try { onChange(await uploadCmsAsset(file)); }
    catch (error) { setUploadError(error.response?.data?.message || error.message); }
    finally { setUploading(false); event.target.value = ''; }
  };

  return (
    <Stack spacing={1}>
      <Stack direction="row" spacing={1} alignItems="stretch">
        {isColor && <Box component="input" type="color" value={value} onChange={(event) => onChange(event.target.value)} aria-label={`${humanize(fieldKey)} color`} sx={{ width: 58, minHeight: 56, p: .5, border: '1px solid', borderColor: 'divider', borderRadius: 2, bgcolor: 'background.paper', cursor: 'pointer' }} />}
        <TextField
          fullWidth
          label={humanize(fieldKey)}
          value={value ?? ''}
          type={typeof value === 'number' ? 'number' : 'text'}
          multiline={isLongField(fieldKey)}
          minRows={isLongField(fieldKey) ? 3 : undefined}
          onChange={(event) => onChange(typeof value === 'number' ? Number(event.target.value) : event.target.value)}
        />
      </Stack>
      {isMedia && <Button component="label" variant="outlined" disabled={uploading} sx={{ alignSelf: 'flex-start' }}>{uploading ? 'Uploading…' : lowerKey.includes('video') || lowerKey.includes('media') ? 'Upload image or video' : 'Upload image'}<input hidden type="file" accept={lowerKey.includes('poster') || isImage ? 'image/*' : 'image/*,video/*'} onChange={upload} /></Button>}
      {uploadError && <Alert severity="error">{uploadError}</Alert>}
      {isMedia && value && (lowerKey.includes('video') || /\.(mp4|webm|mov)(\?|$)/i.test(value) ? <Box component="video" src={value} controls sx={{ width: 220, maxHeight: 140, borderRadius: 2 }} /> : <Box component="img" src={value} alt="CMS preview" sx={{ width: 220, height: 120, objectFit: 'cover', borderRadius: 2, border: '1px solid', borderColor: 'divider' }} />)}
    </Stack>
  );
}

function isLongField(key) {
  return ['body', 'summary', 'subtitle', 'description', 'quote', 'html'].some((name) => key.toLowerCase().includes(name));
}

function humanize(value) {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/[-_]/g, ' ').replace(/^./, (letter) => letter.toUpperCase());
}

function emptyLike(value) {
  if (Array.isArray(value)) return [];
  if (value && typeof value === 'object') return Object.fromEntries(Object.entries(value).map(([key, child]) => [key, emptyLike(child)]));
  if (typeof value === 'number') return 0;
  if (typeof value === 'boolean') return false;
  return '';
}

function newArrayItem(path, example, itemCount = 0) {
  if (path === 'customHtmlSections') {
    return newCustomSection('html');
  }
  if (path === 'courseOffers') return { slug: `course-${Date.now()}`, eyebrow: 'Online course', title: 'New course', description: '', imageUrl: '', benefits: ['Lifetime access'], paymentTitle: 'Complete your enrollment', paymentSubtitle: 'Enter your details to get started', fullNameLabel: 'Full name', emailLabel: 'Email address', cardLabel: 'Card details', payButtonLabel: 'Pay and enroll now', securePaymentText: 'Secure encrypted payment', lessons: [], type: 'course', price: 0, currency: 'USD', duration: 'Self-paced', isPublished: false, sortOrder: itemCount + 1, sections: [] };
  if (path.endsWith('.sections')) return { key: `section-${Date.now()}`, title: 'New section', description: '', sortOrder: itemCount + 1, lessons: [] };
  if (path.endsWith('.lessons')) return { key: `lesson-${Date.now()}`, title: 'New lesson', videoUrl: '', durationMinutes: 15 };
  if (path === 'stories') return { key: `story-${Date.now()}`, title: 'New story', caption: '', videoUrl: '', posterUrl: '', audience: 'both', order: itemCount + 1, isVisible: true };
  return emptyLike(example);
}

function newCustomSection(type) {
  return {
    key: `custom-${type}-${Date.now()}`,
    title: `New ${type} section`,
    eyebrow: 'Featured story',
    body: 'Add a concise description that explains why this content matters.',
    html: type === 'html' ? '<section><h2>New section</h2><p>Add your content here.</p></section>' : '',
    order: 1,
    isVisible: true,
    type,
    mediaUrl: '',
    posterUrl: '',
    altText: '',
    layout: 'media-right',
    theme: 'light',
    autoplay: false,
    muted: true,
    loop: false,
    ctaLabel: '',
    ctaUrl: '',
  };
}
