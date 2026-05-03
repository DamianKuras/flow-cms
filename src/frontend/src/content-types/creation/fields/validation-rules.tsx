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
import type { ContentTypeCreateFormData } from "../../types";

interface ValidationRulesSectionProps {
  form: any;
  fieldIndex: number;
  fieldType: string;
}

function getAllowedValidations(fieldType: string) {
  return listValidationRuleTypes().filter((ruleType) => {
    const supported = getValidationRule(ruleType)?.supportedTypes;
    return !supported || supported.includes(fieldType);
  });
}

export function ValidationRulesSection({
  form,
  fieldIndex,
  fieldType,
}: ValidationRulesSectionProps) {
  const available = getAllowedValidations(fieldType);

  if (available.length === 0) {
    return (
      <div className="space-y-4 border-4 p-6 rounded-md">
        <Label>Validation Rules</Label>
        <p className="text-sm text-slate-500">
          No available validation rules for this type yet.
        </p>
      </div>
    );
  }

  return (
    <form.Field
      name={`fields[${fieldIndex}].validationRules`}
      mode="array"
      children={(rulesForField: AnyFieldApi) => {
        const usedTypes: string[] = rulesForField.state.value
          .map((r: any) => r.type)
          .filter((t: string) => t !== "");
        const canAddMore = available.some((t) => !usedTypes.includes(t));

        return (
          <div className="space-y-4 border-4 p-6 rounded-md">
            <Label>Validation Rules</Label>
            {rulesForField.state.value.length > 0 && (
              <div className="space-y-3">
                {rulesForField.state.value.map((_: any, ruleIndex: number) => {
                  const rowUsedTypes = rulesForField.state.value
                    .filter((_: any, i: number) => i !== ruleIndex)
                    .map((r: any) => r.type)
                    .filter((t: string) => t !== "");
                  return (
                    <ValidationRuleRow
                      key={ruleIndex}
                      form={form}
                      fieldIndex={fieldIndex}
                      ruleIndex={ruleIndex}
                      rulesForField={rulesForField}
                      usedTypes={rowUsedTypes}
                    />
                  );
                })}
              </div>
            )}
            {canAddMore && (
              <Button
                type="button"
                onClick={() =>
                  rulesForField.pushValue({ type: "", parameters: {} })
                }
              >
                Add Validation rule
              </Button>
            )}
          </div>
        );
      }}
    />
  );
}

interface ValidationRuleRowProps {
  form: any;
  fieldIndex: number;
  ruleIndex: number;
  rulesForField: any;
  usedTypes: string[];
}

function ValidationRuleRow({
  form,
  fieldIndex,
  ruleIndex,
  rulesForField,
  usedTypes,
}: ValidationRuleRowProps) {
  const currentFieldType = useStore(form.store, (state) => {
    const formState = state as { values: ContentTypeCreateFormData };
    return formState?.values?.fields?.[fieldIndex]?.type;
  });
  const allowedTypes = getAllowedValidations(currentFieldType).filter(
    (t) => !usedTypes.includes(t),
  );

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
                onValueChange={(v) => field.handleChange(v)}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select rule type" />
                </SelectTrigger>
                <SelectContent>
                  {allowedTypes.map((type) => (
                    <SelectItem key={type} value={type}>
                      {getValidationRule(type)?.label ?? type}
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
