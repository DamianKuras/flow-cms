import { Button } from "@/components/ui/button";
import { Field, FieldError, FieldLabel } from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useStore, type AnyFieldApi } from "@tanstack/react-form";
import { FIELD_TYPES } from "../../types";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { ValidationRulesSection } from "./validation-rules";
import { TransformationRulesSection } from "./transformation-rules";
import { useTranslation } from "react-i18next";

interface FieldRowProps {
  form: any;
  fieldIndex: number;
  onRemove: () => void;
}

export function FieldRow({ form, fieldIndex, onRemove }: FieldRowProps) {
  const { t } = useTranslation();
  const fieldType = useStore(form.store, (state: any) => state.values?.fields?.[fieldIndex]?.type ?? "");
  return (
    <div className="space-y-8 border-2 rounded-md p-4">
      <div className="space-y-2 flex justify-between">
        {/* "fields" is the path key from form defaultValues*/}
        <form.Field
          name={`fields[${fieldIndex}].name`}
          children={(field: AnyFieldApi) => {
            const invalid =
              field.state.meta.isTouched && !field.state.meta.isValid;

            return (
              <div className="flex">
                <Field data-invalid={invalid}>
                  <FieldLabel htmlFor={`field-name-${fieldIndex}`}>
                    {t("contentType.create.field.nameLabel")}
                  </FieldLabel>
                  <Input
                    id={`field-name-${fieldIndex}`}
                    value={field.state.value}
                    onChange={(e) => field.handleChange(e.target.value)}
                    onBlur={field.handleBlur}
                    placeholder={t("contentType.create.field.namePlaceholder")}
                    autoComplete="off"
                  />
                  {invalid && <FieldError errors={field.state.meta.errors} />}
                </Field>
              </div>
            );
          }}
        />

        <Button
          variant="destructive"
          onClick={onRemove}
        >
          {t("contentType.create.field.removeField")}
        </Button>
      </div>

      <form.Field
        name={`fields[${fieldIndex}].type`}
        children={(field: AnyFieldApi) => {
          const invalid =
            field.state.meta.isTouched && !field.state.meta.isValid;

          return (
            <div>
              <Field data-invalid={invalid}>
                <FieldLabel>
                  {t("contentType.create.field.typeLabel")}
                </FieldLabel>
                <Select
                  value={field.state.value}
                  onValueChange={(v) => {
                    field.handleChange(v);
                    form.setFieldValue(`fields[${fieldIndex}].validationRules`, []);
                    form.setFieldValue(`fields[${fieldIndex}].transformationRules`, []);
                  }}
                >
                  <SelectTrigger>
                    <SelectValue
                      placeholder={t(
                        "contentType.create.field.typePlaceholder",
                      )}
                    />
                  </SelectTrigger>
                  <SelectContent>
                    {FIELD_TYPES.map((t) => (
                      <SelectItem key={t} value={t}>
                        {t}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {invalid && <FieldError errors={field.state.meta.errors} />}
              </Field>
            </div>
          );
        }}
      />

      <form.Field
        name={`fields[${fieldIndex}].isRequired`}
        children={(field: AnyFieldApi) => (
          <div className="flex gap-2 items-center">
            <Checkbox
              checked={field.state.value}
              onCheckedChange={(v) => field.handleChange(Boolean(v))}
            />
            <Label>{t("contentType.create.field.isRequiredLabel")}</Label>
          </div>
        )}
      />
      {fieldType !== "" && (
        <>
          <ValidationRulesSection form={form} fieldIndex={fieldIndex} fieldType={fieldType} />
          <TransformationRulesSection form={form} fieldIndex={fieldIndex} fieldType={fieldType} />
        </>
      )}
    </div>
  );
}
