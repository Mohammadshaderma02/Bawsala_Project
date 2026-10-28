import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded';
import CloudUploadRoundedIcon from '@mui/icons-material/CloudUploadRounded';
import LinkRoundedIcon from '@mui/icons-material/LinkRounded';
import { Accordion, AccordionDetails, AccordionSummary, Alert, Box, Button, Chip, CircularProgress, Grid, Paper, Stack, TextField, Typography } from '@mui/material';
import ExpandMoreRoundedIcon from '@mui/icons-material/ExpandMoreRounded';
import { useState } from 'react';
import { uploadCmsAsset } from './cmsApi';

const strengths = ['Creativity','Curiosity','Judgment','Love of learning','Perspective','Bravery','Perseverance','Honesty','Zest','Love','Kindness','Social intelligence','Teamwork','Fairness','Leadership','Forgiveness','Humility','Prudence','Self-regulation','Appreciation of beauty','Gratitude','Hope','Humor','Spirituality'];
const slug = (value) => value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
const groups = [
  { key: 'program-intro', title: 'Program Home introduction', description: 'The welcome media clients see before paying and starting Career Clarity.', accept: 'image/*,video/*', items: [
    ['program-intro', 'Dana’s Career Clarity welcome'],
  ] },
  { key: 'assessment-intros', title: 'Assessment introduction videos', accept: 'video/*', items: [
    ['strengths-intro', 'Character Strengths introduction'], ['interests-intro', 'Career Interests introduction'], ['values-intro', 'Career Values introduction'], ['purpose-intro', 'Life Purpose introduction'],
  ] },
  { key: 'strength-videos', title: 'Character Strengths videos (24)', accept: 'video/*', items: strengths.map((name) => [`strength-${slug(name)}`, name]) },
  { key: 'assessment-resources', title: 'Assessment files and Career Profile template', accept: '.pdf,.doc,.docx,.xls,.xlsx', items: [
    ['interests-workbook', 'Career Interests workbook'], ['values-workbook', 'Career Values workbook'], ['life-purpose-document', 'Life Purpose document'], ['career-profile-template', 'Career Profile template'],
  ] },
];

export function CareerClarityAssetsManager({ courses, onChange, language }) {
  const [uploading, setUploading] = useState(''); const [error, setError] = useState(''); const [links, setLinks] = useState({});
  const courseIndex = courses.findIndex((item) => item.slug === 'career-clarity-program');
  if (courseIndex < 0) return <Alert severity="warning">Create the Career Clarity Program in Courses first using the slug <b>career-clarity-program</b>.</Alert>;
  const course = courses[courseIndex]; const sections = course.sections ?? [];
  const lesson = (key) => sections.flatMap((section) => section.lessons ?? []).find((item) => item.key === key);

  function updateAsset(groupKey, key, title, url) {
    let found = false;
    let nextSections = sections.map((section) => ({ ...section, lessons: (section.lessons ?? []).map((item) => { if (item.key !== key) return item; found = true; return { ...item, title, videoUrl: url }; }) }));
    if (!found) {
      const group = groups.find((item) => item.key === groupKey);
      const sectionIndex = nextSections.findIndex((item) => item.key === groupKey);
      const newLesson = { key, title, videoUrl: url, durationMinutes: 10 };
      if (sectionIndex >= 0) nextSections = nextSections.map((item, index) => index === sectionIndex ? { ...item, lessons: [...(item.lessons ?? []), newLesson] } : item);
      else nextSections = [...nextSections, { key: groupKey, title: group.title, description: 'Managed from Career Clarity Assets', sortOrder: nextSections.length + 1, lessons: [newLesson] }];
    }
    onChange(courses.map((item, index) => index === courseIndex ? { ...item, sections: nextSections } : item));
  }

  async function upload(event, group, key, title) {
    const file = event.target.files?.[0]; event.target.value = ''; if (!file) return;
    setUploading(key); setError('');
    try { updateAsset(group.key, key, title, await uploadCmsAsset(file, title)); }
    catch (requestError) { setError(requestError.response?.data?.message || requestError.message); }
    finally { setUploading(''); }
  }

  function saveLink(group, key, title, current) {
    const value = (links[key] ?? current ?? '').trim();
    if (!/^https?:\/\//i.test(value)) { setError('Enter a complete link starting with http:// or https://'); return; }
    setError(''); updateAsset(group.key, key, title, value);
  }

  const total = groups.reduce((sum, group) => sum + group.items.length, 0);
  const completed = groups.flatMap((group) => group.items).filter(([key]) => lesson(key)?.videoUrl).length;
  return <Stack spacing={3}>
    <Paper sx={{ p: 3.5, borderRadius: 4, color: 'white', background: 'linear-gradient(125deg,#102d5b,#2f4f23)' }}><Typography variant="h4" sx={{ color: 'white' }}>Career Clarity content library</Typography><Typography sx={{ mt: 1, color: 'rgba(255,255,255,.7)' }}>Upload every video, assessment resource, and Career Profile template from one place. Publish after uploading to make the changes visible to clients.</Typography><Chip icon={<CheckCircleRoundedIcon />} label={`${completed}/${total} files ready · ${language.toUpperCase()} content`} sx={{ mt: 2, bgcolor: '#f3dfb8', color: '#102d5b', fontWeight: 900 }} /></Paper>
    {error && <Alert severity="error">{error}</Alert>}
    {groups.map((group, groupIndex) => <Accordion key={group.key} defaultExpanded={groupIndex < 2} sx={{ borderRadius: '16px!important', '&:before': { display: 'none' } }}><AccordionSummary expandIcon={<ExpandMoreRoundedIcon />}><Box><Typography variant="h6" fontWeight={900}>{group.title}</Typography><Typography variant="body2" color="text.secondary">{group.description || `${group.items.filter(([key]) => lesson(key)?.videoUrl).length}/${group.items.length} ready`}</Typography></Box></AccordionSummary><AccordionDetails><Grid container spacing={1.5}>{group.items.map(([key, title]) => { const current = lesson(key)?.videoUrl ?? ''; const draft = links[key] ?? current; const isImage = /\.(png|jpe?g|webp|gif|svg)(\?|$)/i.test(current); const isExternalPlayer = /youtu\.?be|youtube\.com|vimeo\.com/i.test(current); const embed = current.includes('youtu.be/') ? `https://www.youtube.com/embed/${current.split('youtu.be/')[1]?.split(/[?#]/)[0]}` : current.includes('youtube.com/watch') ? `https://www.youtube.com/embed/${new URL(current).searchParams.get('v')}` : current.includes('vimeo.com/') && !current.includes('player.vimeo.com') ? `https://player.vimeo.com/video/${current.split('vimeo.com/')[1]?.split(/[?#/]/)[0]}` : current; return <Grid key={key} size={{ xs: 12, md: group.key === 'program-intro' ? 12 : 6 }}><Paper variant="outlined" sx={{ p: 2.5, height: '100%', borderRadius: 3 }}><Stack spacing={2}><Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} justifyContent="space-between" alignItems={{ sm: 'center' }}><Box sx={{ minWidth: 0 }}><Typography fontWeight={900}>{title}</Typography><Typography variant="caption" color="text.secondary" noWrap sx={{ display: 'block', maxWidth: 520 }}>{current || 'No media selected yet'}</Typography></Box><Button component="label" variant={current ? 'outlined' : 'contained'} startIcon={uploading === key ? <CircularProgress size={17} /> : <CloudUploadRoundedIcon />} disabled={Boolean(uploading)}>{current ? 'Replace file' : 'Upload video or image'}<input hidden type="file" accept={group.accept} onChange={(event) => upload(event, group, key, title)} /></Button></Stack><Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}><TextField fullWidth size="small" label="Or paste a video / image link" value={draft} onChange={(event) => setLinks({ ...links, [key]: event.target.value })} placeholder="https://youtube.com/... or https://.../video.mp4" /><Button variant="outlined" startIcon={<LinkRoundedIcon />} onClick={() => saveLink(group, key, title, current)} sx={{ whiteSpace: 'nowrap' }}>Use link</Button></Stack>{current && (isImage ? <Box component="img" src={current} alt={title} sx={{ width: 1, maxHeight: 300, objectFit: 'cover', borderRadius: 2.5 }} /> : isExternalPlayer ? <Box component="iframe" src={embed} title={title} allow="autoplay; encrypted-media; picture-in-picture" allowFullScreen sx={{ width: 1, aspectRatio: '16/9', border: 0, borderRadius: 2.5 }} /> : <Box component="video" src={current} controls preload="metadata" sx={{ width: 1, maxHeight: 320, bgcolor: '#091026', borderRadius: 2.5 }} />)}</Stack></Paper></Grid>})}</Grid></AccordionDetails></Accordion>)}
  </Stack>;
}
