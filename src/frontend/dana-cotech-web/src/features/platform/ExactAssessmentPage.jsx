import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded';
import LockRoundedIcon from '@mui/icons-material/LockRounded';
import PlayCircleRoundedIcon from '@mui/icons-material/PlayCircleRounded';
import {
  Alert, Box, Button, Card, CardContent, Checkbox, Chip, CircularProgress, Divider,
  FormControlLabel, Grid, LinearProgress, MenuItem, Paper, Stack, TextField, Typography,
} from '@mui/material';
import { useMemo, useState } from 'react';
import assessmentData from './assessmentData.json';
import { saveAssessment } from './platformApi';

const STRENGTHS = ['Creativity','Curiosity','Judgment','Love of learning','Perspective','Bravery','Perseverance','Honesty','Zest','Love','Kindness','Social intelligence','Teamwork','Fairness','Leadership','Forgiveness','Humility','Prudence','Self-regulation','Appreciation of beauty','Gratitude','Hope','Humor','Spirituality'];
const TYPES = { R: 'Realistic', I: 'Investigative', A: 'Artistic', S: 'Social', E: 'Enterprising', C: 'Conventional' };

export function ExactAssessmentPage({ language, assessmentKey, snapshot, onUpdate }) {
  const ar = language === 'ar';
  const sequence = ['strengths', 'interests', 'values', 'purpose'];
  const index = sequence.indexOf(assessmentKey);
  const enrolled = snapshot.state.enrollments.some((item) => item.productSlug === 'career-clarity-program');
  const previousComplete = index <= 0 || Boolean(snapshot.state.assessments[sequence[index - 1]]);
  const existing = snapshot.state.assessments[assessmentKey]?.answers ?? {};
  const [selected, setSelected] = useState(() => [1,2,3,4,5].map((n) => existing[assessmentKey === 'strengths' ? `strength${n}` : `value${n}`]).filter(Boolean));
  const [ratings, setRatings] = useState(() => Object.fromEntries(Object.entries(existing).filter(([key]) => key.startsWith('rating-'))));
  const [watched, setWatched] = useState(() => Object.fromEntries([1,2,3,4,5].map((n) => [n, existing[`watched${n}`] === 'true'])));
  const [reflections, setReflections] = useState(() => Object.fromEntries(assessmentData.purposeQuestions.map((item) => [item.id, existing[item.id] ?? ''])));
  const [statement, setStatement] = useState(existing.statement ?? '');
  const [busy, setBusy] = useState(false); const [error, setError] = useState(''); const [done, setDone] = useState(false);

  const names = ar ? { strengths: 'نقاط القوة الشخصية', interests: 'الميول المهنية RIASEC', values: 'القيم المهنية', purpose: 'غاية الحياة' } : { strengths: 'Character Strengths', interests: 'RIASEC Career Interests', values: 'Career Values', purpose: 'Life Purpose' };
  const ratingCount = Object.keys(ratings).filter((key) => key.startsWith('rating-')).length;
  const required = assessmentKey === 'strengths' ? 10 : assessmentKey === 'interests' ? 72 : assessmentKey === 'values' ? 59 : 12;
  const progress = assessmentKey === 'strengths' ? selected.length + Object.values(watched).filter(Boolean).length : assessmentKey === 'interests' ? ratingCount : assessmentKey === 'values' ? ratingCount + selected.length : Object.values(reflections).filter((value) => value.trim()).length + (statement.trim() ? 1 : 0);

  const allLessons = useMemo(() => snapshot.catalog.find((product) => product.slug === 'career-clarity-program')?.modules.flatMap((module) => module.lessons) ?? [], [snapshot.catalog]);
  const introUrl = allLessons.find((lesson) => lesson.id === `${assessmentKey}-intro`)?.contentUrl;
  const resourceKey = { interests: 'interests-workbook', values: 'values-workbook', purpose: 'life-purpose-document' }[assessmentKey];
  const resourceUrl = resourceKey ? allLessons.find((lesson) => lesson.id === resourceKey)?.contentUrl : '';
  const toggle = (value) => setSelected((current) => current.includes(value) ? current.filter((item) => item !== value) : current.length < 5 ? [...current, value] : current);

  async function submit() {
    setBusy(true); setError('');
    try {
      let answers;
      if (assessmentKey === 'strengths') answers = { ...Object.fromEntries(selected.map((value, i) => [`strength${i + 1}`, value])), ...Object.fromEntries(selected.map((_, i) => [`watched${i + 1}`, String(Boolean(watched[i + 1]))])) };
      else if (assessmentKey === 'interests') {
        const totals = Object.keys(TYPES).map((type) => [type, assessmentData.interests.filter((item) => item.type === type).reduce((sum, item) => sum + Number(ratings[`rating-${item.id}`] ?? 0), 0)]).sort((a, b) => b[1] - a[1]).slice(0, 3);
        answers = { ...ratings, ...Object.fromEntries(totals.map(([type, score], i) => [`interest${i + 1}`, `${TYPES[type]} (${score}/24)`])) };
      } else if (assessmentKey === 'values') answers = { ...ratings, ...Object.fromEntries(selected.map((value, i) => [`value${i + 1}`, value])) };
      else answers = { ...reflections, statement };
      onUpdate(await saveAssessment(assessmentKey, answers, language)); setDone(true); window.scrollTo({ top: 0, behavior: 'smooth' });
    } catch (requestError) { setError(requestError.response?.data?.message || requestError.message); }
    finally { setBusy(false); }
  }

  if (!enrolled || !previousComplete) return <Paper sx={{ maxWidth: 720, mx: 'auto', p: 6, textAlign: 'center', borderRadius: 4 }}><LockRoundedIcon color="primary" sx={{ fontSize: 62 }} /><Typography variant="h4" fontWeight={900} sx={{ mt: 2 }}>{!enrolled ? (ar ? 'أكمل دفع البرنامج أولاً' : 'Complete the program payment first') : (ar ? 'أكمل المرحلة السابقة أولاً' : 'Complete the previous stage first')}</Typography><Button component="a" href={!enrolled ? '/checkout/career-clarity-program' : `/assessment/${sequence[index - 1]}`} variant="contained" sx={{ mt: 3 }}>{ar ? 'الانتقال للخطوة المطلوبة' : 'Go to the required step'}</Button></Paper>;

  return <Stack spacing={3} sx={{ maxWidth: 1050, mx: 'auto' }}>
    <Paper sx={{ p: { xs: 3, md: 5 }, color: 'white', borderRadius: 5, background: 'linear-gradient(125deg,#102d5b,#2f4f23)' }}><Chip label={ar ? `المرحلة ${index + 1} من 4` : `Stage ${index + 1} of 4`} sx={{ bgcolor: '#f3dfb8', color: '#102d5b', fontWeight: 900 }} /><Typography variant="h2" sx={{ color: 'white', mt: 2 }}>{names[assessmentKey]}</Typography><Typography sx={{ mt: 1.5, color: 'rgba(255,255,255,.72)', maxWidth: 760 }}>{descriptions(assessmentKey, ar)}</Typography><LinearProgress variant="determinate" value={Math.min(100, progress / required * 100)} sx={{ mt: 3, height: 10, borderRadius: 8, bgcolor: 'rgba(255,255,255,.14)', '& .MuiLinearProgress-bar': { bgcolor: '#f3dfb8' } }} /></Paper>
    {introUrl ? <Box component="video" controls src={introUrl} sx={{ width: '100%', maxHeight: 520, bgcolor: '#071226', borderRadius: 4 }} /> : <Alert severity="info">{ar ? 'سيظهر فيديو دانا التعريفي هنا فور رفعه من لوحة الإدارة.' : "Dana's introduction video will appear here once it is uploaded from the admin area."}</Alert>}
    {resourceUrl && <Alert severity="success" action={<Button component="a" href={resourceUrl} target="_blank" rel="noreferrer" color="inherit">{ar ? 'تنزيل الملف' : 'Download file'}</Button>}>{ar ? 'الملف الأصلي الذي رفعته دانا متاح أيضاً للتحميل كمرجع.' : "Dana's original assessment file is also available to download as a reference."}</Alert>}
    {done && <Alert icon={<CheckCircleRoundedIcon />} severity="success">{ar ? 'تم حفظ المرحلة وفتح المرحلة التالية.' : 'This stage was saved and the next stage is now unlocked.'}</Alert>}{error && <Alert severity="error">{error}</Alert>}
    {assessmentKey === 'strengths' && <Strengths ar={ar} selected={selected} toggle={toggle} watched={watched} setWatched={setWatched} lessons={allLessons} />}
    {assessmentKey === 'interests' && <Interests ar={ar} ratings={ratings} setRatings={setRatings} />}
    {assessmentKey === 'values' && <Values ar={ar} selected={selected} toggle={toggle} ratings={ratings} setRatings={setRatings} />}
    {assessmentKey === 'purpose' && <Purpose ar={ar} reflections={reflections} setReflections={setReflections} statement={statement} setStatement={setStatement} />}
    <Paper sx={{ position: 'sticky', bottom: 16, p: 2, borderRadius: 3, boxShadow: '0 18px 60px rgba(16,45,91,.2)', zIndex: 4 }}><Stack direction={{ xs: 'column', sm: 'row' }} alignItems="center" justifyContent="space-between" spacing={2}><Typography fontWeight={900}>{progress}/{required} {ar ? 'مكتمل' : 'complete'}</Typography><Button disabled={busy || progress < required} onClick={submit} variant="contained" size="large">{busy ? <CircularProgress size={22} color="inherit" /> : (ar ? 'حفظ وإكمال المرحلة' : 'Save and complete stage')}</Button></Stack></Paper>
  </Stack>;
}

function Strengths({ ar, selected, toggle, watched, setWatched, lessons }) { const slug = (value) => value.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, ''); return <Stack spacing={3}><Alert severity="info" action={<Button component="a" href="https://www.viacharacter.org/account/register" target="_blank" rel="noreferrer" color="inherit">{ar ? 'فتح VIA' : 'Open VIA'}</Button>}>{ar ? 'أكمل اختبار VIA الخارجي، ثم اختر أعلى خمس نقاط قوة.' : 'Complete the external VIA assessment, then select your top five strengths.'}</Alert><Grid container spacing={1.2}>{STRENGTHS.map((item) => <Grid key={item} size={{ xs: 12, sm: 6, md: 4 }}><Paper variant="outlined" onClick={() => toggle(item)} sx={choiceStyle(selected.includes(item))}><Stack direction="row" alignItems="center"><Checkbox checked={selected.includes(item)} /><Typography fontWeight={selected.includes(item) ? 900 : 600}>{item}</Typography></Stack></Paper></Grid>)}</Grid>{selected.length === 5 && <><Divider /><Typography variant="h4" fontWeight={900}>{ar ? 'فيديوهات نقاط قوتك' : 'Your strength videos'}</Typography><Grid container spacing={2}>{selected.map((item, i) => { const videoUrl = lessons.find((lesson) => lesson.id === `strength-${slug(item)}`)?.contentUrl; return <Grid key={item} size={{ xs: 12, md: 6 }}><Card variant="outlined">{videoUrl ? <Box component="video" src={videoUrl} controls onEnded={() => setWatched({ ...watched, [i + 1]: true })} sx={{ width: '100%', height: 220, bgcolor: '#071226' }} /> : <Box sx={{ height: 150, display: 'grid', placeItems: 'center', color: 'white', bgcolor: '#102d5b' }}><Stack alignItems="center"><PlayCircleRoundedIcon sx={{ fontSize: 58 }} /><Typography variant="caption">{ar ? 'بانتظار رفع الفيديو' : 'Waiting for video upload'}</Typography></Stack></Box>}<CardContent><Typography fontWeight={900}>{item}</Typography><FormControlLabel control={<Checkbox disabled={!videoUrl} checked={Boolean(watched[i + 1])} onChange={(event) => setWatched({ ...watched, [i + 1]: event.target.checked })} />} label={ar ? 'شاهدت الفيديو كاملاً' : 'I watched the complete video'} /></CardContent></Card></Grid>})}</Grid></>}</Stack>; }

function Interests({ ar, ratings, setRatings }) { return <Stack spacing={1.5}>{assessmentData.interests.map((item) => <Paper key={item.id} variant="outlined" sx={{ p: 2.2 }}><Grid container spacing={2} alignItems="center"><Grid size={{ xs: 12, md: 8 }}><Typography fontWeight={800}>{item.id}. {ar ? item.ar : item.en}</Typography><Chip label={TYPES[item.type]} size="small" sx={{ mt: 1 }} /></Grid><Grid size={{ xs: 12, md: 4 }}><TextField select fullWidth size="small" label={ar ? 'درجة اهتمامي' : 'My interest'} value={ratings[`rating-${item.id}`] ?? ''} onChange={(event) => setRatings({ ...ratings, [`rating-${item.id}`]: event.target.value })}><MenuItem value="0">{ar ? 'لا أحب' : 'Dislike'}</MenuItem><MenuItem value="1">{ar ? 'محايد' : 'Indifferent'}</MenuItem><MenuItem value="2">{ar ? 'أحب' : 'Like'}</MenuItem></TextField></Grid></Grid></Paper>)}</Stack>; }

function Values({ ar, selected, toggle, ratings, setRatings }) { return <Stack spacing={1.5}>{assessmentData.values.map((item) => <Paper key={item.id} variant="outlined" sx={{ p: 2.2 }}><Grid container spacing={2} alignItems="center"><Grid size={{ xs: 12, md: 7 }}><Typography fontWeight={900}>{item.id}. {ar ? item.ar : item.en}</Typography><Typography variant="body2" color="text.secondary" sx={{ mt: .5 }}>{ar ? item.descriptionAr : item.descriptionEn}</Typography></Grid><Grid size={{ xs: 12, md: 5 }}><TextField select fullWidth size="small" label={ar ? 'مدى أهمية القيمة' : 'How much I value this'} value={ratings[`rating-${item.id}`] ?? ''} onChange={(event) => setRatings({ ...ratings, [`rating-${item.id}`]: event.target.value })}>{[5,4,3,2,1].map((n) => <MenuItem key={n} value={String(n)}>{n} — {(ar ? ['','غير مهمة','نادراً','أحياناً','غالباً','دائماً'] : ['','Never valued','Seldom valued','Sometimes valued','Often valued','Always valued'])[n]}</MenuItem>)}</TextField></Grid></Grid></Paper>)}<Divider sx={{ my: 2 }} /><Typography variant="h4" fontWeight={900}>{ar ? 'اختر أهم خمس قيم' : 'Select your top five values'}</Typography><Grid container spacing={1.2}>{assessmentData.values.map((item) => { const active = selected.includes(item.en); return <Grid key={item.id} size={{ xs: 12, sm: 6, md: 4 }}><Paper variant="outlined" onClick={() => toggle(item.en)} sx={choiceStyle(active)}><Stack direction="row" alignItems="center"><Checkbox checked={active} /><Typography fontWeight={active ? 900 : 600}>{ar ? item.ar : item.en}</Typography></Stack></Paper></Grid>})}</Grid></Stack>; }

function Purpose({ ar, reflections, setReflections, statement, setStatement }) { return <Stack spacing={2.2}>{assessmentData.purposeQuestions.map((item, i) => <TextField key={item.id} fullWidth multiline minRows={i === 4 || i === 5 ? 6 : 3} label={`${i + 1}. ${ar ? item.ar : item.en}`} value={reflections[item.id]} onChange={(event) => setReflections({ ...reflections, [item.id]: event.target.value })} />)}<Paper sx={{ p: 3, bgcolor: 'rgba(184,207,124,.16)' }}><Typography fontWeight={900}>{ar ? 'صيغة بيان الغاية' : 'Purpose statement format'}</Typography><Typography sx={{ mt: 1 }}>{ar ? 'غرض حياتي هو أن __________ من خلال __________' : 'My life purpose is to __________ through __________'}</Typography></Paper><TextField fullWidth multiline minRows={5} label={ar ? 'بيان غاية حياتي' : 'My life purpose statement'} value={statement} onChange={(event) => setStatement(event.target.value)} /></Stack>; }

function descriptions(key, ar) { const copy = { strengths: ['أكمل VIA، اختر خمس نقاط قوة، وشاهد فيديو كل نتيجة.','Complete VIA, select five strengths, and watch each result video.'], interests: ['قيّم الأنشطة الـ72 الأصلية وسيحسب النظام أعلى ثلاثة أنماط.','Rate the original 72 activities and the system will calculate your top three types.'], values: ['قيّم القيم الـ54 ثم حدد أعلى خمس أولويات.','Rate all 54 values, then identify your top five priorities.'], purpose: ['أكمل أسئلة التأمل واكتب بيان الغاية بصيغة الجوهر والتعبير.','Complete the reflection questions and write your essence and expression statement.'] }; return copy[key]?.[ar ? 0 : 1]; }
function choiceStyle(active) { return { p: 1.6, cursor: 'pointer', borderColor: active ? 'primary.main' : 'divider', bgcolor: active ? 'rgba(47,79,35,.08)' : 'white' }; }
