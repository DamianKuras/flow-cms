import { useQuery } from "@tanstack/react-query";
import { useApiClient } from "@/hooks/use-api-client";

export type ValidationRuleDto = {
  type: string;
  parameters?: Record<string, any>;
};

export type TransformationRuleDto = {
  type: string;
  parameters: Record<string, any>;
};

export type FieldDto = {
  id: string; // Guid from backend
  name: string;
  type: string;
  isRequired: boolean;
  validationRules?: ValidationRuleDto[];
  transformationRules?: TransformationRuleDto[];
};

export type ContentTypeDto = {
  id: string; // Guid from backend
  name: string;
  status: string;
  fields: FieldDto[];
  version: number;
};

export function useContentType(id: string) {
  const api = useApiClient();

  return useQuery({
    queryKey: ["content-type", id],
    queryFn: async () => {
      const response = await api.get<ContentTypeDto>(`/content-types/${id}`);
      return {
        contentType: response.data,
      };
    },
    enabled: !!id,
  });
}
