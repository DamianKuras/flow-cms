import { useQuery } from "@tanstack/react-query";
import { useApiClient } from "@/hooks/use-api-client";

export type MigrationJobDto = {
  id: string;
  fromSchemaId: string;
  toSchemaId: string;
  mode: "Lazy" | "Eager";
  status: "Pending" | "Running" | "Completed" | "Failed";
  createdBy: string;
  createdAt: string;
  totalItemsCount: number;
  migratedItemsCount: number;
  failedItemsCount: number;
};

export function useMigrationJobs(contentTypeName: string) {
  const api = useApiClient();

  return useQuery({
    queryKey: ["migration-jobs", contentTypeName],
    queryFn: async () => {
      const response = await api.get<MigrationJobDto[]>(
        `/content-types/${contentTypeName}/migration-jobs`,
      );
      return response.data;
    },
    enabled: !!contentTypeName,
    refetchInterval: (query) => {
      const jobs = query.state.data;
      const hasActiveJob = jobs?.some(
        (j) => j.status === "Pending" || j.status === "Running",
      );
      return hasActiveJob ? 3000 : false;
    },
  });
}
