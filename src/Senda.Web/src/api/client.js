import axios from 'axios';
import { useTenantStore } from '../stores/tenant';

const apiClient = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

apiClient.interceptors.request.use((config) => {
  const tenantStore = useTenantStore();
  if (tenantStore.currentTenantId) {
    config.headers['X-Tenant-Id'] = tenantStore.currentTenantId;
  }
  return config;
}, (error) => {
  return Promise.reject(error);
});

export default apiClient;
