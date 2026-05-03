import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useApiClient } from "@/hooks/use-api-client";
import { toast } from "sonner";

export type CreateContentTypeData = {
  name: string;
  fields: Array<{
    name: string;
    type: string;
    isRequired: boolean;
    validationRules: Array<{
      type: string;
      parameters: Record<string, string | number | boolean>;
    }>;
    transformationRules: Array<{
      type: string;
      parameters: Record<string, string | number | boolean>;
    }>;
  }>;
};

export function useCreateContentType() {
  const api = useApiClient();
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (data: CreateContentTypeData) => {
      const response = await api.post("/content-types", data);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["content-types"] });
      queryClient.invalidateQueries({ queryKey: ["content-type-summaries"] });
      toast.success("Content type created successfully!");
    },
    onError: (error: any) => {
      const errorMessage =
        error.response?.data?.message ||
        error.message ||
        "Failed to create content type";
      toast.error("Failed to create content type", {
        description: String(errorMessage),
      });
    },
  });
}
