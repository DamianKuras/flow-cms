import React, {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  useRef,
} from "react";

const API_BASE = import.meta.env.VITE_CMS_API_URL || "http://localhost:5000";

interface User {
  id: string;
  username: string;
  email: string;
}

interface SignInResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: string;
}

export interface AuthState {
  isAuthenticated: boolean;
  isLoading: boolean;
  user: User | null;
  accessToken: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<string>;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

const ACCESS_TOKEN_EXPIRY_KEY = "access-token-expiry";
const REFRESH_BUFFER_MS = 60000; // Refresh 1 minute before expiry

function getExpirationTimestamp(expiresInSeconds: string): number {
  const seconds = parseInt(expiresInSeconds, 10);
  return Date.now() + seconds * 1000;
}

function isTokenExpired(expiryTimestamp: number | null): boolean {
  if (!expiryTimestamp) return true;
  return Date.now() >= expiryTimestamp;
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const refreshTimeoutRef = useRef<number | null>(null);
  const isRefreshingRef = useRef(false);
  const initializingRef = useRef(false);

  const refreshAccessToken = useCallback(async (): Promise<string> => {
    if (isRefreshingRef.current) {
      throw new Error("Token refresh already in progress");
    }

    isRefreshingRef.current = true;

    try {
      const response = await fetch(`${API_BASE}/auth/refresh-token`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({}),
      });

      if (!response.ok) {
        throw new Error("Token refresh failed");
      }

      const data: SignInResponse = await response.json();
      const expiryTimestamp = getExpirationTimestamp(data.expiresIn);

      setAccessToken(data.accessToken);
      localStorage.setItem(ACCESS_TOKEN_EXPIRY_KEY, expiryTimestamp.toString());
      scheduleTokenRefresh(expiryTimestamp);

      return data.accessToken;
    } catch (error) {
      localStorage.removeItem(ACCESS_TOKEN_EXPIRY_KEY);
      setAccessToken(null);
      setIsAuthenticated(false);
      throw error;
    } finally {
      isRefreshingRef.current = false;
    }
  }, []);

  const scheduleTokenRefresh = useCallback(
    (expiryTimestamp: number) => {
      if (refreshTimeoutRef.current) {
        clearTimeout(refreshTimeoutRef.current);
      }

      const timeUntilRefresh = expiryTimestamp - Date.now() - REFRESH_BUFFER_MS;

      if (timeUntilRefresh > 0) {
        refreshTimeoutRef.current = window.setTimeout(() => {
          refreshAccessToken().catch(() => {});
        }, timeUntilRefresh);
      }
    },
    [refreshAccessToken],
  );

  useEffect(() => {
    const initializeAuth = async () => {
      if (initializingRef.current) return;

      initializingRef.current = true;

      try {
        const expiryTimestampStr = localStorage.getItem(ACCESS_TOKEN_EXPIRY_KEY);

        if (!expiryTimestampStr) {
          setIsAuthenticated(false);
          setIsLoading(false);
          return;
        }

        const expiryTimestamp = parseInt(expiryTimestampStr, 10);

        if (isTokenExpired(expiryTimestamp)) {
          localStorage.removeItem(ACCESS_TOKEN_EXPIRY_KEY);
          setIsAuthenticated(false);
          setIsLoading(false);
          return;
        }

        await refreshAccessToken();
        setIsAuthenticated(true);
      } catch {
        localStorage.removeItem(ACCESS_TOKEN_EXPIRY_KEY);
        setIsAuthenticated(false);
      } finally {
        setIsLoading(false);
        initializingRef.current = false;
      }
    };

    initializeAuth();
  }, []);

  useEffect(() => {
    return () => {
      if (refreshTimeoutRef.current) {
        clearTimeout(refreshTimeoutRef.current);
      }
    };
  }, []);

  const login = async (email: string, password: string) => {
    try {
      const response = await fetch(`${API_BASE}/auth/sign-in`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ email, password }),
      });

      if (!response.ok) {
        const error = await response
          .json()
          .catch(() => ({ message: "Authentication failed" }));
        throw new Error(error.message || "Authentication failed");
      }

      const data: SignInResponse = await response.json();
      const expiryTimestamp = getExpirationTimestamp(data.expiresIn);

      setAccessToken(data.accessToken);
      localStorage.setItem(ACCESS_TOKEN_EXPIRY_KEY, expiryTimestamp.toString());
      setIsAuthenticated(true);
      scheduleTokenRefresh(expiryTimestamp);
    } catch (error) {
      throw error;
    }
  };

  const logout = async () => {
    try {
      if (refreshTimeoutRef.current) {
        clearTimeout(refreshTimeoutRef.current);
      }

      await fetch(`${API_BASE}/auth/sign-out`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({}),
      });
    } finally {
      setUser(null);
      setAccessToken(null);
      setIsAuthenticated(false);
      localStorage.removeItem(ACCESS_TOKEN_EXPIRY_KEY);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        Loading...
      </div>
    );
  }

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        isLoading,
        user,
        accessToken,
        login,
        logout,
        refreshToken: refreshAccessToken,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
