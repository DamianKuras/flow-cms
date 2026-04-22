import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useApiClient } from "@/hooks/use-api-client";
import { useNavigate } from "@tanstack/react-router";
import { toast } from "sonner";

export type MigrationMode = "Lazy" | "Eager";

export function usePublishContentType(contentTypeName: string) {
  const api = useApiClient();
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (migrationMode: MigrationMode) => {
      const response = await api.post<{ id: string }>(
        `/content-types/${contentTypeName}/publish`,
        { migrationMode },
      );
      return response.data.id;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["content-types"] });
      queryClient.invalidateQueries({
        queryKey: ["migration-jobs", contentTypeName],
      });
      navigate({ to: "/content-types/$name", params: { name: contentTypeName } });
      toast.success("Content type published successfully!");
    },
    onError: (error: any) => {
      const errorMessage =
        error.response?.data?.message ||
        error.message ||
        "Failed to publish content type";
      toast.error("Failed to publish", { description: String(errorMessage) });
    },
  });
}
