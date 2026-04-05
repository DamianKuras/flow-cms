import type { PagedContentType } from "@/content-types/types";
import { useApiMutation, useApiQuery } from "@/hooks/use-api-query";

interface ContentTypesResponse {
  data: PagedContentType[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export function useContentTypes(
  pageNumber = 1,
  pageSize = 10,
  sort = "",
  filter = "",
  status = "",
) {
  const params = new URLSearchParams({
    pageNumber: pageNumber.toString(),
    pageSize: pageSize.toString(),
  });

  if (sort) params.append("sort", sort);
  if (filter) params.append("filter", filter);
  if (status) params.append("status", status);
  return useApiQuery<ContentTypesResponse>(
    ["contentTypes", { pageNumber, pageSize, sort, filter, status }],
    `/content-types?${params.toString()}`,

    {
      staleTime: 30 * 1000,
    },
  );
}

export function usePublishContentType(onSuccess?: () => void) {
  return useApiMutation(
    (id: string) => `/content-types/${id}/publish`,
    "post",
    {
      onSuccess,
    },
  );
}

export function useUnpublishContentType(onSuccess?: () => void) {
  return useApiMutation(
    (id: string) => `/content-types/${id}/unpublish`,
    "post",
    {
      onSuccess,
    },
  );
}

export function useArchiveContentType(onSuccess?: () => void) {
  return useApiMutation(
    (id: string) => `/content-types/${id}/archive`,
    "post",
    {
      onSuccess,
    },
  );
}
