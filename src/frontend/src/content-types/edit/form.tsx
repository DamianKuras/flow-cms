import { useUpdateContentType } from "@/hooks/use-update-content-type";
import { useForm } from "@tanstack/react-form";
import { useNavigate } from "@tanstack/react-router";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { FieldRow } from "../creation/fields/field-row";
import { useState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import type { ContentTypeDto, FieldDto } from "@/hooks/use-content-type";
import { formSchema } from "../creation/schema";

type EditFieldValue = {
  existingId?: string;
  name: string;
  type: string;
  isRequired: boolean;
  validationRules: Array<{ type: string; parameters: Record<string, any> }>;
  transformationRules: Array<{ type: string; parameters: Record<string, any> }>;
};

function fieldDtoToEditValue(f: FieldDto): EditFieldValue {
  return {
    existingId: f.id,
    name: f.name,
    type: f.type,
    isRequired: f.isRequired,
    validationRules: (f.validationRules ?? []).map((r) => ({
      type: r.type,
      parameters: r.parameters ?? {},
    })),
    transformationRules: (f.transformationRules ?? []).map((r) => ({
      type: r.type,
      parameters: r.parameters ?? {},
    })),
  };
}

interface EditContentTypeFormProps {
  contentType: ContentTypeDto;
}

export function EditContentTypeForm({ contentType }: EditContentTypeFormProps) {
  const navigate = useNavigate();
  const updateContentType = useUpdateContentType();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const form = useForm({
    defaultValues: {
      name: contentType.name,
      fields: contentType.fields.map(fieldDtoToEditValue),
    },
    validators: {
      onSubmit: formSchema,
    },
    onSubmit: async ({ value }) => {
      setErrorMessage(null);
      updateContentType.mutate(
        {
          id: contentType.id,
          fields: value.fields.map((f) => ({
            existingId: (f as EditFieldValue).existingId,
            name: f.name,
            type: f.type,
            isRequired: f.isRequired,
            validationRules: f.validationRules,
            transformationRules: f.transformationRules,
          })),
        },
        {
          onSuccess: () => {
            navigate({
              to: "/content-types/$name",
              params: { name: contentType.name },
            });
          },
          onError: () => {
            setErrorMessage("Failed to update draft. Please try again.");
          },
        },
      );
    },
  });

  return (
    <div className="p-4 sm:p-8">
      <Card className="w-full sm:max-w-5xl mx-auto">
        <CardHeader>
          <CardTitle>Edit Draft: {contentType.name}</CardTitle>
          <CardDescription>
            Modify the fields of this draft. Publish to apply changes and create
            a migration job for existing items.
          </CardDescription>
        </CardHeader>

        <CardContent>
          {errorMessage && (
            <Alert variant="destructive" className="mb-6">
              <AlertDescription>{errorMessage}</AlertDescription>
            </Alert>
          )}

          <form
            id="edit-content-type"
            onSubmit={(e) => {
              e.preventDefault();
              form.handleSubmit();
            }}
            aria-label="Edit content type form"
          >
            <FieldGroup>
              <Field>
                <FieldLabel>Name</FieldLabel>
                <Input value={contentType.name} disabled />
                <FieldDescription>
                  The content type name cannot be changed after creation.
                </FieldDescription>
              </Field>
            </FieldGroup>

            <FieldGroup>
              <form.Field
                name="fields"
                mode="array"
                children={(fieldsArray) => {
                  const isInvalid =
                    fieldsArray.state.meta.isTouched &&
                    !fieldsArray.state.meta.isValid;
                  return (
                    <Field data-invalid={isInvalid}>
                      <FieldLabel>Fields</FieldLabel>
                      <div className="space-y-6">
                        {fieldsArray.state.value.map((_, fieldIndex) => (
                          <FieldRow
                            key={fieldIndex}
                            form={form}
                            fieldIndex={fieldIndex}
                            fieldsArray={fieldsArray}
                          />
                        ))}
                      </div>
                      {isInvalid && (
                        <FieldError errors={fieldsArray.state.meta.errors} />
                      )}
                      <Button
                        type="button"
                        onClick={() => {
                          fieldsArray.pushValue({
                            name: "",
                            type: "",
                            isRequired: false,
                            validationRules: [],
                            transformationRules: [],
                          });
                        }}
                      >
                        Add Field
                      </Button>
                    </Field>
                  );
                }}
              />
            </FieldGroup>
          </form>
        </CardContent>

        <CardFooter>
          <Field orientation="horizontal">
            <Button
              type="button"
              variant="outline"
              onClick={() =>
                navigate({
                  to: "/content-types/$name",
                  params: { name: contentType.name },
                })
              }
              disabled={updateContentType.isPending}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              form="edit-content-type"
              disabled={updateContentType.isPending}
            >
              {updateContentType.isPending ? "Saving..." : "Save Draft"}
            </Button>
          </Field>
        </CardFooter>
      </Card>
    </div>
  );
}
