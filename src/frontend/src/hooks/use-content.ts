import { useQuery } from "@tanstack/react-query";
import { useApiClient } from "@/hooks/use-api-client";

type Content = {
  id: string;
  title: string;
  fields_values?: Record<string, string>;
};

type ContentType = {
  id: string;
  name: string;
  status: string;
  version: string;
};

type ContentListResponse = {
  pagedList: Array<Content>;
  totalCount: number;
};

type Response = {
  Content: Array<Content>;
  Type: ContentType;
};

export function useContent(id: string) {
  const api = useApiClient();

  return useQuery({
    queryKey: ["content", id],
    queryFn: async (): Promise<Response> => {
      const [typeResponse, contentResponse] = await Promise.all([
        api.get<ContentType>(`/content-types/${id}`),
        api.get<ContentListResponse>(`/content-items?contentTypeId=${id}`),
      ]);

      const contentItems = contentResponse.data.pagedList || [];

      return {
        Content: contentItems,
        Type: typeResponse.data,
      };
    },
    enabled: !!id,
  });
}
