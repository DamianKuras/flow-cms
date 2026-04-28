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
import { useContentTypeSummaryByName } from "@/hooks/use-content-type-summaries";
import { useApiClient } from "@/hooks/use-api-client";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { AlertCircle, ArrowLeft, Loader2 } from "lucide-react";
import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { getValidationRule } from "@/registry/validation-rule-registry";

export const Route = createFileRoute(
  "/_authenticated/content-types/$name/items/new",
)({
  component: RouteComponent,
});

function getApiErrorMessage(error: any): string {
  const data = error?.response?.data;
  if (!data) return error?.message ?? "An unexpected error occurred.";
  if (data.errors) {
    return Object.entries(data.errors as Record<string, string[]>)
      .map(([field, msgs]) => `${field}: ${msgs.join(", ")}`)
      .join(" | ");
  }
  if (data.detail) return data.detail;
  if (data.title) return data.title;
  return error?.message ?? "An unexpected error occurred.";
}

type FormValues = {
  title: string;
  fieldValues: Record<string, any>;
};

function RouteComponent() {
  const { name } = Route.useParams();
  const navigate = useNavigate();
  const api = useApiClient();
  const queryClient = useQueryClient();

  const { summary, isLoading: summaryLoading } = useContentTypeSummaryByName(name);
  const publishedId = summary?.publishedId ?? "";
  const { data, error, isLoading: typeLoading } = useContentType(publishedId);

  const [formValues, setFormValues] = useState<FormValues>({
    title: "",
    fieldValues: {},
  });
  const [validationErrors, setValidationErrors] = useState<Record<string, string[]>>({});

  const createMutation = useMutation({
    mutationFn: async (values: FormValues) => {
      const response = await api.post(`/content-items`, {
        title: values.title,
        contentTypeId: publishedId,
        values: values.fieldValues,
      });
      return response.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["content", publishedId] });
      navigate({ to: "/content-types/$name/items", params: { name } });
    },
    onError: () => {},
  });

  const handleInputChange = (fieldId: string, value: any) => {
    setFormValues((prev) => ({
      ...prev,
      fieldValues: { ...prev.fieldValues, [fieldId]: value },
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

  const validateField = (field: FieldDto, value: string | null) => {
    const errors: string[] = [];
    if (field.isRequired && (value === "" || value == null)) {
      errors.push(`${field.name} is required`);
    }
    for (const rule of field.validationRules ?? []) {
      const plugin = getValidationRule(rule.type);
      if (plugin) {
        const err = plugin.validate(value, rule.parameters || {}, formValues.fieldValues);
        if (err) errors.push(err);
      }
    }
    return errors;
  };

  const validateForm = (fields: FieldDto[]): boolean => {
    const errors: Record<string, string[]> = {};
    if (!formValues.title?.trim()) {
      errors.title = ["Title is required"];
    }
    fields.forEach((field) => {
      const fieldErrors = validateField(field, formValues.fieldValues[field.id]);
      if (fieldErrors.length > 0) errors[field.id] = fieldErrors;
    });
    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const fields = data?.contentType?.fields;
    if (!fields || !validateForm(fields)) return;
    createMutation.mutate(formValues);
  };

  const isLoading = summaryLoading || (!!publishedId && typeLoading);

  if (isLoading) {
    return (
      <div className="flex-1 p-6 flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!summary?.publishedId) {
    return (
      <div className="flex-1 p-6">
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>No Published Version</AlertTitle>
          <AlertDescription>
            Publish the schema first before creating items.
          </AlertDescription>
        </Alert>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex-1 p-6">
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error Loading Content Type</AlertTitle>
          <AlertDescription>{error.message}</AlertDescription>
        </Alert>
      </div>
    );
  }

  if (!data?.contentType) {
    return null;
  }

  const contentType = data.contentType;

  return (
    <div className="flex-1 space-y-6 p-6 max-w-4xl mx-auto">
      <div className="space-y-2">
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate({ to: "/content-types/$name/items", params: { name } })}
        >
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back to {name}
        </Button>
        <h1 className="text-3xl font-bold">Create New Content Item</h1>
        <p className="text-muted-foreground">
          Creating content of type: <span className="font-semibold">{name}</span>
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <Card>
          <CardHeader>
            <CardTitle>Basic Information</CardTitle>
            <CardDescription>Enter the basic details for this content item</CardDescription>
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
                <p className="text-sm text-destructive">{validationErrors.title}</p>
              )}
            </div>
          </CardContent>
        </Card>

        {contentType.fields.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle>Content Fields</CardTitle>
              <CardDescription>Enter values for the content fields</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {contentType.fields.map((field) => (
                <div key={field.id} className="space-y-2">
                  <Label htmlFor={field.id}>
                    {field.name}
                    {field.isRequired && <span className="text-destructive ml-1">*</span>}
                  </Label>

                  {(field.type === "Text" || field.type === "ShortText") && (
                    <Input
                      id={field.id}
                      value={formValues.fieldValues[field.id] || ""}
                      onChange={(e) => handleInputChange(field.id, e.target.value)}
                      placeholder={`Enter ${field.name}`}
                      className={validationErrors[field.id] ? "border-destructive" : ""}
                    />
                  )}

                  {(field.type === "LongText" || field.type === "RichText") && (
                    <Textarea
                      id={field.id}
                      value={formValues.fieldValues[field.id] || ""}
                      onChange={(e) => handleInputChange(field.id, e.target.value)}
                      placeholder={`Enter ${field.name}`}
                      rows={6}
                      className={validationErrors[field.id] ? "border-destructive" : ""}
                    />
                  )}

                  {field.type === "Numeric" && (
                    <Input
                      id={field.id}
                      type="number"
                      value={formValues.fieldValues[field.id] ?? ""}
                      onChange={(e) => {
                        const num = e.target.value === "" ? "" : parseFloat(e.target.value);
                        handleInputChange(field.id, num);
                      }}
                      placeholder={`Enter ${field.name}`}
                      className={validationErrors[field.id] ? "border-destructive" : ""}
                    />
                  )}

                  {!["Text", "ShortText", "LongText", "RichText", "Numeric"].includes(field.type) && (
                    <Input
                      id={field.id}
                      value={formValues.fieldValues[field.id] || ""}
                      onChange={(e) => handleInputChange(field.id, e.target.value)}
                      placeholder={`Enter ${field.name} (${field.type})`}
                      className={validationErrors[field.id] ? "border-destructive" : ""}
                    />
                  )}

                  {validationErrors[field.id]?.length > 0 && (
                    <div className="space-y-1 text-sm text-destructive">
                      {validationErrors[field.id].map((msg, i) => (
                        <p key={i}>{msg}</p>
                      ))}
                    </div>
                  )}

                  {field.validationRules?.map((rule) => {
                    const plugin = getValidationRule(rule.type);
                    if (!plugin?.HintComponent) return null;
                    const Hint = plugin.HintComponent;
                    return <Hint key={rule.type} params={rule.parameters || {}} />;
                  })}
                </div>
              ))}
            </CardContent>
          </Card>
        )}

        {createMutation.isError && (
          <Alert variant="destructive">
            <AlertCircle className="h-4 w-4" />
            <AlertTitle>Error Creating Content Item</AlertTitle>
            <AlertDescription>{getApiErrorMessage(createMutation.error)}</AlertDescription>
          </Alert>
        )}

        <div className="flex gap-3">
          <Button type="submit" disabled={createMutation.isPending}>
            {createMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Create Content Item
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => navigate({ to: "/content-types/$name/items", params: { name } })}
            disabled={createMutation.isPending}
          >
            Cancel
          </Button>
        </div>
      </form>
    </div>
  );
}
