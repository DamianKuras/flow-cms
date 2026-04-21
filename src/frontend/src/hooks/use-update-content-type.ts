import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useApiClient } from "@/hooks/use-api-client";
import { toast } from "sonner";

export type UpdateFieldData = {
  existingId?: string;
  name: string;
  type: string;
  isRequired: boolean;
  validationRules: Array<{ type: string; parameters: Record<string, string> }>;
  transformationRules: Array<{
    type: string;
    parameters: Record<string, string>;
  }>;
};

export type UpdateContentTypeData = {
  id: string;
  fields: UpdateFieldData[];
};

export function useUpdateContentType() {
  const api = useApiClient();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, fields }: UpdateContentTypeData) => {
      const response = await api.put(`/content-types/${id}/draft`, { fields });
      return response.data as { id: string };
    },
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: ["content-type", variables.id] });
      toast.success("Draft updated successfully!");
    },
    onError: (error: any) => {
      const errorMessage =
        error.response?.data?.message ||
        error.message ||
        "Failed to update draft";
      toast.error("Failed to update draft", { description: String(errorMessage) });
    },
  });
}
