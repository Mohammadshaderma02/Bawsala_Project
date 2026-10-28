import { httpClient } from '../../api/httpClient';
import { authConfig, getSession } from '../platform/platformApi';

export async function getCmsLandingContent(language) {
  const response = await httpClient.get('/content/landing', { params: { lang: language } });
  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message || 'Unable to load CMS content');
  }
  return response.data.data;
}

export async function uploadCmsAsset(file, altText = '') {
  const form = new FormData();
  form.append('file', file);
  form.append('altText', altText);
  const response = await httpClient.post('/assets', form, {
    headers: { Authorization: `Bearer ${getSession()?.token ?? ''}`, 'Content-Type': 'multipart/form-data' },
  });
  if (!response.data.success) throw new Error(response.data.message || 'Unable to upload asset');
  const apiBase = new URL(httpClient.defaults.baseURL, window.location.origin);
  return new URL(response.data.data.url, apiBase.origin).toString();
}

export async function saveCmsLandingContent(language, content) {
  const response = await httpClient.put(`/content/landing/${language}`, content, authConfig());
  if (!response.data.success) {
    throw new Error(response.data.message || 'Unable to publish CMS content');
  }
  return response.data.data;
}
