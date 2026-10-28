import axios from 'axios';

export const httpClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5115/api',
  //timeout: 15000,
  headers: {
    'Content-Type': 'application/json',
  },
});
