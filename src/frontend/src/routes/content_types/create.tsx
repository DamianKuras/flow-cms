import { createFileRoute } from "@tanstack/react-router";
import { useForm } from "@tanstack/react-form";
import {
  Field,
  FieldDescription,
  FieldError,
  FieldGroup,
  FieldLabel,
} from "@/components/ui/field";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import * as z from "zod";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";

export const Route = createFileRoute("/content_types/create")({
  component: CreateContentType,
});

type ContentType = {
  name: string;
  fields: Array<{
    name: string;
    type: string;
    isRequired: boolean;
    validationRules: Array<{
      type: string;
      parameters: Record<string, string>;
    }>;
  }>;
};

const FIELD_TYPES = ["Text", "Numeric", "Boolean", "Richtext", "Markdown"];
const VALIDATION_RULE_TYPES: Record<string, { params: string[] }> = {
  IsLowercase: { params: [] },
  MaxLength: { params: ["value"] },
  MinLength: { params: ["value"] },
  Regex: { params: ["regex"] },
  Range: { params: ["min", "max"] },
};

const formSchema = z.object({
  name: z
    .string()
    .min(5, "Name must be at least 5 characters.")
    .max(32, "Name must be at most 32 characters."),

  fields: z.array(
    z.object({
      name: z
        .string()
        .min(1, "Field name is required")
        .max(50, "Field name must be at most 50 characters"),
      type: z.string().min(1, "Field type is required"),
      isRequired: z.boolean(),
      validationRules: z.array(
        z.object({
          type: z.string(),
          parameters: z.record(z.string()),
        })
      ),
    })
  ),
});

function CreateContentType() {
  const form = useForm({
    defaultValues: {
      name: "",
      fields: [
        {
          name: "",
          type: "",
          isRequired: false as boolean,
          validationRules: [] as Array<{
            type: string;
            parameters: Record<string, string>;
          }>,
        },
      ],
    } satisfies ContentType,
    validators: {
      onSubmit: formSchema,
    },
    onSubmit: async ({ value }) => {
      console.log("Submitting:", JSON.stringify(value, null, 2));
    },
  });

  return (
    <div className="p-4 sm:p-8">
      <Card className="w-full  sm:max-w-5xl mx-auto">
        <CardHeader>
          <CardTitle>Create Content Type</CardTitle>
          <CardDescription>
            Define the structure for your content type with custom fields,
            validation rules and transformation rules.
          </CardDescription>
        </CardHeader>

        <CardContent>
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
                      <FieldDescription>
                        5-32 characters, lowercase and underscores recommended
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
              variant="outline"
              onClick={() => form.reset()}
            >
              Reset
            </Button>
            <Button type="submit" form="create-content-type">
              Submit
            </Button>
          </Field>
        </CardFooter>
      </Card>
    </div>
  );
}

function FieldRow({ form, fieldIndex, fieldsArray }) {
  return (
    <div className="space-y-8 border-2 rounded-md p-4">
      <div className="space-y-2 flex justify-between">
        <form.Field
          name={`fields[${fieldIndex}].name`}
          children={(field) => {
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
          onClick={() => {
            fieldsArray.removeValue(fieldIndex);
          }}
        >
          Remove Field
        </Button>
      </div>

      <form.Field
        name={`fields[${fieldIndex}].type`}
        children={(field) => {
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
        children={(field) => (
          <div className="flex gap-2 items-center">
            <Checkbox
              checked={field.state.value}
              onCheckedChange={(v) => field.handleChange(Boolean(v))}
            />
            <Label>Required</Label>
          </div>
        )}
      />

      <ValidationRulesSection form={form} fieldIndex={fieldIndex} />
    </div>
  );
}

function ValidationRulesSection({ form, fieldIndex }) {
  return (
    <form.Field
      name={`fields[${fieldIndex}].validationRules`}
      mode="array"
      children={(rulesForField) => {
        return (
          <div className="space-y-4 border-2 p-6 rounded-md">
            <Label>Validation Rules</Label>
            {rulesForField.state.value.length === 0 ? (
              <p className="text-sm text-slate-500">
                No validation rules added yet
              </p>
            ) : (
              <div className="space-y-3">
                {rulesForField.state.value.map((_, ruleIndex) => (
                  <div key={ruleIndex}>
                    <ValidationRuleRow
                      form={form}
                      fieldIndex={fieldIndex}
                      ruleIndex={ruleIndex}
                      rulesForField={rulesForField}
                    />
                  </div>
                ))}
              </div>
            )}

            <Button
              type="button"
              onClick={() => {
                rulesForField.pushValue({
                  type: "",
                  parameters: {},
                });
              }}
            >
              Add Validation rule
            </Button>
          </div>
        );
      }}
    />
  );
}

function ValidationRuleRow({ fieldIndex, ruleIndex }) {
  return (
    <>
      {/* Rule type */}
      <form.Field
        name={`fields[${fieldIndex}].validationRules[${ruleIndex}].type`}
        children={(field) => (
          <Select
            value={field.state.value}
            onValueChange={(v) => field.handleChange(v)}
          >
            <SelectTrigger>
              <SelectValue placeholder="Rule type" />
            </SelectTrigger>
            <SelectContent>
              {Object.keys(VALIDATION_RULE_TYPES).map((rule) => (
                <SelectItem key={rule} value={rule}>
                  {rule}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
      />

      {/* Parameters */}
      {VALIDATION_RULE_TYPES[field.state.value]?.params?.map((param) => (
        <form.Field
          key={param}
          name={`fields[${fieldIndex}].validationRules[${ruleIndex}].parameters.${param}`}
          children={(paramField) => (
            <Input
              value={paramField.state.value || ""}
              onChange={(e) => paramField.handleChange(e.target.value)}
              onBlur={paramField.handleBlur}
            />
          )}
        />
      ))}
    </>
  );
}
