import { httpClient } from '../../api/httpClient';

export async function getLandingContent(language) {
  const response = await httpClient.get('/content/landing', {
    params: { lang: language },
  });

  if (!response.data.success || !response.data.data) {
    throw new Error(response.data.message || 'Unable to load landing content');
  }

  return response.data.data;
}
