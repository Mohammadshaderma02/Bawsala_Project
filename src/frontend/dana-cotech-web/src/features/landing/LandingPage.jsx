import ArrowForwardRoundedIcon from '@mui/icons-material/ArrowForwardRounded';
import AutoAwesomeRoundedIcon from '@mui/icons-material/AutoAwesomeRounded';
import CalendarMonthRoundedIcon from '@mui/icons-material/CalendarMonthRounded';
import CheckRoundedIcon from '@mui/icons-material/CheckRounded';
import EmailRoundedIcon from '@mui/icons-material/EmailRounded';
import LanguageRoundedIcon from '@mui/icons-material/LanguageRounded';
import LoginRoundedIcon from '@mui/icons-material/LoginRounded';
import MenuRoundedIcon from '@mui/icons-material/MenuRounded';
import CloseRoundedIcon from '@mui/icons-material/CloseRounded';
import NorthEastRoundedIcon from '@mui/icons-material/NorthEastRounded';
import PlayArrowRoundedIcon from '@mui/icons-material/PlayArrowRounded';
import PauseRoundedIcon from '@mui/icons-material/PauseRounded';
import VolumeOffRoundedIcon from '@mui/icons-material/VolumeOffRounded';
import VolumeUpRoundedIcon from '@mui/icons-material/VolumeUpRounded';
import QuoteRoundedIcon from '@mui/icons-material/FormatQuoteRounded';
import {
  Alert,
  AppBar,
  Avatar,
  Box,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Container,
  Divider,
  Dialog,
  DialogContent,
  Drawer,
  Grid,
  IconButton,
  Stack,
  TextField,
  Toolbar,
  Tooltip,
  Typography,
} from '@mui/material';
import { useEffect, useMemo, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ThemeProvider } from '@mui/material/styles';
import { getDirection, oppositeLanguage } from '../../i18n/languages';
import { createAppTheme } from '../../theme/theme';
import { useLandingContent } from './useLandingContent';

export function LandingPage({ language }) {
  const { t, i18n } = useTranslation();
  const { content, isLoading, error } = useLandingContent(language);
  const direction = getDirection(language);
  const pageTheme = useMemo(() => createAppTheme(direction, content?.appearance?.theme), [direction, content?.appearance?.theme]);
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  useScrollReveal(content);

  const switchLanguage = async () => i18n.changeLanguage(oppositeLanguage(language));

  if (isLoading) {
    return (
      <Box sx={{ display: 'grid', minHeight: '100vh', placeItems: 'center', bgcolor: 'brand.cream' }}>
        <Stack spacing={2} alignItems="center">
          <Box component="img" src="/assets/logo-dana-gharaibeh.svg" alt="Dana Gharaibeh" sx={{ width: 190 }} />
          <CircularProgress size={28} thickness={4} />
        </Stack>
      </Box>
    );
  }

  if (error || !content) {
    return <Container sx={{ py: 8 }}><Alert severity="error">{error ?? t('errors.empty')}</Alert></Container>;
  }

  const uiText = content.uiText ?? createFallbackUiText(t);
  const sections = content.appearance?.sections ?? createDefaultVisibility();

  const navItems = [
    { label: uiText.navPrograms, href: '#programs', visible: sections.programs },
    { label: uiText.navExperience, href: '#experience', visible: sections.whyDana },
    { label: uiText.navAbout, href: '#about', visible: sections.about },
  ].filter((item) => item.visible);

  return (
    <ThemeProvider theme={pageTheme}>
    <Box dir={direction} sx={{ overflow: 'hidden', bgcolor: 'background.default' }}>
      <AppBar
        position="fixed"
        elevation={0}
        sx={{ display: sections.header ? 'block' : 'none', bgcolor: (theme) => `color-mix(in srgb, ${theme.palette.background.paper} 82%, transparent)`, color: 'text.primary', borderBottom: '1px solid', borderColor: 'divider', backdropFilter: 'blur(22px) saturate(150%)' }}
      >
        <Container maxWidth="xl">
          <Toolbar disableGutters sx={{ minHeight: { xs: 72, md: 86 }, gap: 3 }}>
            <Box component="a" href="#top" sx={{ display: 'flex', flexGrow: { xs: 1, md: 0 } }}>
              <Box component="img" src="/assets/logo-dana-gharaibeh.svg" alt="Dana Gharaibeh" sx={{ width: { xs: 132, md: 162 }, height: 58, objectFit: 'contain' }} />
            </Box>

            <Stack direction="row" spacing={.5} sx={{ mx: 'auto', p: .6, display: { xs: 'none', md: 'flex' }, bgcolor: (theme) => `color-mix(in srgb, ${theme.palette.primary.main} 6%, transparent)`, borderRadius: 99 }}>
              {navItems.map((item) => (
                <Typography key={item.href} component="a" href={item.href} variant="body2" sx={{ px: 2, py: 1, borderRadius: 99, fontWeight: 800, color: 'text.secondary', transition: '.25s ease', '&:hover': { color: 'primary.main', bgcolor: 'background.paper', boxShadow: (theme) => `0 8px 22px color-mix(in srgb, ${theme.palette.primary.main} 10%, transparent)` } }}>
                  {item.label}
                </Typography>
              ))}
            </Stack>

            <Stack direction="row" spacing={1} alignItems="center">
              <IconButton onClick={() => setMobileMenuOpen(true)} aria-label="Open navigation" sx={{ display: { md: 'none' }, border: '1px solid', borderColor: 'divider', borderRadius: 3 }}>
                <MenuRoundedIcon fontSize="small" />
              </IconButton>
              <Tooltip title={t('actions.switchLanguage')}>
                <IconButton onClick={switchLanguage} sx={{ border: '1px solid', borderColor: 'divider', borderRadius: 3 }}>
                  <LanguageRoundedIcon fontSize="small" />
                </IconButton>
              </Tooltip>
              <Button
                href="/login"
                variant="contained"
                aria-label={t('actions.login')}
                startIcon={<LoginRoundedIcon />}
                sx={{
                  minWidth: { xs: 46, sm: 'auto' },
                  px: { xs: 1.25, sm: 2.75 },
                  gap: { xs: 0, sm: 1 },
                  bgcolor: 'brand.navy', boxShadow: (theme) => `0 12px 28px color-mix(in srgb, ${theme.palette.brand.navy} 24%, transparent)`,
                  '& .MuiButton-startIcon': { m: 0 },
                  '&:hover': { bgcolor: 'brand.forest' },
                }}
              >
                <Box component="span" sx={{ display: { xs: 'none', sm: 'inline' } }}>
                  {t('actions.login')}
                </Box>
              </Button>
            </Stack>
          </Toolbar>
        </Container>
      </AppBar>
      <ScrollProgress top={sections.header ? { xs: 72, md: 86 } : 0} />
      <Drawer anchor={direction === 'rtl' ? 'right' : 'left'} open={mobileMenuOpen} onClose={() => setMobileMenuOpen(false)} PaperProps={{ sx: { width: 'min(88vw, 360px)', p: 3, bgcolor: 'brand.cream' } }}>
        <Stack spacing={3} sx={{ height: 1 }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Box component="img" src="/assets/logo-dana-gharaibeh.svg" alt="Dana Gharaibeh" sx={{ width: 150 }} />
            <IconButton onClick={() => setMobileMenuOpen(false)} aria-label="Close navigation"><CloseRoundedIcon /></IconButton>
          </Stack>
          <Divider />
          <Stack spacing={1}>
            {navItems.map((item, index) => <Button key={item.href} href={item.href} onClick={() => setMobileMenuOpen(false)} color="inherit" sx={{ justifyContent: 'space-between', py: 1.5, fontSize: 18 }} endIcon={<Typography color="text.disabled">0{index + 1}</Typography>}>{item.label}</Button>)}
          </Stack>
          <Box sx={{ flexGrow: 1 }} />
          <Button href="/programs" variant="contained" size="large" endIcon={<ArrowForwardRoundedIcon />}>{content.hero.primaryCta.label}</Button>
        </Stack>
      </Drawer>

      <Box component="main" id="top" sx={{ bgcolor: 'background.default', pt: sections.header ? { xs: '72px', md: '86px' } : 0 }}>
        <Box sx={{ display: sections.hero ? 'block' : 'none', px: { xs: 1.25, md: 2.5 }, pt: { xs: 1.25, md: 2.5 } }}>
          <Box
            sx={{
              position: 'relative', isolation: 'isolate', overflow: 'hidden', maxWidth: 1536, mx: 'auto',
              minHeight: { xs: 760, md: 760 }, borderRadius: { xs: 4, md: 8 }, color: 'common.white',
              background: (theme) => `linear-gradient(128deg, ${theme.palette.brand.navy} 0%, color-mix(in srgb, ${theme.palette.brand.navy} 80%, ${theme.palette.brand.forest}) 58%, ${theme.palette.brand.forest} 145%)`,
              boxShadow: (theme) => `0 38px 100px color-mix(in srgb, ${theme.palette.brand.navy} 28%, transparent)`,
            }}
          >
            <Box sx={{ position: 'absolute', inset: 0, background: (theme) => `radial-gradient(circle at 14% 18%, color-mix(in srgb, ${theme.palette.secondary.main} 28%, transparent), transparent 31%), radial-gradient(circle at 84% 82%, color-mix(in srgb, ${theme.palette.brand.gold} 24%, transparent), transparent 30%)` }} />
            <Box sx={{ position: 'absolute', inset: 0, opacity: .18, backgroundImage: 'linear-gradient(rgba(255,255,255,.05) 1px, transparent 1px), linear-gradient(90deg, rgba(255,255,255,.05) 1px, transparent 1px)', backgroundSize: '72px 72px', maskImage: 'linear-gradient(to bottom, black, transparent 82%)' }} />
            <Box sx={{ position: 'absolute', width: 420, height: 420, border: '1px solid rgba(255,255,255,.08)', borderRadius: '50%', top: -210, left: '35%' }} />
            <Container maxWidth="xl" sx={{ position: 'relative', height: '100%' }}>
              <Grid container spacing={{ xs: 5, md: 7 }} sx={{ minHeight: { xs: 760, md: 760 }, py: { xs: 7, md: 6 }, alignItems: 'center' }}>
                <Grid size={{ xs: 12, md: 6.5 }}>
                  <Stack spacing={3.5} className="reveal-up" sx={{ maxWidth: 820 }}>
                    <Eyebrow light>{content.hero.eyebrow}</Eyebrow>
                    <Typography variant="h1" sx={{ maxWidth: 820, textWrap: 'balance' }}>{content.hero.title}</Typography>
                    <Typography sx={{ maxWidth: 680, color: 'rgba(255,255,255,.72)', fontSize: { xs: 18, md: 21 }, lineHeight: 1.75 }}>
                      {content.hero.subtitle}
                    </Typography>
                    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                      <Button href={content.hero.primaryCta.url} size="large" variant="contained" color="secondary" endIcon={<ArrowForwardRoundedIcon />}>
                        {content.hero.primaryCta.label}
                      </Button>
                      <Button href={content.hero.secondaryCta.url} size="large" variant="outlined" color="inherit" startIcon={<CalendarMonthRoundedIcon />}>
                        {content.hero.secondaryCta.label}
                      </Button>
                    </Stack>
                    <Stack direction="row" spacing={{ xs: 2.5, sm: 5 }} sx={{ pt: 2 }}>
                      <HeroMetric value="01:01" label={uiText.personalLabel} />
                      <HeroMetric value="360°" label={uiText.growthLabel} />
                      <HeroMetric value="03" label={uiText.pathsLabel} />
                    </Stack>
                  </Stack>
                </Grid>
                <Grid size={{ xs: 12, md: 5.5 }}>
                  <Box sx={{ position: 'relative', maxWidth: 560, mx: 'auto', transform: { md: 'rotate(1.5deg)' }, transition: 'transform .6s cubic-bezier(.2,.8,.2,1)', '&:hover': { transform: { md: 'rotate(0deg) translateY(-6px)' } } }}>
                    <HeroMedia hero={content.hero} />
                    <Box sx={{ position: 'absolute', bottom: 26, insetInlineStart: { xs: 12, md: -42 }, p: 2, pr: 3, borderRadius: 3, bgcolor: (theme) => `color-mix(in srgb, ${theme.palette.background.paper} 92%, transparent)`, color: 'brand.navy', boxShadow: '0 24px 60px rgba(0,0,0,.28)', backdropFilter: 'blur(16px)' }}>
                      <Stack direction="row" spacing={1.5} alignItems="center">
                        <Avatar sx={{ bgcolor: 'secondary.main', color: 'brand.navy' }}><AutoAwesomeRoundedIcon /></Avatar>
                        <Box><Typography variant="caption" color="text.secondary">{uiText.nextChapterLabel}</Typography><Typography fontWeight={900}>{uiText.startsHereLabel}</Typography></Box>
                      </Stack>
                    </Box>
                  </Box>
                </Grid>
              </Grid>
            </Container>
          </Box>
        </Box>

        <Container maxWidth="xl" data-reveal="up" sx={{ display: sections.featuredOffer ? 'block' : 'none', py: { xs: 10, md: 15 } }}>
          <Grid container spacing={{ xs: 5, md: 10 }} sx={{ alignItems: 'center' }}>
            <Grid size={{ xs: 12, md: 6 }}>
              <Box sx={{ position: 'relative' }}>
                <Box sx={{ minHeight: { xs: 390, md: 610 }, borderRadius: { xs: 4, md: 6 }, backgroundImage: `url(${content.featuredOffer.imageUrl})`, backgroundSize: 'cover', backgroundPosition: 'center' }} />
                <IconButton href="/programs" aria-label={content.featuredOffer.cta.label} sx={{ position: 'absolute', insetInlineEnd: 24, bottom: 24, width: 74, height: 74, bgcolor: 'secondary.main', color: 'brand.navy', '&:hover': { bgcolor: 'secondary.light', transform: 'scale(1.04)' } }}>
                  <PlayArrowRoundedIcon sx={{ fontSize: 36 }} />
                </IconButton>
              </Box>
            </Grid>
            <Grid size={{ xs: 12, md: 6 }}>
              <Stack spacing={3} sx={{ maxWidth: 620 }}>
                <Eyebrow>{content.featuredOffer.eyebrow}</Eyebrow>
                <Typography variant="h2">{content.featuredOffer.title}</Typography>
                <Typography color="text.secondary" sx={{ fontSize: { xs: 18, md: 20 }, lineHeight: 1.9 }}>{content.featuredOffer.summary}</Typography>
                <Button href="/programs" variant="text" endIcon={<NorthEastRoundedIcon />} sx={{ alignSelf: 'flex-start', px: 0, fontSize: 16 }}>
                  {content.featuredOffer.cta.label}
                </Button>
              </Stack>
            </Grid>
          </Grid>
        </Container>

        <Box id="programs" data-reveal="up" sx={{ display: sections.programs ? 'block' : 'none', background: (theme) => `linear-gradient(180deg, ${theme.palette.brand.cream}, color-mix(in srgb, ${theme.palette.brand.cream} 72%, ${theme.palette.background.default}))`, py: { xs: 9, md: 14 }, borderRadius: { md: '72px 72px 0 0' }, position: 'relative' }}>
          <Container maxWidth="xl">
            <SectionHeading eyebrow={uiText.programsEyebrow} title={uiText.programsTitle} number="01" />
            <Grid container spacing={2.5}>
              {content.programs.map((program, index) => (
                <Grid key={program.slug} size={{ xs: 12, md: index === 0 ? 6 : 3 }} data-reveal="card" style={{ '--reveal-delay': `${index * 110}ms` }}>
                  <Card className="lift-card" sx={{ height: '100%', border: 0, bgcolor: 'background.paper', overflow: 'hidden', borderRadius: 5, boxShadow: (theme) => `0 22px 60px color-mix(in srgb, ${theme.palette.brand.navy} 10%, transparent)`, transition: 'transform .4s cubic-bezier(.2,.8,.2,1), box-shadow .4s ease', '&:hover': { transform: 'translateY(-10px)', boxShadow: (theme) => `0 34px 80px color-mix(in srgb, ${theme.palette.brand.navy} 18%, transparent)` }, '&:hover .program-image': { transform: 'scale(1.055)' } }}>
                    <Box sx={{ position: 'relative', m: 1.2, mb: 0, height: { xs: 285, md: index === 0 ? 360 : 285 }, borderRadius: 4, overflow: 'hidden' }}><Box className="program-image" sx={{ position: 'absolute', inset: 0, backgroundImage: (theme) => `linear-gradient(180deg, transparent 42%, color-mix(in srgb, ${theme.palette.brand.navy} 72%, transparent)), url(${program.imageUrl})`, backgroundSize: 'cover', backgroundPosition: 'center', transition: 'transform .7s cubic-bezier(.2,.8,.2,1)' }} />
                      <Chip label={`0${index + 1}`} sx={{ position: 'absolute', top: 16, insetInlineStart: 16, bgcolor: (theme) => `color-mix(in srgb, ${theme.palette.background.paper} 92%, transparent)`, color: 'primary.main', fontWeight: 900 }} />
                    </Box>
                    <CardContent sx={{ p: 3.2 }}>
                      <Stack spacing={2.2}>
                        <Typography variant="h4" sx={{ fontSize: 25 }}>{program.title}</Typography>
                        <Typography color="text.secondary" sx={{ lineHeight: 1.8 }}>{program.summary}</Typography>
                        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
                          <Chip size="small" label={program.duration} />
                          <Chip size="small" label={program.priceLabel} variant="outlined" />
                        </Stack>
                        <Button href={`/checkout/${program.slug}`} variant="contained" endIcon={<ArrowForwardRoundedIcon />} fullWidth>{program.cta.label}</Button>
                      </Stack>
                    </CardContent>
                  </Card>
                </Grid>
              ))}
            </Grid>
          </Container>
        </Box>

        <StoryRail
          stories={(content.stories ?? []).filter((story) => story.isVisible && ['home', 'both'].includes(story.audience?.toLowerCase()))}
          language={language}
        />

        <Box id="experience" data-reveal="up" sx={{ display: sections.whyDana ? 'block' : 'none', bgcolor: 'brand.navy', color: 'white', py: { xs: 9, md: 14 }, position: 'relative' }}>
          <Container maxWidth="xl">
            <SectionHeading eyebrow={uiText.whyEyebrow} title={content.whyDana.title} number="02" light />
            <Grid container spacing={2.5}>
              {content.whyDana.reasons.map((reason, index) => (
                <Grid key={reason.order} size={{ xs: 12, md: index === 0 ? 6 : 3 }} data-reveal="card" style={{ '--reveal-delay': `${index * 120}ms` }}>
                  <Box className="reason-card" sx={{ position: 'relative', minHeight: { xs: 430, md: 540 }, borderRadius: 4, overflow: 'hidden', backgroundImage: `linear-gradient(180deg, rgba(15,20,53,.08) 30%, rgba(15,20,53,.94) 100%), url(${reason.imageUrl})`, backgroundSize: 'cover', backgroundPosition: 'center' }}>
                    <Stack sx={{ position: 'absolute', insetInline: 0, bottom: 0, p: 3 }} spacing={1.2}>
                      <Typography color="secondary.main" fontWeight={900}>{String(reason.order).padStart(2, '0')}</Typography>
                      <Typography variant="h5" fontWeight={800}>{reason.title}</Typography>
                      <Typography sx={{ color: 'rgba(255,255,255,.68)', lineHeight: 1.75 }}>{reason.body}</Typography>
                    </Stack>
                  </Box>
                </Grid>
              ))}
            </Grid>
          </Container>
        </Box>

        <Container maxWidth="xl" data-reveal="up" sx={{ display: sections.dashboard ? 'block' : 'none', py: { xs: 10, md: 15 } }}>
          <Grid container spacing={{ xs: 6, md: 10 }} sx={{ alignItems: 'center' }}>
            <Grid size={{ xs: 12, md: 5 }}>
              <Stack spacing={3}>
                <Eyebrow>{uiText.dashboardEyebrow}</Eyebrow>
                <Typography variant="h2">{uiText.dashboardTitle}</Typography>
                <Typography color="text.secondary" sx={{ fontSize: 18, lineHeight: 1.85 }}>{content.masterClass.body}</Typography>
                <Typography variant="h5" color="primary.main" fontWeight={900}>{content.masterClass.closingLine}</Typography>
                <Button href="/programs" variant="outlined" endIcon={<ArrowForwardRoundedIcon />} sx={{ alignSelf: 'flex-start' }}>{content.masterClass.cta.label}</Button>
              </Stack>
            </Grid>
            <Grid size={{ xs: 12, md: 7 }}>
              <Box sx={{ bgcolor: 'brand.cream', borderRadius: { xs: 4, md: 6 }, p: { xs: 2, md: 4 } }}>
                <Box sx={{ bgcolor: 'brand.navy', borderRadius: 4, p: { xs: 2.5, md: 4 }, color: 'white', boxShadow: (theme) => `0 30px 70px color-mix(in srgb, ${theme.palette.brand.navy} 24%, transparent)` }}>
                  <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ mb: 3 }}>
                    <Typography fontWeight={900}>{content.masterClass.title}</Typography>
                    <Stack direction="row" spacing={.6}>{[0, 1, 2].map((dot) => <Box key={dot} sx={{ width: 7, height: 7, borderRadius: '50%', bgcolor: dot === 0 ? 'secondary.main' : 'rgba(255,255,255,.24)' }} />)}</Stack>
                  </Stack>
                  <Grid container spacing={1.5}>
                    {content.dashboardModules.map((module, index) => (
                      <Grid key={module.key} size={{ xs: 12, sm: 6 }}>
                        <Stack direction="row" spacing={1.5} sx={{ p: 2, minHeight: 112, borderRadius: 2.5, bgcolor: index === 0 ? 'secondary.main' : 'rgba(255,255,255,.07)', color: index === 0 ? 'brand.navy' : 'white' }}>
                          <Avatar sx={{ width: 32, height: 32, bgcolor: index === 0 ? 'rgba(32,41,91,.12)' : 'rgba(255,255,255,.1)', color: 'inherit' }}><CheckRoundedIcon fontSize="small" /></Avatar>
                          <Box><Typography fontWeight={800}>{module.title}</Typography><Typography variant="body2" sx={{ mt: .5, opacity: .7, lineHeight: 1.55 }}>{module.description}</Typography></Box>
                        </Stack>
                      </Grid>
                    ))}
                  </Grid>
                </Box>
              </Box>
            </Grid>
          </Grid>
        </Container>

        <Box id="about" data-reveal="up" sx={{ display: sections.about ? 'block' : 'none', bgcolor: 'brand.cream', py: { xs: 9, md: 14 } }}>
          <Container maxWidth="xl">
            <Grid container spacing={{ xs: 5, md: 10 }}>
              <Grid size={{ xs: 12, md: 5 }}><Eyebrow>{uiText.aboutEyebrow}</Eyebrow><Typography variant="h2" sx={{ mt: 2 }}>{content.about.title}</Typography></Grid>
              <Grid size={{ xs: 12, md: 7 }}>
                <Typography sx={{ fontSize: { xs: 19, md: 24 }, color: 'text.secondary', lineHeight: 1.8 }}>{content.about.body}</Typography>
                <Grid container spacing={1.5} sx={{ mt: 3 }}>
                  {content.about.highlights.map((highlight) => (
                    <Grid key={highlight} size={{ xs: 12, sm: 6 }}><Stack direction="row" spacing={1.5} alignItems="center" sx={{ p: 2, bgcolor: 'rgba(255,255,255,.55)', borderRadius: 2.5 }}><Avatar sx={{ width: 30, height: 30, bgcolor: 'primary.main' }}><CheckRoundedIcon sx={{ fontSize: 18 }} /></Avatar><Typography fontWeight={700}>{highlight}</Typography></Stack></Grid>
                  ))}
                </Grid>
              </Grid>
            </Grid>
          </Container>
        </Box>

        <Container maxWidth="xl" data-reveal="up" sx={{ display: sections.testimonials ? 'block' : 'none', py: { xs: 9, md: 14 } }}>
          <SectionHeading eyebrow={uiText.testimonialsEyebrow} title={uiText.testimonialsTitle} number="03" />
          <Grid container spacing={2.5}>
            {content.testimonials.map((testimonial, index) => (
              <Grid key={`${testimonial.name}-${testimonial.role}`} size={{ xs: 12, md: 6 }} data-reveal="card" style={{ '--reveal-delay': `${index * 120}ms` }}>
                <Card sx={{ height: '100%', border: '1px solid rgba(32,41,91,.1)', boxShadow: 'none' }}>
                  <CardContent sx={{ p: { xs: 3, md: 4 } }}>
                    <QuoteRoundedIcon sx={{ fontSize: 48, color: 'secondary.main', transform: 'rotate(180deg)' }} />
                    <Typography variant="h5" sx={{ my: 2.5, lineHeight: 1.75, fontWeight: 600 }}>“{testimonial.quote}”</Typography>
                    <Stack direction="row" spacing={2} alignItems="center">
                      <Avatar src={testimonial.imageUrl} alt={testimonial.name} sx={{ width: 54, height: 54 }} />
                      <Box><Typography fontWeight={900}>{testimonial.name}</Typography><Typography variant="body2" color="text.secondary">{testimonial.role}</Typography><Typography variant="body2" color="primary">{testimonial.handle}</Typography></Box>
                    </Stack>
                  </CardContent>
                </Card>
              </Grid>
            ))}
          </Grid>
        </Container>

        {[...(content.customHtmlSections ?? [])]
          .filter((section) => section.isVisible)
          .sort((left, right) => left.order - right.order)
          .map((section) => (
            <CustomContentSection key={section.key} section={section} />
          ))}

        <Box id="contact" data-reveal="up" sx={{ display: sections.leadCapture ? 'block' : 'none', px: { xs: 1.5, md: 3 }, pb: { xs: 1.5, md: 3 } }}>
          <Box sx={{ maxWidth: 1536, mx: 'auto', position: 'relative', overflow: 'hidden', bgcolor: 'brand.forest', color: 'white', borderRadius: { xs: 4, md: 7 }, p: { xs: 3.5, sm: 6, md: 9 } }}>
            <Box sx={{ position: 'absolute', width: 500, height: 500, borderRadius: '50%', border: '1px solid rgba(255,255,255,.1)', top: -310, insetInlineEnd: -80 }} />
            <Grid container spacing={5} sx={{ position: 'relative', alignItems: 'center' }}>
              <Grid size={{ xs: 12, md: 6 }}><Eyebrow light>{uiText.stayConnectedLabel}</Eyebrow><Typography variant="h2" sx={{ mt: 2 }}>{content.leadCapture.title}</Typography><Typography sx={{ mt: 2, color: 'rgba(255,255,255,.7)', fontSize: 18, lineHeight: 1.8 }}>{content.leadCapture.subtitle}</Typography></Grid>
              <Grid size={{ xs: 12, md: 6 }}>
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.2} sx={{ p: 1, bgcolor: 'rgba(255,255,255,.1)', borderRadius: 3 }}>
                  <TextField fullWidth placeholder={content.leadCapture.emailPlaceholder} type="email" sx={{ '& .MuiOutlinedInput-root': { bgcolor: 'white', borderRadius: 2.5 } }} />
                  <Button variant="contained" color="secondary" startIcon={<EmailRoundedIcon />} sx={{ whiteSpace: 'nowrap' }}>{content.leadCapture.submitLabel}</Button>
                </Stack>
                <Typography variant="h6" sx={{ mt: 4 }}>{content.leadCapture.contactTitle}</Typography>
                <Typography sx={{ mt: 1, color: 'rgba(255,255,255,.65)' }}>{content.leadCapture.contactSubtitle}</Typography>
              </Grid>
            </Grid>
          </Box>
        </Box>

        <Container maxWidth="xl" component="footer" sx={{ display: sections.footer ? 'block' : 'none', py: 4 }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} justifyContent="space-between" alignItems="center">
            <Box component="img" src="/assets/logo-dana-gharaibeh.svg" alt="Dana Gharaibeh" sx={{ width: 145 }} />
            <Typography variant="body2" color="text.secondary">© {new Date().getFullYear()} Dana Gharaibeh. {uiText.rightsText}</Typography>
          </Stack>
        </Container>
      </Box>
    </Box>
    </ThemeProvider>
  );
}

function CustomContentSection({ section }) {
  const common = { component: 'section', id: section.key, 'data-reveal': 'up', 'aria-label': section.title };
  if (section.type === 'image' || section.type === 'video') return <MediaStorySection section={section} common={common} />;
  return <Box {...common} dangerouslySetInnerHTML={{ __html: section.html }} />;
}

function HeroMedia({ hero }) {
  const videoRef = useRef(null);
  const playableVideo = Boolean(hero.videoUrl) && !hero.videoUrl.includes('dQw4w9WgXcQ');
  const directVideo = playableVideo && isDirectVideo(hero.videoUrl);
  const [playing, setPlaying] = useState(directVideo);
  const [muted, setMuted] = useState(true);

  const togglePlayback = async () => {
    if (!videoRef.current) return;
    if (videoRef.current.paused) { await videoRef.current.play(); setPlaying(true); }
    else { videoRef.current.pause(); setPlaying(false); }
  };

  return (
    <Box className="hero-media" sx={{ position: 'relative', height: { xs: 400, sm: 520, md: 625 }, borderRadius: { xs: '40px 140px 40px 40px', md: '56px 240px 56px 56px' }, overflow: 'hidden', bgcolor: 'brand.forest', border: '1px solid rgba(255,255,255,.16)', boxShadow: (theme) => `0 34px 80px color-mix(in srgb, ${theme.palette.brand.navy} 35%, transparent)` }}>
      {directVideo ? <Box ref={videoRef} component="video" src={hero.videoUrl} poster={hero.imageUrl} autoPlay muted={muted} loop playsInline sx={{ width: 1, height: 1, objectFit: 'cover' }} /> : playableVideo ? <Box component="iframe" src={toEmbedUrl(hero.videoUrl)} title={hero.title} allow="autoplay; encrypted-media; picture-in-picture" allowFullScreen sx={{ width: 1, height: 1, border: 0 }} /> : <Box component="img" src={hero.imageUrl} alt="" sx={{ width: 1, height: 1, objectFit: 'cover' }} />}
      <Box sx={{ position: 'absolute', inset: 0, background: 'linear-gradient(180deg, transparent 48%, rgba(12,16,43,.62))' }} />
      {directVideo && <Stack direction="row" spacing={1} sx={{ position: 'absolute', top: 20, insetInlineEnd: 20 }}>
        <IconButton onClick={togglePlayback} aria-label={playing ? 'Pause video' : 'Play video'} sx={{ color: 'white', bgcolor: 'rgba(12,16,43,.5)', backdropFilter: 'blur(12px)', '&:hover': { bgcolor: 'rgba(12,16,43,.72)' } }}>{playing ? <PauseRoundedIcon /> : <PlayArrowRoundedIcon />}</IconButton>
        <IconButton onClick={() => setMuted((current) => !current)} aria-label={muted ? 'Unmute video' : 'Mute video'} sx={{ color: 'white', bgcolor: 'rgba(12,16,43,.5)', backdropFilter: 'blur(12px)', '&:hover': { bgcolor: 'rgba(12,16,43,.72)' } }}>{muted ? <VolumeOffRoundedIcon /> : <VolumeUpRoundedIcon />}</IconButton>
      </Stack>}
    </Box>
  );
}

function MediaStorySection({ section, common }) {
  const fullBleed = section.layout === 'full-bleed';
  const mediaFirst = section.layout === 'media-left';
  const tones = {
    light: { bg: 'background.default', color: 'text.primary', muted: 'text.secondary' },
    cream: { bg: 'brand.cream', color: 'text.primary', muted: 'text.secondary' },
    navy: { bg: 'brand.navy', color: 'white', muted: 'rgba(255,255,255,.68)' },
    forest: { bg: 'brand.forest', color: 'white', muted: 'rgba(255,255,255,.68)' },
  };
  const tone = tones[section.theme] ?? tones.light;
  const mediaSx = { width: 1, height: 1, minHeight: { xs: 320, md: fullBleed ? 620 : 520 }, maxHeight: 760, objectFit: 'cover', display: 'block' };
  const media = section.type === 'video'
    ? <VideoAsset section={section} sx={mediaSx} />
    : <Box component="img" src={section.mediaUrl} alt={section.altText || section.title} sx={{ width: 1, height: 1, minHeight: { xs: 320, md: fullBleed ? 620 : 520 }, maxHeight: 760, objectFit: 'cover', display: 'block' }} />;

  if (fullBleed) return (
    <Box {...common} sx={{ px: { xs: 1.5, md: 3 }, py: { xs: 4, md: 7 }, bgcolor: tone.bg }}>
      <Box sx={{ position: 'relative', maxWidth: 1536, mx: 'auto', borderRadius: { xs: 4, md: 7 }, overflow: 'hidden', bgcolor: 'black' }}>
        {media}
        <Box sx={{ position: 'absolute', inset: 0, background: 'linear-gradient(90deg, rgba(9,13,34,.88) 0%, rgba(9,13,34,.38) 58%, transparent 100%)' }} />
        <Stack spacing={2.2} sx={{ position: 'absolute', insetInlineStart: { xs: 24, md: 72 }, insetInlineEnd: { xs: 24, md: '48%' }, bottom: { xs: 28, md: 64 }, color: 'white' }}>
          {section.eyebrow && <Eyebrow light>{section.eyebrow}</Eyebrow>}
          <Typography variant="h2">{section.title}</Typography>
          {section.body && <Typography sx={{ color: 'rgba(255,255,255,.72)', fontSize: { xs: 16, md: 19 }, lineHeight: 1.8 }}>{section.body}</Typography>}
          {section.ctaLabel && <Button href={section.ctaUrl || '#'} variant="contained" color="secondary" endIcon={<NorthEastRoundedIcon />} sx={{ alignSelf: 'flex-start' }}>{section.ctaLabel}</Button>}
        </Stack>
      </Box>
    </Box>
  );

  return (
    <Box {...common} sx={{ bgcolor: tone.bg, color: tone.color, py: { xs: 8, md: 13 } }}>
      <Container maxWidth="xl">
        <Grid container spacing={{ xs: 5, md: 9 }} sx={{ alignItems: 'center', flexDirection: mediaFirst ? { md: 'row' } : { md: 'row-reverse' } }}>
          <Grid size={{ xs: 12, md: 7 }}><Box className="media-story-frame" sx={{ borderRadius: { xs: 4, md: 6 }, overflow: 'hidden', boxShadow: section.theme === 'navy' || section.theme === 'forest' ? '0 32px 80px rgba(0,0,0,.28)' : '0 32px 80px rgba(24,32,66,.14)' }}>{media}</Box></Grid>
          <Grid size={{ xs: 12, md: 5 }}><Stack spacing={2.5}>{section.eyebrow && <Eyebrow light={section.theme === 'navy' || section.theme === 'forest'}>{section.eyebrow}</Eyebrow>}<Typography variant="h2">{section.title}</Typography>{section.body && <Typography sx={{ color: tone.muted, fontSize: { xs: 17, md: 19 }, lineHeight: 1.85 }}>{section.body}</Typography>}{section.ctaLabel && <Button href={section.ctaUrl || '#'} variant="contained" color="secondary" endIcon={<NorthEastRoundedIcon />} sx={{ alignSelf: 'flex-start' }}>{section.ctaLabel}</Button>}</Stack></Grid>
        </Grid>
      </Container>
    </Box>
  );
}

function VideoAsset({ section, sx }) {
  if (!isDirectVideo(section.mediaUrl)) {
    return <Box component="iframe" src={toEmbedUrl(section.mediaUrl)} title={section.title} allow="autoplay; encrypted-media; picture-in-picture" allowFullScreen sx={{ ...sx, border: 0 }} />;
  }
  return <Box component="video" src={section.mediaUrl} poster={section.posterUrl || undefined} title={section.title} controls={!section.autoplay} autoPlay={section.autoplay} muted={section.muted ?? true} loop={section.loop} playsInline preload="metadata" sx={sx} />;
}

function StoryRail({ stories, language }) {
  const [activeStory, setActiveStory] = useState(null);
  if (!stories.length) return null;
  const sortedStories = [...stories].sort((left, right) => left.order - right.order);
  const copy = language === 'ar'
    ? { eyebrow: 'قصص دانا', title: 'لحظات قصيرة قد تغيّر خطوتك القادمة', hint: 'مرّر لاستكشاف المزيد', close: 'إغلاق الفيديو' }
    : { eyebrow: 'Dana stories', title: 'Short moments that can shift your next move', hint: 'Scroll to explore', close: 'Close video' };

  return (
    <Box component="section" data-reveal="up" sx={{ bgcolor: 'brand.navy', color: 'white', py: { xs: 8, md: 12 }, overflow: 'hidden' }}>
      <Container maxWidth="xl">
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} sx={{ mb: 4, justifyContent: 'space-between', alignItems: { md: 'flex-end' } }}>
          <Box><Eyebrow light>{copy.eyebrow}</Eyebrow><Typography variant="h2" sx={{ mt: 1.5, maxWidth: 760 }}>{copy.title}</Typography></Box>
          <Typography variant="body2" sx={{ color: 'rgba(255,255,255,.55)' }}>{copy.hint} →</Typography>
        </Stack>
        <Box className="story-rail" sx={{ display: 'grid', gridAutoFlow: 'column', gridAutoColumns: { xs: '78%', sm: '43%', md: '27%', lg: '22%' }, gap: 2, overflowX: 'auto', pb: 2, scrollSnapType: 'x mandatory' }}>
          {sortedStories.map((story, index) => <StoryCard key={story.key} story={story} index={index} onOpen={() => setActiveStory(story)} />)}
        </Box>
      </Container>
      <Dialog open={Boolean(activeStory)} onClose={() => setActiveStory(null)} maxWidth="sm" fullWidth PaperProps={{ sx: { overflow: 'hidden', bgcolor: '#070a18', borderRadius: { xs: 0, sm: 4 } } }}>
        {activeStory && <DialogContent sx={{ p: 0, position: 'relative', aspectRatio: '9/16', maxHeight: '90vh' }}>
          <IconButton onClick={() => setActiveStory(null)} aria-label={copy.close} sx={{ position: 'absolute', zIndex: 2, top: 14, insetInlineEnd: 14, color: 'white', bgcolor: 'rgba(0,0,0,.4)', backdropFilter: 'blur(10px)' }}><CloseRoundedIcon /></IconButton>
          <Box component="video" src={activeStory.videoUrl} poster={activeStory.posterUrl || undefined} controls autoPlay playsInline sx={{ width: 1, height: 1, objectFit: 'contain', display: 'block' }} />
        </DialogContent>}
      </Dialog>
    </Box>
  );
}

function StoryCard({ story, index, onOpen }) {
  const previewRef = useRef(null);
  const playPreview = () => previewRef.current?.play().catch(() => {});
  const pausePreview = () => { if (previewRef.current) previewRef.current.pause(); };
  return (
    <Box component="button" type="button" onClick={onOpen} onMouseEnter={playPreview} onMouseLeave={pausePreview} onFocus={playPreview} onBlur={pausePreview} sx={{ position: 'relative', p: 0, border: '1px solid rgba(255,255,255,.14)', borderRadius: 4, overflow: 'hidden', aspectRatio: '9/14', scrollSnapAlign: 'start', bgcolor: '#101735', color: 'white', cursor: 'pointer', textAlign: 'start', boxShadow: '0 20px 55px rgba(0,0,0,.2)', '&:hover .story-cover': { transform: 'scale(1.045)' } }}>
      <Box ref={previewRef} className="story-cover" component="video" src={story.videoUrl} poster={story.posterUrl || undefined} muted loop playsInline preload={index < 5 ? 'auto' : 'metadata'} onLoadedData={(event) => { if (!story.posterUrl && event.currentTarget.currentTime === 0) event.currentTarget.currentTime = .35; }} sx={{ width: 1, height: 1, objectFit: 'cover', display: 'block', transition: 'transform 700ms cubic-bezier(.2,.8,.2,1)' }} />
      <Box sx={{ position: 'absolute', inset: 0, background: 'linear-gradient(180deg, rgba(5,8,24,.05) 35%, rgba(5,8,24,.92) 100%)' }} />
      <Chip label={String(index + 1).padStart(2, '0')} size="small" sx={{ position: 'absolute', top: 16, insetInlineStart: 16, bgcolor: 'rgba(255,255,255,.9)', color: 'brand.navy', fontWeight: 900 }} />
      <Avatar sx={{ position: 'absolute', top: 16, insetInlineEnd: 16, bgcolor: 'secondary.main', color: 'brand.navy' }}><PlayArrowRoundedIcon /></Avatar>
      <Stack spacing={.8} sx={{ position: 'absolute', insetInline: 20, bottom: 20 }}><Typography variant="h5" fontWeight={900}>{story.title}</Typography><Typography variant="body2" sx={{ color: 'rgba(255,255,255,.68)', lineHeight: 1.55 }}>{story.caption}</Typography></Stack>
    </Box>
  );
}

function ScrollProgress({ top }) {
  const [progress, setProgress] = useState(0);
  useEffect(() => {
    const update = () => {
      const available = document.documentElement.scrollHeight - window.innerHeight;
      setProgress(available > 0 ? Math.min(100, (window.scrollY / available) * 100) : 0);
    };
    update();
    window.addEventListener('scroll', update, { passive: true });
    window.addEventListener('resize', update);
    return () => { window.removeEventListener('scroll', update); window.removeEventListener('resize', update); };
  }, []);
  return <Box aria-hidden sx={{ position: 'fixed', zIndex: 1300, top, insetInlineStart: 0, width: `${progress}%`, height: 3, bgcolor: 'secondary.main', transition: 'width 80ms linear', pointerEvents: 'none' }} />;
}

function isDirectVideo(url = '') {
  return /(^blob:|^data:video|\/api\/assets\/|\.(mp4|webm|mov|m4v|ogg)(\?|$))/i.test(url);
}

function toEmbedUrl(url = '') {
  if (url.includes('youtube.com/watch')) {
    const videoId = new URL(url).searchParams.get('v');
    return videoId ? `https://www.youtube.com/embed/${videoId}` : url;
  }
  if (url.includes('youtu.be/')) return `https://www.youtube.com/embed/${url.split('youtu.be/')[1].split(/[?#]/)[0]}`;
  if (url.includes('vimeo.com/') && !url.includes('player.vimeo.com')) return `https://player.vimeo.com/video/${url.split('vimeo.com/')[1].split(/[?#/]/)[0]}`;
  return url;
}

function Eyebrow({ children, light = false }) {
  return <Stack direction="row" spacing={1} alignItems="center"><Box sx={{ width: 30, height: 2, bgcolor: light ? 'secondary.main' : 'primary.main' }} /><Typography variant="overline" sx={{ color: light ? 'secondary.main' : 'primary.main' }}>{children}</Typography></Stack>;
}

function HeroMetric({ value, label }) {
  return <Box><Typography variant="h5" fontWeight={900} color="secondary.main">{value}</Typography><Typography variant="caption" sx={{ color: 'rgba(255,255,255,.55)' }}>{label}</Typography></Box>;
}

function SectionHeading({ eyebrow, title, number, light = false }) {
  return (
    <Stack direction={{ xs: 'column', md: 'row' }} justifyContent="space-between" alignItems={{ md: 'flex-end' }} spacing={2} sx={{ mb: 5 }}>
      <Box><Eyebrow light={light}>{eyebrow}</Eyebrow><Typography variant="h2" sx={{ mt: 1.5, maxWidth: 760, color: light ? 'white' : 'text.primary' }}>{title}</Typography></Box>
      <Typography sx={{ fontSize: 64, lineHeight: 1, fontWeight: 900, color: light ? 'rgba(255,255,255,.1)' : 'rgba(32,41,91,.08)' }}>{number}</Typography>
    </Stack>
  );
}

function useScrollReveal(content) {
  useEffect(() => {
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    const elements = [...document.querySelectorAll('[data-reveal]')];

    if (reducedMotion || !('IntersectionObserver' in window)) {
      elements.forEach((element) => element.classList.add('is-visible'));
      return undefined;
    }

    document.documentElement.classList.add('scroll-reveal-ready');

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          entry.target.classList.toggle('is-visible', entry.isIntersecting);
        });
      },
      { threshold: 0.12, rootMargin: '0px 0px -60px' },
    );

    elements.forEach((element) => observer.observe(element));

    return () => {
      observer.disconnect();
      document.documentElement.classList.remove('scroll-reveal-ready');
    };
  }, [content]);
}

function createFallbackUiText(t) {
  return {
    navPrograms: t('nav.programs'), navExperience: t('nav.experience'), navAbout: t('nav.about'),
    programsEyebrow: t('landing.programsEyebrow'), programsTitle: t('landing.programsTitle'),
    whyEyebrow: t('landing.whyEyebrow'), dashboardEyebrow: t('landing.dashboardEyebrow'),
    dashboardTitle: t('landing.dashboardTitle'), testimonialsEyebrow: t('landing.testimonialsEyebrow'),
    testimonialsTitle: t('landing.testimonialsTitle'), aboutEyebrow: t('nav.about'),
    personalLabel: t('landing.personal'), growthLabel: t('landing.growth'), pathsLabel: t('landing.paths'),
    nextChapterLabel: t('landing.nextChapter'), startsHereLabel: t('landing.startsHere'),
    stayConnectedLabel: t('landing.stayConnected'), rightsText: t('landing.rights'),
  };
}

function createDefaultVisibility() {
  return {
    header: true, hero: true, featuredOffer: true, programs: true, whyDana: true,
    dashboard: true, about: true, testimonials: true, leadCapture: true, footer: true,
  };
}
