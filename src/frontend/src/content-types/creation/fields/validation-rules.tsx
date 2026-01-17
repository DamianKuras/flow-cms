import { Button } from "@/components/ui/button";
import { Field, FieldLabel } from "@/components/ui/field";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  getValidationRule,
  listValidationRuleTypes,
} from "@/registry/validation-rule-registry";
import { useStore, type AnyFieldApi } from "@tanstack/react-form";
import type { ContentTypeFormData } from "../types";

interface ValidationRulesSectionProps {
  form: any;
  fieldIndex: number;
}

export function ValidationRulesSection({
  form,
  fieldIndex,
}: ValidationRulesSectionProps) {
  return (
    <form.Field
      name={`fields[${fieldIndex}].validationRules`}
      mode="array"
      children={(rulesForField: AnyFieldApi) => {
        return (
          <div className="space-y-4 border-4 p-6 rounded-md">
            <Label>Validation Rules</Label>
            {rulesForField.state.value.length === 0 ? (
              <p className="text-sm text-slate-500">
                No validation rules added yet
              </p>
            ) : (
              <div className="space-y-3">
                {rulesForField.state.value.map((_: any, ruleIndex: number) => (
                  <ValidationRuleRow
                    key={ruleIndex}
                    form={form}
                    fieldIndex={fieldIndex}
                    ruleIndex={ruleIndex}
                    rulesForField={rulesForField}
                  />
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

function getAllowedValidations(fieldType: string) {
  return listValidationRuleTypes().filter((ruleType) => {
    const plugin = getValidationRule(ruleType);
    const supported = plugin?.supportedTypes;
    return !supported || supported.includes(fieldType);
  });
}

interface ValidationRuleRowProps {
  form: any;
  fieldIndex: number;
  ruleIndex: number;
  rulesForField: any;
}

function ValidationRuleRow({
  form,
  fieldIndex,
  ruleIndex,
  rulesForField,
}: ValidationRuleRowProps) {
  const currentFieldType = useStore(form.store, (state) => {
    const formState = state as { values: ContentTypeFormData };
    return formState?.values?.fields?.[fieldIndex]?.type;
  });
  const allowedTypes = getAllowedValidations(currentFieldType);
  return (
    <form.Field
      name={`fields[${fieldIndex}].validationRules[${ruleIndex}].type`}
      children={(field: AnyFieldApi) => {
        const ruleType = field.state.value;
        const plugin = getValidationRule(ruleType);
        const RuleConfig = plugin?.ConfigComponent;

        return (
          <div className="space-y-4 border p-4 rounded-md">
            <Field data-invalid={false}>
              <FieldLabel>Rule Type</FieldLabel>
              <Select
                value={ruleType}
                onValueChange={(v) => {
                  field.handleChange(v);
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select rule type" />
                </SelectTrigger>
                <SelectContent>
                  {allowedTypes.map((type) => (
                    <SelectItem key={type} value={type}>
                      {type}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </Field>

            {RuleConfig && (
              <RuleConfig
                value={rulesForField.state.value[ruleIndex].parameters || {}}
                onChange={(newParams) =>
                  rulesForField.replaceValue(ruleIndex, {
                    ...rulesForField.state.value[ruleIndex],
                    parameters: newParams,
                  })
                }
              />
            )}

            <Button
              type="button"
              variant="destructive"
              onClick={() => rulesForField.removeValue(ruleIndex)}
            >
              Remove
            </Button>
          </div>
        );
      }}
    />
  );
}
