import { useQuery } from "@tanstack/react-query";
import { useApiClient } from "@/hooks/use-api-client";

export type ContentTypeSummary = {
  name: string;
  publishedId: string | null;
  publishedVersion: number | null;
  draftId: string | null;
};

export function useContentTypeSummaries() {
  const api = useApiClient();
  return useQuery({
    queryKey: ["content-type-summaries"],
    queryFn: async () => {
      const response = await api.get<ContentTypeSummary[]>("/content-types/summaries");
      return response.data;
    },
    staleTime: 30 * 1000,
  });
}

export function useContentTypeSummaryByName(name: string) {
  const { data, ...rest } = useContentTypeSummaries();
  const summary = data?.find((s) => s.name === name) ?? null;
  return { summary, ...rest };
}
