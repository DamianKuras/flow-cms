import { useCreateContentType } from "@/hooks/use-create-content-type";
import { useForm } from "@tanstack/react-form";
import { useNavigate } from "@tanstack/react-router";
import type { ContentTypeFormData } from "./types";
import { formSchema } from "./schema";
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
import { FieldRow } from "./fields/field-row";
import { useState } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";

export function CreateContentTypeForm() {
  const navigate = useNavigate();
  const createContentType = useCreateContentType();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const form = useForm({
    defaultValues: {
      name: "",
      fields: [],
    } as ContentTypeFormData,
    validators: {
      onSubmit: formSchema,
    },
    onSubmit: async ({ value }) => {
      createContentType.mutate(value, {
        onSuccess: (data) => {
          form.reset();
          navigate({ to: `/content-types/${data.id}` });
        },
        onError: (_) => {
          setErrorMessage(
            "Failed to create content type. Please try again latter.",
          );
        },
      });
    },
  });

  return (
    <div className="p-4 sm:p-8">
      <Card className="w-full sm:max-w-5xl mx-auto">
        <CardHeader>
          <CardTitle>Create Content Type</CardTitle>
          <CardDescription>
            Define the structure for your content type with fields, validation
            rules and transformation rules.
          </CardDescription>
        </CardHeader>

        <CardContent>
          {errorMessage && (
            <Alert variant="destructive" className="mb-6">
              <AlertDescription>{errorMessage}</AlertDescription>
            </Alert>
          )}

          <form
            id="create-content-type"
            onSubmit={(e) => {
              e.preventDefault();
              form.handleSubmit();
            }}
            aria-label="Create content type form"
          >
            <FieldGroup>
              <form.Field
                name="name"
                children={(field) => {
                  const isInvalid =
                    field.state.meta.isTouched && !field.state.meta.isValid;
                  return (
                    <Field data-invalid={isInvalid}>
                      <FieldLabel htmlFor={field.name}>
                        Content Type Name
                      </FieldLabel>
                      <Input
                        id={field.name}
                        name={field.name}
                        value={field.state.value}
                        onBlur={field.handleBlur}
                        onChange={(e) => field.handleChange(e.target.value)}
                        aria-invalid={isInvalid}
                        placeholder="e.g., Blog Posts, Products"
                        autoComplete="off"
                      />
                      {isInvalid && (
                        <FieldError errors={field.state.meta.errors} />
                      )}
                      <FieldDescription>minimum 5 characters</FieldDescription>
                    </Field>
                  );
                }}
              />
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
                      <FieldLabel htmlFor={fieldsArray.name}>
                        Fields:
                      </FieldLabel>
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
                        Add field
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
              variant="destructive"
              onClick={() => form.reset()}
              disabled={createContentType.isPending}
            >
              Reset
            </Button>
            <Button
              type="submit"
              variant="default"
              form="create-content-type"
              disabled={createContentType.isPending}
            >
              {createContentType.isPending ? "Creating..." : "Submit"}
            </Button>
          </Field>
        </CardFooter>
      </Card>
    </div>
  );
}
