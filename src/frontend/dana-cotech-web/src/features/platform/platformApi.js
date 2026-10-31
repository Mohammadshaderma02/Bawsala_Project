import { httpClient } from '../../api/httpClient';

const SESSION_KEY = 'dana-client-session';

export function getSession() {
  try { return JSON.parse(localStorage.getItem(SESSION_KEY)); } catch { return null; }
}

export function saveSession(session) { localStorage.setItem(SESSION_KEY, JSON.stringify(session)); }
export function clearSession() { localStorage.removeItem(SESSION_KEY); }

export function authConfig() {
  const token = getSession()?.token;
  return { headers: { Authorization: token ? `Bearer ${token}` : '' } };
}

function unwrap(response) {
  if (!response.data?.success) throw new Error(response.data?.message || 'Request failed');
  return response.data.data;
}

export async function loginAccount(email, password) { return unwrap(await httpClient.post('/auth/login', { email, password })); }
export async function registerAccount(name, email, password) { return unwrap(await httpClient.post('/auth/register', { name, email, password })); }
export async function verifyEmail(token) { return unwrap(await httpClient.post('/auth/verify-email', { token })); }
export async function requestPasswordReset(email) { return unwrap(await httpClient.post('/auth/forgot-password', { email })); }
export async function resetPassword(token, newPassword) { return unwrap(await httpClient.post('/auth/reset-password', { token, newPassword })); }
export async function createUser(name, email, password, role) { return unwrap(await httpClient.post('/auth/users', { name, email, password, role }, authConfig())); }
export async function getAdminUsers() { return unwrap(await httpClient.get('/auth/users', authConfig())); }
export async function getAdminUserDeletionLogs() { return unwrap(await httpClient.get('/auth/users/deletion-log', authConfig())); }
export async function deleteAdminUser(userId) { return unwrap(await httpClient.delete(`/auth/users/${userId}`, authConfig())); }
export async function updateUserRole(userId, role) { return unwrap(await httpClient.patch(`/auth/users/${userId}/role`, { role }, authConfig())); }
export async function getCourseStudents(productSlug) { return unwrap(await httpClient.get(`/platform/admin/courses/${encodeURIComponent(productSlug)}/students`, authConfig())); }
export async function getCatalog(language) { return unwrap(await httpClient.get('/platform/catalog', { params: { lang: language } })); }
export async function getJourney(language) { return unwrap(await httpClient.get('/platform/journey', { ...authConfig(), params: { lang: language } })); }
export async function enroll(productSlug, language) { return unwrap(await httpClient.post('/platform/checkout', { productSlug, language }, authConfig())); }
export async function createStripeCheckoutSession(productSlug, language) { return unwrap(await httpClient.post('/payments/checkout-session', { productSlug, language }, authConfig())); }
export async function completeMepsPayment(paymentId, resultIndicator, language) { return unwrap(await httpClient.get('/payments/meps/complete', { ...authConfig(), params: { paymentId, resultIndicator, language } })); }
export async function guestCoachingCheckout(name, email, sessionId, language) { return unwrap(await httpClient.post('/payments/meps/guest-checkout-session', { name, email, sessionId, language })); }
export async function completeGuestMepsPayment(paymentId, resultIndicator, language) { return unwrap(await httpClient.get('/payments/meps/guest-complete', { params: { paymentId, resultIndicator, language } })); }
export async function completeLesson(lessonId, language) { return unwrap(await httpClient.post(`/platform/lessons/${lessonId}/complete`, { language }, authConfig())); }
export async function saveAssessment(key, answers, language) { return unwrap(await httpClient.post(`/platform/assessments/${key}`, { answers, language }, authConfig())); }
export async function bookSession(sessionType, startsAtUtc, language) { return unwrap(await httpClient.post('/platform/bookings', { sessionType, startsAtUtc, language }, authConfig())); }
export async function getAvailableSessions() { return unwrap(await httpClient.get('/platform/sessions')); }
export async function bookAvailableSession(sessionId, language) { return unwrap(await httpClient.post(`/platform/sessions/${sessionId}/book`, { language }, authConfig())); }
export async function getAdminSessions() { return unwrap(await httpClient.get('/platform/admin/sessions', authConfig())); }
export async function getAdminNotifications(filters) { return unwrap(await httpClient.get('/platform/admin/notifications', { ...authConfig(), params: filters })); }
export async function createPlatformSession(session) { return unwrap(await httpClient.post('/platform/admin/sessions', session, authConfig())); }
export async function getAdminPayments() { return unwrap(await httpClient.get('/platform/admin/payments', authConfig())); }
export async function refundAdminPayment(paymentId, reason) { return unwrap(await httpClient.post(`/platform/admin/payments/${paymentId}/refund`, { reason }, authConfig())); }
export async function getProgramClients() { return unwrap(await httpClient.get('/platform/admin/program-clients', authConfig())); }
export async function reviewCareerProfile(userId, notes, release) { return unwrap(await httpClient.post(`/platform/admin/program-clients/${userId}/profile`, { notes, release }, authConfig())); }
export async function downloadCareerProfile(language) { const response = await httpClient.get('/platform/career-profile.pdf', { ...authConfig(), params: { lang: language }, responseType: 'blob' }); const url = URL.createObjectURL(response.data); const link = document.createElement('a'); link.href = url; link.download = 'career-profile.pdf'; link.click(); URL.revokeObjectURL(url); }
