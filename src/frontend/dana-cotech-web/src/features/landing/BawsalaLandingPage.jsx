import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded';
import CalendarMonthRoundedIcon from '@mui/icons-material/CalendarMonthRounded';
import CheckCircleOutlineRoundedIcon from '@mui/icons-material/CheckCircleOutlineRounded';
import LanguageRoundedIcon from '@mui/icons-material/LanguageRounded';
import LoginRoundedIcon from '@mui/icons-material/LoginRounded';
import MenuRoundedIcon from '@mui/icons-material/MenuRounded';
import PlayArrowRoundedIcon from '@mui/icons-material/PlayArrowRounded';
import SchoolRoundedIcon from '@mui/icons-material/SchoolRounded';
import ExploreRoundedIcon from '@mui/icons-material/ExploreRounded';
import DashboardRoundedIcon from '@mui/icons-material/DashboardRounded';
import { AppBar, Box, Button, Card, Container, Grid, IconButton, Stack, Toolbar, Typography } from '@mui/material';
import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { ThemeProvider } from '@mui/material/styles';
import { getDirection, oppositeLanguage } from '../../i18n/languages';
import { createAppTheme } from '../../theme/theme';
import { useLandingContent } from './useLandingContent';
import { getSession } from '../platform/platformApi';

const fallbackPrograms = [
  { slug: 'career-clarity-program', title: 'Career Clarity Program', summary: 'A guided journey through strengths, interests, values, purpose, and a personal career profile.', cta: { label: 'Discover Career Clarity' } },
  { slug: 'one-to-one-coaching', title: 'Career Coaching', summary: 'Book a focused conversation with Dana for direction, decisions, and an actionable plan.', cta: { label: 'View available sessions' } },
];

const fallbackContent = {
  appearance: { theme: null },
  hero: {
    subtitle: 'Build a career direction that reflects who you are, what matters to you, and where you want to go.',
    imageUrl: '',
    videoUrl: '',
  },
  programs: fallbackPrograms,
  stories: [],
  about: {
    body: 'I help professionals and young people turn uncertainty into a clear, practical career direction—one thoughtful step at a time.',
    imageUrl: '',
  },
};

export function BawsalaLandingPage({ language }) {
  const { i18n } = useTranslation();
  const { content: remoteContent } = useLandingContent(language);
  const content = remoteContent ?? fallbackContent;
  const direction = getDirection(language);
  const theme = useMemo(() => createAppTheme(direction, content?.appearance?.theme ?? undefined), [direction, content?.appearance?.theme]);
  const ar = language === 'ar';
  const session = getSession();
  const accountUrl = session?.role === 'Admin' ? '/cms' : '/dashboard';

  const journeyServices = ['one-to-one-coaching', 'career-clarity-program'];
  const configuredPrograms = content.programs?.length ? content.programs : fallbackPrograms;
  const programs = journeyServices.map((slug) => configuredPrograms.find((program) => program.slug === slug) ?? fallbackPrograms.find((program) => program.slug === slug)).filter(Boolean);
  const stories = (content.stories ?? []).filter((story) => story.isVisible && ['home', 'both'].includes(story.audience?.toLowerCase())).slice(0, 4);
  const hero = content.hero;

  return <ThemeProvider theme={theme}>
    <Box dir={direction} sx={{ bgcolor: '#fffdf6', color: '#152013', minHeight: '100vh' }}>
      <AppBar position="sticky" elevation={0} sx={{ bgcolor: 'rgba(255,255,255,.96)', color: '#152013', borderBottom: '1px solid #e6e2d9', backdropFilter: 'blur(16px)' }}>
        <Container maxWidth="xl"><Toolbar disableGutters sx={{ minHeight: 76, gap: 2 }}>
          <BrandLogo compact />
          <Stack direction="row" spacing={3} sx={{ ml: 'auto', display: { xs: 'none', md: 'flex' } }}>
            <Button href="#programs" color="inherit">{ar ? 'البرامج' : 'Programs'}</Button>
            <Button href="#insights" color="inherit">{ar ? 'أفكار' : 'Insights'}</Button>
            <Button href="#about" color="inherit">{ar ? 'عن دانا' : 'About Dana'}</Button>
            <Button href={session ? accountUrl : '/login'} color="inherit" startIcon={session ? <DashboardRoundedIcon /> : <LoginRoundedIcon />}>{session ? (ar ? 'لوحة التحكم' : 'My dashboard') : (ar ? 'دخول' : 'Log in')}</Button>
          </Stack>
          <IconButton onClick={() => i18n.changeLanguage(oppositeLanguage(language))} aria-label="Switch language"><LanguageRoundedIcon /></IconButton>
          <Button href="/booking" variant="contained" sx={{ bgcolor: '#102d5b', display: { xs: 'none', sm: 'inline-flex' } }}>{ar ? 'احجز جلسة' : 'Book a session'}</Button>
          <IconButton component="a" href={session ? accountUrl : '/login'} aria-label={session ? 'Open dashboard' : 'Log in'} sx={{ display: { md: 'none' }, color: '#102d5b' }}>{session ? <DashboardRoundedIcon /> : <MenuRoundedIcon />}</IconButton>
        </Toolbar></Container>
      </AppBar>

      <Box component="main">
        <Box sx={{ background: 'linear-gradient(120deg,#f4f6ef 0%,#fffdf6 54%,#eeedf5 100%)' }}>
          <Container maxWidth="xl" sx={{ py: { xs: 7, md: 10 } }}>
            <Grid container spacing={{ xs: 6, md: 8 }} alignItems="center">
              <Grid size={{ xs: 12, md: 6.3 }}>
                <Stack spacing={3} alignItems="flex-start">
                  <Eyebrow>{ar ? 'وضوح مهني مصمم حولك' : 'Career clarity, built around you'}</Eyebrow>
                  <Typography component="h1" sx={{ fontFamily: 'Georgia,serif', fontWeight: 500, fontSize: { xs: 52, md: 78 }, lineHeight: .98, letterSpacing: '-.045em', maxWidth: 760 }}>
                    {ar ? 'تقدم بوضوح، لا بالتخمين.' : <>Move forward with <Box component="span" sx={{ color: '#2f4f23', fontStyle: 'italic' }}>clarity</Box>, not guesswork.</>}
                  </Typography>
                  <Typography sx={{ maxWidth: 650, color: '#59635b', fontSize: { xs: 17, md: 19 }, lineHeight: 1.8 }}>{hero.subtitle}</Typography>
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                    <Button href="/programs" variant="contained" size="large" endIcon={<ArrowForwardRoundedIcon />} sx={{ bgcolor: '#102d5b' }}>{ar ? 'استكشف البرامج' : 'Explore programs'}</Button>
                    <Button href="/booking" variant="outlined" size="large" startIcon={<CalendarMonthRoundedIcon />} sx={{ color: '#2f4f23', borderColor: '#2f4f23', bgcolor: 'white' }}>{ar ? 'احجز جلسة' : 'Book a session'}</Button>
                  </Stack>
                  <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2.5} useFlexGap sx={{ flexWrap: 'wrap' }}>
                    {[ar ? 'تقييمات مبنية على أسس علمية' : 'Evidence-based assessments', ar ? 'توجيه شخصي' : 'Personal guidance', ar ? 'العربية والإنجليزية' : 'English & Arabic'].map((item) => <Stack key={item} direction="row" spacing={.7} alignItems="center"><CheckCircleOutlineRoundedIcon sx={{ fontSize: 18, color: '#2f4f23' }} /><Typography variant="body2" color="text.secondary">{item}</Typography></Stack>)}
                  </Stack>
                </Stack>
              </Grid>
              <Grid size={{ xs: 12, md: 5.7 }}>
                <Box sx={{ position: 'relative', minHeight: { xs: 430, md: 560 } }}>
                  <Box sx={{ position: 'absolute', inset: '0 40px 30px 0', overflow: 'hidden', borderRadius: '220px 220px 30px 30px', bgcolor: '#dce4d7', boxShadow: '0 30px 75px rgba(47,79,35,.2)' }}>
                    {(hero.videoUrl || hero.imageUrl) ? <Box component={hero.videoUrl ? 'video' : 'img'} src={hero.videoUrl || hero.imageUrl} poster={hero.imageUrl} autoPlay muted loop playsInline sx={{ width: 1, height: 1, objectFit: 'cover' }} /> : <Box sx={{ width: 1, height: 1, display: 'grid', placeItems: 'center', background: 'radial-gradient(circle at 48% 38%,#fff 0 12%,transparent 13%),linear-gradient(145deg,#dce4d7,#9dac96)' }}><Box component="img" src="/assets/bawsala-logo.jpg" alt="Bawsala" sx={{ width: '74%', mixBlendMode: 'multiply', opacity: .78 }} /></Box>}
                  </Box>
                  <Card sx={{ position: 'absolute', right: 0, bottom: 0, width: 240, p: 2.2, borderRadius: 3, boxShadow: '0 18px 45px rgba(32,41,91,.16)' }}><Typography fontWeight={900} color="#102d5b">{ar ? 'خطوتك التالية، أوضح.' : 'Your next step, made clearer.'}</Typography><Typography variant="body2" color="text.secondary" sx={{ mt: .5 }}>{ar ? 'ابدأ ببرنامج أو تقييم أو محادثة.' : 'Start with a program, an assessment, or a conversation.'}</Typography></Card>
                </Box>
              </Grid>
            </Grid>
          </Container>
        </Box>

        <Box sx={{ bgcolor: '#102d5b', color: 'white' }}><Container maxWidth="xl"><Grid container>{[
          ['4', ar ? 'تقييمات مهنية موجهة' : 'Guided career assessments'], ['1:1', ar ? 'جلسات إرشاد شخصية' : 'Personal coaching sessions'], ['EN + AR', ar ? 'تجربة ثنائية اللغة' : 'Bilingual experience'], [ar ? 'بوقتك' : 'Your pace', ar ? 'تعلم وتقدم بمرونة' : 'Learn and progress flexibly'],
        ].map(([value, label]) => <Grid key={value} size={{ xs: 6, md: 3 }} sx={{ p: 2.5, borderInlineEnd: '1px solid rgba(255,255,255,.12)' }}><Typography sx={{ color: '#c69a55', fontFamily: 'Georgia,serif', fontSize: 28 }}>{value}</Typography><Typography variant="caption" sx={{ color: 'rgba(255,255,255,.68)' }}>{label}</Typography></Grid>)}</Grid></Container></Box>

        <Container id="programs" maxWidth="xl" sx={{ py: { xs: 8, md: 11 } }}>
          <SectionIntro eyebrow={ar ? 'اختر نقطة البداية' : 'Choose your starting point'} title={ar ? 'دعم لكل مرحلة مهنية.' : 'Support for every career chapter.'} body={ar ? 'خياران واضحان: احجز جلسة مباشرة أو ابدأ رحلة وضوح المسار الكاملة.' : 'Two clear options: book a focused coaching session or begin the complete Career Clarity journey.'} />
          <Grid container spacing={2.5}>{programs.map((program, index) => <Grid key={program.slug} size={{ xs: 12, md: 6 }}><ProgramCard program={program} index={index} ar={ar} /></Grid>)}</Grid>
        </Container>

        <Box id="insights" sx={{ bgcolor: '#f4f5ef', py: { xs: 8, md: 11 } }}><Container maxWidth="xl">
          <SectionIntro eyebrow={ar ? 'منظورات سريعة' : 'Quick perspectives'} title={ar ? 'أفكار قصيرة من دانا.' : 'Short insights from Dana.'} body={ar ? 'أفكار مفيدة يمكنك تطبيقها فوراً.' : 'Useful ideas you can watch in a few minutes and apply immediately.'} />
          <Grid container spacing={2}>{(stories.length ? stories : Array.from({ length: 4 }, (_, i) => ({ key: i, title: ['When your career feels stuck', 'Strengths are not job titles', 'Choosing without pressure', 'Your next step can be small'][i] }))).map((story, index) => <Grid key={story.key} size={{ xs: 6, md: 3 }}><StoryCard story={story} index={index} /></Grid>)}</Grid>
        </Container></Box>

        <Container id="about" maxWidth="xl" sx={{ py: { xs: 8, md: 11 } }}><Grid container spacing={7} alignItems="center">
          <Grid size={{ xs: 12, md: 5 }}><Box sx={{ minHeight: 430, borderRadius: 5, overflow: 'hidden', bgcolor: '#eef2e9', backgroundImage: `url(${content.about?.imageUrl || hero.imageUrl})`, backgroundSize: 'cover', backgroundPosition: 'center' }} /></Grid>
          <Grid size={{ xs: 12, md: 7 }}><Eyebrow>{ar ? 'تعرف إلى مدربتك المهنية' : 'Meet your career coach'}</Eyebrow><Typography variant="h2" sx={{ fontFamily: 'Georgia,serif', fontWeight: 500, mt: 2 }}>{ar ? 'مرحباً، أنا دانا غرايبة.' : 'Hi, I’m Dana Gharaibeh.'}</Typography><Typography sx={{ mt: 2, color: 'text.secondary', fontSize: 18, lineHeight: 1.8 }}>{content.about?.body}</Typography><Box sx={{ my: 3, pl: 2.5, borderLeft: '3px solid #c69a55' }}><Typography sx={{ fontFamily: 'Georgia,serif', fontSize: 22, color: '#2f4f23' }}>{ar ? 'نصنع الوضوح من خلال الأسئلة الصحيحة، والتأمل، والعمل.' : '“Clarity is built through the right questions, reflection, and action.”'}</Typography></Box><Button href="#" variant="outlined">{ar ? 'اعرف المزيد عن دانا' : 'Learn more about Dana'}</Button></Grid>
        </Grid></Container>

        <Container maxWidth="xl" sx={{ pb: 7 }}><Box sx={{ p: { xs: 4, md: 5 }, borderRadius: 5, color: 'white', background: 'linear-gradient(110deg,#102d5b,#2f4f23)', display: 'flex', flexDirection: { xs: 'column', md: 'row' }, alignItems: { md: 'center' }, justifyContent: 'space-between', gap: 3 }}><Box><Typography variant="h3" sx={{ fontFamily: 'Georgia,serif', color: 'white' }}>{ar ? 'جاهز لتوضيح خطوتك القادمة؟' : 'Ready to make your next move clearer?'}</Typography><Typography sx={{ mt: 1, color: 'rgba(255,255,255,.72)' }}>{ar ? 'ابدأ برنامج الوضوح المهني أو احجز جلسة شخصية.' : 'Start the Career Clarity Program or book a personal session with Dana.'}</Typography></Box><Button href="/programs" variant="contained" size="large" sx={{ bgcolor: '#f3dfb8', color: '#102d5b', whiteSpace: 'nowrap' }}>{ar ? 'ابدأ رحلتك' : 'Start your journey'} →</Button></Box></Container>

        <Box component="footer" sx={{ bgcolor: 'white', borderTop: '1px solid #e6e2d9' }}><Container maxWidth="xl" sx={{ py: 3, display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 2 }}><BrandLogo compact /><Typography variant="body2" color="text.secondary">© {new Date().getFullYear()} Bawsala</Typography></Container></Box>
      </Box>
    </Box>
  </ThemeProvider>;
}

export function BrandLogo({ compact = false }) {
  return <Box component="a" href="/" aria-label="Bawsala home" sx={{ display: 'flex', alignItems: 'center', flexShrink: 0, textDecoration: 'none', color: '#102d5b', transition: 'transform .22s ease', transformOrigin: 'left center', '&:hover': { transform: 'translateY(-1px) scale(1.02)' } }}><Box component="img" src="/assets/bawsala-logo-horizontal.png" alt="Bawsala Consultancy" sx={{ width: compact ? { xs: 144, sm: 200 } : { xs: 196, sm: 250 }, height: compact ? { xs: 48, sm: 64 } : { xs: 68, sm: 86 }, objectFit: 'contain', display: 'block' }} /></Box>;
}

function Eyebrow({ children }) { return <Typography sx={{ display: 'flex', alignItems: 'center', gap: 1, color: '#2f4f23', fontSize: 12, fontWeight: 900, letterSpacing: '.12em', textTransform: 'uppercase', '&:before': { content: '""', width: 28, height: 2, bgcolor: '#c69a55' } }}>{children}</Typography>; }
function SectionIntro({ eyebrow, title, body }) { return <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mb: 4, justifyContent: 'space-between', alignItems: { md: 'flex-end' } }}><Box><Eyebrow>{eyebrow}</Eyebrow><Typography variant="h2" sx={{ mt: 1.2, fontFamily: 'Georgia,serif', fontWeight: 500 }}>{title}</Typography></Box><Typography color="text.secondary" sx={{ maxWidth: 440 }}>{body}</Typography></Stack>; }
function ProgramCard({ program, index, ar }) { const Icon = index === 0 ? ExploreRoundedIcon : index === 1 ? SchoolRoundedIcon : CalendarMonthRoundedIcon; return <Card sx={{ height: '100%', minHeight: 300, p: 3, border: index === 0 ? 0 : '1px solid #e1e2da', color: index === 0 ? 'white' : '#152013', background: index === 0 ? 'linear-gradient(145deg,#102d5b,#2f4f23)' : 'white', display: 'flex', flexDirection: 'column', justifyContent: 'space-between' }}><Box><Box sx={{ width: 44, height: 44, borderRadius: 2.5, display: 'grid', placeItems: 'center', bgcolor: index === 0 ? '#f3dfb8' : '#102d5b', color: index === 0 ? '#102d5b' : 'white' }}><Icon /></Box><Typography variant="h5" sx={{ mt: 2.2, fontWeight: 800 }}>{program.title}</Typography><Typography sx={{ mt: 1, lineHeight: 1.7, color: index === 0 ? 'rgba(255,255,255,.72)' : 'text.secondary' }}>{program.summary}</Typography></Box><Button href={program.slug === 'one-to-one-coaching' ? '/booking' : `/checkout/${program.slug}`} endIcon={<ArrowForwardRoundedIcon />} sx={{ justifyContent: 'flex-start', px: 0, color: index === 0 ? '#f3dfb8' : '#2f4f23' }}>{program.cta?.label || (ar ? 'اعرف المزيد' : 'Learn more')}</Button></Card>; }
function StoryCard({ story, index }) { const gradients = ['#314b36,#91a08a,#d8d1c3', '#20295b,#817c91,#ddd6c9', '#294735,#829583,#dce2d7', '#20295b,#777b9c,#ded9e7']; return <Box sx={{ position: 'relative', minHeight: 370, borderRadius: 4, overflow: 'hidden', bgcolor: '#263e2d', background: `linear-gradient(165deg,${gradients[index % gradients.length]})`, backgroundImage: story.posterUrl ? `linear-gradient(180deg,transparent 38%,rgba(9,14,25,.88)),url(${story.posterUrl})` : undefined, backgroundSize: 'cover', backgroundPosition: 'center' }}><IconButton sx={{ position: 'absolute', top: 15, right: 15, bgcolor: 'rgba(255,255,255,.9)', color: '#102d5b' }}><PlayArrowRoundedIcon /></IconButton><Box sx={{ position: 'absolute', insetInline: 18, bottom: 18, color: 'white' }}><Typography fontWeight={900}>{story.title}</Typography><Typography variant="caption" sx={{ color: 'rgba(255,255,255,.68)' }}>{story.caption || '2 min watch'}</Typography></Box></Box>; }
