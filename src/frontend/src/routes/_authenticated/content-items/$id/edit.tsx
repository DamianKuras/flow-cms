import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useContentType, type FieldDto } from "@/hooks/use-content-type";
import { useApiClient } from "@/hooks/use-api-client";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { AlertCircle, ArrowLeft, Loader2 } from "lucide-react";
import { useState, useEffect } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";

export const Route = createFileRoute("/_authenticated/content-items/$id/edit")({
  component: RouteComponent,
});

type ContentItem = {
  id: string;
  name: string;
  contentTypeId: string;
  status: string;
  values: Record<string, { value: any }>;
};

type ContentTypeDto = {
  id: string;
  name: string;
  status: string;
  fields: FieldDto[];
  version: number;
};

type FormValues = {
  title: string;
  fieldValues: Record<string, any>;
};

function useContentItem(id: string) {
  const api = useApiClient();

  return useQuery({
    queryKey: ["content-item", id],
    queryFn: async (): Promise<ContentItem> => {
      const response = await api.get<ContentItem>(`/content-items/${id}`);
      return response.data;
    },
    enabled: !!id,
  });
}

function RouteComponent() {
  const { id } = Route.useParams();
  const navigate = useNavigate();
  const api = useApiClient();
  const queryClient = useQueryClient();

  const {
    data: contentItem,
    error: itemError,
    isLoading: itemLoading,
  } = useContentItem(id);

  const {
    data: contentTypeData,
    error: typeError,
    isLoading: typeLoading,
  } = useContentType(contentItem?.contentTypeId || "");

  const [formValues, setFormValues] = useState<FormValues>({
    title: "",
    fieldValues: {},
  });
  const [validationErrors, setValidationErrors] = useState<
    Record<string, string>
  >({});

  useEffect(() => {
    if (contentItem && contentTypeData?.contentType) {
      const contentType = contentTypeData.contentType;

      const fieldValues: Record<string, any> = {};

      if (contentItem.values) {
        contentType.fields.forEach((field) => {
          const fieldData = contentItem.values[field.name];
          if (fieldData) {
            fieldValues[field.id] = fieldData.value;
          }
        });
      }

      setFormValues({
        title: contentItem.name || "",
        fieldValues,
      });
    }
  }, [contentItem, contentTypeData]);

  const updateMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const payload = {
        title: values.title,
        contentTypeId: contentItem!.contentTypeId,
        values: values.fieldValues,
      };

      const response = await api.patch(`/content-items/${id}`, payload);
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["content-item", id] });
      queryClient.invalidateQueries({
        queryKey: ["content", contentItem?.contentTypeId],
      });
      navigate({ to: `/content-items/${id}` });
    },
    onError: () => {},
  });

  const handleInputChange = (fieldId: string, value: any) => {
    setFormValues((prev) => ({
      ...prev,
      fieldValues: {
        ...prev.fieldValues,
        [fieldId]: value,
      },
    }));

    if (validationErrors[fieldId]) {
      setValidationErrors((prev) => {
        const next = { ...prev };
        delete next[fieldId];
        return next;
      });
    }
  };

  const handleTitleChange = (value: string) => {
    setFormValues((prev) => ({ ...prev, title: value }));
    if (validationErrors.title) {
      setValidationErrors((prev) => {
        const next = { ...prev };
        delete next.title;
        return next;
      });
    }
  };

  const validateForm = (contentType: ContentTypeDto): boolean => {
    const errors: Record<string, string> = {};

    if (!formValues.title?.trim()) {
      errors.title = "Title is required";
    }

    contentType.fields.forEach((field) => {
      if (field.isRequired) {
        const value = formValues.fieldValues[field.id];
        if (value === undefined || value === null || value === "") {
          errors[field.id] = `${field.name} is required`;
        }
      }
    });

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (
      !contentTypeData?.contentType ||
      !validateForm(contentTypeData.contentType)
    ) {
      return;
    }

    updateMutation.mutate(formValues);
  };

  const isLoading = itemLoading || typeLoading;
  const error = itemError || typeError;

  if (isLoading) {
    return (
      <div className="flex-1 p-6">
        <div className="flex items-center justify-center min-h-[400px]">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex-1 p-6">
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error Loading Data</AlertTitle>
          <AlertDescription>
            {error.message ||
              "Could not load content item or content type information."}
          </AlertDescription>
        </Alert>
      </div>
    );
  }

  if (!contentItem || !contentTypeData?.contentType) {
    return (
      <div className="flex-1 p-6">
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Content Not Found</AlertTitle>
          <AlertDescription>
            The content item or content type could not be loaded.
          </AlertDescription>
        </Alert>
      </div>
    );
  }

  const contentType = contentTypeData.contentType;

  return (
    <div className="flex-1 space-y-6 p-6 max-w-4xl mx-auto">
      <div className="space-y-2">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate({ to: `/content-items/${id}` })}
        >
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back to Content Item
        </Button>
        <h1 className="text-3xl font-bold">Edit Content Item</h1>
        <p className="text-muted-foreground">
          Editing content of type:{" "}
          <span className="font-semibold">{contentType.name}</span>
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>Basic Information</CardTitle>
            <CardDescription>
              Edit the basic details for this content item
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="title">
                Title <span className="text-destructive">*</span>
              </Label>
              <Input
                id="title"
                value={formValues.title}
                onChange={(e) => handleTitleChange(e.target.value)}
                placeholder="Enter content title"
                className={validationErrors.title ? "border-destructive" : ""}
              />
              {validationErrors.title && (
                <p className="text-sm text-destructive">
                  {validationErrors.title}
                </p>
              )}
            </div>
          </CardContent>
        </Card>

        {contentType.fields.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle>Content Fields</CardTitle>
              <CardDescription>
                Edit values for the content fields
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {contentType.fields.map((field) => (
                <div key={field.id} className="space-y-2">
                  <Label htmlFor={field.id}>
                    {field.name}
                    {field.isRequired && (
                      <span className="text-destructive ml-1">*</span>
                    )}
                  </Label>

                  {(field.type === "Text" || field.type === "ShortText") && (
                    <Input
                      id={field.id}
                      value={formValues.fieldValues[field.id] || ""}
                      onChange={(e) =>
                        handleInputChange(field.id, e.target.value)
                      }
                      placeholder={`Enter ${field.name}`}
                      className={
                        validationErrors[field.id] ? "border-destructive" : ""
                      }
                    />
                  )}

                  {(field.type === "LongText" || field.type === "Richtext") && (
                    <Textarea
                      id={field.id}
                      value={formValues.fieldValues[field.id] || ""}
                      onChange={(e) =>
                        handleInputChange(field.id, e.target.value)
                      }
                      placeholder={`Enter ${field.name}`}
                      rows={6}
                      className={
                        validationErrors[field.id] ? "border-destructive" : ""
                      }
                    />
                  )}

                  {field.type === "Number" && (
                    <Input
                      id={field.id}
                      type="number"
                      value={formValues.fieldValues[field.id] ?? ""}
                      onChange={(e) => {
                        const num =
                          e.target.value === ""
                            ? ""
                            : parseFloat(e.target.value);
                        handleInputChange(field.id, num);
                      }}
                      placeholder={`Enter ${field.name}`}
                      className={
                        validationErrors[field.id] ? "border-destructive" : ""
                      }
                    />
                  )}

                  {![
                    "Text",
                    "ShortText",
                    "LongText",
                    "Richtext",
                    "Number",
                  ].includes(field.type) && (
                    <Input
                      id={field.id}
                      value={formValues.fieldValues[field.id] || ""}
                      onChange={(e) =>
                        handleInputChange(field.id, e.target.value)
                      }
                      placeholder={`Enter ${field.name} (${field.type})`}
                      className={
                        validationErrors[field.id] ? "border-destructive" : ""
                      }
                    />
                  )}

                  {validationErrors[field.id] && (
                    <p className="text-sm text-destructive">
                      {validationErrors[field.id]}
                    </p>
                  )}
                </div>
              ))}
            </CardContent>
          </Card>
        )}

        {updateMutation.isError && (
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertTitle>Error Updating Content Item</AlertTitle>
            <AlertDescription>
              {updateMutation.error?.message ||
                "An unexpected error occurred. Please try again."}
            </AlertDescription>
          </Alert>
        )}

        <div className="flex gap-3">
          <Button type="submit" disabled={updateMutation.isPending}>
            {updateMutation.isPending && (
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            )}
            Save Changes
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate({ to: `/content-items/${id}` })}
            disabled={updateMutation.isPending}
          >
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
