import { useMemo } from "react";
import { useRouter } from "@tanstack/react-router";
import { useAuth } from "@/contexts/AuthContext";
import { createApiClient } from "@/lib/api";

export function useApiClient() {
  const { accessToken, refreshToken, logout } = useAuth();
  const router = useRouter();

  const api = useMemo(() => {
    return createApiClient({
      getAccessToken: () => accessToken,
      refreshToken: refreshToken,
      onAuthError: () => {
        logout();

        router.navigate({
          to: "/login",
          search: { redirect: router.state.location.href },
        });
      },
    });
  }, [accessToken, refreshToken, logout, router]);

  return api;
}
