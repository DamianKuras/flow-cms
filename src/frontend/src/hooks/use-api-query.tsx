import {
  useQuery,
  useMutation,
  type UseQueryOptions,
  type UseMutationOptions,
} from "@tanstack/react-query";
import { AxiosError } from "axios";
import { useApiClient } from "./use-api-client";

export function useApiQuery<TData = unknown, TError = AxiosError>(
  queryKey: any[],
  endpoint: string,
  options?: Omit<UseQueryOptions<TData, TError>, "queryKey" | "queryFn">,
) {
  const api = useApiClient();

  return useQuery<TData, TError>({
    queryKey,
    queryFn: async () => {
      const response = await api.get<TData>(endpoint);
      return response.data;
    },
    ...options,
  });
}
export function useApiMutation<
  TData = unknown,
  TVariables = unknown,
  TError = AxiosError,
>(
  endpoint: string | ((variables: TVariables) => string),
  method: "post" | "patch" | "put" | "delete" = "post",
  options?: UseMutationOptions<TData, TError, TVariables>,
) {
  const api = useApiClient();

  return useMutation<TData, TError, TVariables>({
    mutationFn: async (variables: TVariables) => {
      const url =
        typeof endpoint === "function" ? endpoint(variables) : endpoint;
      const response =
        method === "delete"
          ? await api.delete<TData>(url, { data: variables })
          : await api[method]<TData>(url, variables);
      return response.data;
    },
    ...options,
  });
}
