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
import type { AnyFieldApi } from "@tanstack/react-form";
import { FIELD_TYPES } from "../../types";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { ValidationRulesSection } from "./validation-rules";
import { TransformationRulesSection } from "./transformation-rules";

interface FieldRowProps {
  form: any;
  fieldIndex: number;
  fieldsArray: AnyFieldApi;
}

export function FieldRow({ form, fieldIndex, fieldsArray }: FieldRowProps) {
  return (
    <div className="space-y-8 border-2 rounded-md p-4">
      <div className="space-y-2 flex justify-between">
        <form.Field
          name={`fields[${fieldIndex}].name`}
          children={(field: AnyFieldApi) => {
            const invalid =
              field.state.meta.isTouched && !field.state.meta.isValid;

            return (
              <div className="flex">
                <Field data-invalid={invalid}>
                  <FieldLabel htmlFor={`field-name-${fieldIndex}`}>
                    Field Name
                  </FieldLabel>
                  <Input
                    id={`field-name-${fieldIndex}`}
                    value={field.state.value}
                    onChange={(e) => field.handleChange(e.target.value)}
                    onBlur={field.handleBlur}
                    placeholder="e.g., title"
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
          onClick={() => fieldsArray.removeValue(fieldIndex)}
        >
          Remove Field
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
                <FieldLabel>Field Type</FieldLabel>
                <Select
                  value={field.state.value}
                  onValueChange={(v) => field.handleChange(v)}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Select type" />
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
            <Label>Value is required</Label>
          </div>
        )}
      />

      <ValidationRulesSection form={form} fieldIndex={fieldIndex} />
      <TransformationRulesSection form={form} fieldIndex={fieldIndex} />
    </div>
  );
}
