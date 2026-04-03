import axios, {
  type AxiosInstance,
  type InternalAxiosRequestConfig,
  AxiosError,
} from "axios";

const API_BASE_URL =
  import.meta.env.VITE_CMS_API_URL || "http://localhost:5000";

interface ApiClientConfig {
  getAccessToken: () => string | null;
  refreshToken: () => Promise<string>;
  onAuthError?: () => void;
}

export function createApiClient(config: ApiClientConfig): AxiosInstance {
  const { getAccessToken, refreshToken, onAuthError } = config;

  const api = axios.create({
    baseURL: API_BASE_URL,
    withCredentials: true,
  });

  let isRefreshing = false;
  let refreshPromise: Promise<string> | null = null;

  api.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
      const token = getAccessToken();
      if (token) {
        config.headers.Authorization = `Bearer ${token}`;
      }
      return config;
    },
    (error) => Promise.reject(error),
  );

  api.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      const originalRequest = error.config as InternalAxiosRequestConfig & {
        _retry?: boolean;
      };

      if (error.response?.status === 401 && !originalRequest._retry) {
        originalRequest._retry = true;

        try {
          let newToken: string;
          if (isRefreshing && refreshPromise) {
            newToken = await refreshPromise;
          } else {
            isRefreshing = true;
            refreshPromise = refreshToken();
            newToken = await refreshPromise;
            isRefreshing = false;
            refreshPromise = null;
          }

          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return api(originalRequest);
        } catch (refreshError) {
          isRefreshing = false;
          refreshPromise = null;
          if (onAuthError) {
            onAuthError();
          }
          return Promise.reject(refreshError);
        }
      }

      return Promise.reject(error);
    },
  );

  return api;
}
