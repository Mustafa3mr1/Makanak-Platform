import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '@/features/auth/store/authStore';
import type { ProblemDetails } from './types';

export class ApiError extends Error {
  public status: number;
  public code: string;
  public detail: string;
  public errors?: Record<string, string[]>;
  public isConcurrencyConflict: boolean;

  constructor(status: number, code: string, detail: string, errors?: Record<string, string[]>) {
    super(detail);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
    this.detail = detail;
    this.errors = errors;
    this.isConcurrencyConflict = status === 409 || code === 'concurrency_conflict';
  }
}

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request Interceptor: Inject JWT Bearer Token
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = useAuthStore.getState().token;
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor: Parse RFC 7807 ProblemDetails and handle global errors
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ProblemDetails>) => {
    if (!error.response) {
      // Network or connection dropped
      return Promise.reject(
        new ApiError(0, 'network_error', 'Unable to reach Makanak server. Please check your internet connection.')
      );
    }

    const { status, data } = error.response;

    let code = data?.code || 'error';
    let detail = data?.detail || error.message || 'An unexpected error occurred.';
    const errors = data?.errors;

    // Concurrency conflict mapping
    if (status === 409 || code === 'concurrency_conflict') {
      code = 'concurrency_conflict';
      detail = 'This item was just updated by another user. Please refresh.';
    }

    // 401 Unauthorized Session Expiration
    if (status === 401) {
      useAuthStore.getState().logout();

      if (typeof window !== 'undefined') {
        const currentPath = window.location.pathname + window.location.search;
        if (!currentPath.startsWith('/login') && !currentPath.startsWith('/register')) {
          window.location.href = `/login?returnUrl=${encodeURIComponent(currentPath)}`;
        }
      }
    }

    return Promise.reject(new ApiError(status, code, detail, errors));
  }
);
