import { useQueryClient } from "@tanstack/react-query";
import { useApiMutation, useApiQuery } from "./use-api-query";

export interface RoleListItem {
  id: string;
  name: string;
}

export interface RolesResponse {
  roles: RoleListItem[];
}

export type CmsAction = "read" | "list" | "create" | "update" | "delete";
export type ResourceType = "contentType" | "contentItem" | "field";
export type PermissionScope = "allow" | "deny";

export interface PermissionDto {
  action: CmsAction;
  resourceType: ResourceType;
  resourceId: string | null;
  resourceName: string | null;
  scope: PermissionScope;
}

export interface RoleDetail {
  id: string;
  name: string;
  permissions: PermissionDto[];
}

export interface AddPermissionPayload {
  roleId: string;
  action: CmsAction;
  resourceType: ResourceType;
  resourceId: string | null;
  scope: PermissionScope;
}

export interface RemovePermissionPayload {
  roleId: string;
  action: CmsAction;
  resourceType: ResourceType;
  resourceId: string | null;
  scope: PermissionScope;
}

export function useRoles() {
  return useApiQuery<RolesResponse>(["roles"], "/roles", {
    staleTime: 30 * 1000,
  });
}

export function useRole(id: string) {
  return useApiQuery<RoleDetail>(["roles", id], `/roles/${id}`, {
    staleTime: 30 * 1000,
  });
}

export function useCreateRole() {
  const queryClient = useQueryClient();
  return useApiMutation<{ id: string }, { name: string }>(
    "/roles",
    "post",
    {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ["roles"] });
      },
    },
  );
}

export function useDeleteRole() {
  const queryClient = useQueryClient();
  return useApiMutation<void, { id: string }>(
    (vars) => `/roles/${vars.id}`,
    "delete",
    {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ["roles"] });
      },
    },
  );
}

export function useAddPermission() {
  const queryClient = useQueryClient();
  return useApiMutation<void, AddPermissionPayload>(
    (vars) => `/roles/${vars.roleId}/permissions`,
    "post",
    {
      onSuccess: (_data, vars) => {
        queryClient.invalidateQueries({ queryKey: ["roles", vars.roleId] });
      },
    },
  );
}

export function useRemovePermission() {
  const queryClient = useQueryClient();
  return useApiMutation<void, RemovePermissionPayload>(
    (vars) => `/roles/${vars.roleId}/permissions`,
    "delete",
    {
      onSuccess: (_data, vars) => {
        queryClient.invalidateQueries({ queryKey: ["roles", vars.roleId] });
      },
    },
  );
}

export function useAssignRole() {
  const queryClient = useQueryClient();
  return useApiMutation<void, { roleId: string; userId: string }>(
    (vars) => `/roles/${vars.roleId}/users/${vars.userId}`,
    "post",
    {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ["roles"] });
      },
    },
  );
}

export function useRemoveRoleFromUser() {
  const queryClient = useQueryClient();
  return useApiMutation<void, { roleId: string; userId: string }>(
    (vars) => `/roles/${vars.roleId}/users/${vars.userId}`,
    "delete",
    {
      onSuccess: () => {
        queryClient.invalidateQueries({ queryKey: ["roles"] });
      },
    },
  );
}
