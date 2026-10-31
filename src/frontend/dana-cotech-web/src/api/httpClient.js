import axios from 'axios';

export const httpClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7156/api',
  //timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
  },
});
