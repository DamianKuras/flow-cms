import { useApiQuery } from "./use-api-query";

export interface PagedUser {
  id: string;
  email: string;
  displayName: string;
  status: string;
  createdAt: string;
}

// describes the full paginated response
export interface UsersResponse {
  pagedList: PagedUser[];
  totalCount: number;
}

export function useUsers(pageNumber = 1, pageSize = 10) {
  return useApiQuery<UsersResponse>(
    ["users", { pageNumber, pageSize }],
    `/users?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    { staleTime: 30 * 1000 },
  );
}
