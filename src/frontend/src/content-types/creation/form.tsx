import { useCreateContentType } from "@/hooks/use-create-content-type";
import { useForm } from "@tanstack/react-form";
import { useNavigate } from "@tanstack/react-router";
import type { ContentTypeCreateFormData } from "../types";
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
import { useTranslation } from "react-i18next";
import { Alert, AlertDescription } from "@/components/ui/alert";

export function CreateContentTypeForm() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const createContentType = useCreateContentType();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const form = useForm({
    defaultValues: {
      name: "",
      fields: [],
    } as ContentTypeCreateFormData,
    validators: {
      onSubmit: formSchema,
    },
    onSubmit: async ({ value }) => {
      setErrorMessage(null);
      createContentType.mutate(value, {
        onSuccess: () => {
          form.reset();
          navigate({ to: "/content-types/$name", params: { name: value.name } });
        },
        onError: (_) => {
          setErrorMessage(t("contentType.create.errorRetry"));
        },
      });
    },
  });

  return (
    <div className="p-4 sm:p-8">
      <Card className="w-full sm:max-w-5xl mx-auto">
        <CardHeader>
          <CardTitle>{t("contentType.create.title")}</CardTitle>
          <CardDescription>
            {t("contentType.create.description")}
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
                        {t("contentType.create.nameLabel")}
                      </FieldLabel>
                      <Input
                        id={field.name}
                        name={field.name}
                        value={field.state.value}
                        onBlur={field.handleBlur}
                        onChange={(e) => field.handleChange(e.target.value)}
                        aria-invalid={isInvalid}
                        placeholder={t("contentType.create.namePlaceholder")}
                        autoComplete="off"
                      />
                      {isInvalid && (
                        <FieldError errors={field.state.meta.errors} />
                      )}
                      <FieldDescription>
                        {t("contentType.create.nameHint")}
                      </FieldDescription>
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
                        {t("contentType.create.fieldsLabel")}
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
                        {t("contentType.create.addField")}
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
              {t("contentType.create.reset")}
            </Button>
            <Button
              type="submit"
              variant="default"
              form="create-content-type"
              disabled={createContentType.isPending}
            >
              {createContentType.isPending
                ? t("contentType.create.submitting")
                : t("contentType.create.submit")}
            </Button>
          </Field>
        </CardFooter>
      </Card>
    </div>
  );
}
