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
  getTransformationRule,
  listTransformationRuleTypes,
} from "@/registry/transformation-rule-registry";
import { useStore, type AnyFieldApi } from "@tanstack/react-form";
import type { ContentTypeFormData } from "../types";

interface TransformationRulesSectionProps {
  form: any;
  fieldIndex: number;
}

export function TransformationRulesSection({
  form,
  fieldIndex,
}: TransformationRulesSectionProps) {
  return (
    <form.Field
      name={`fields[${fieldIndex}].transformationRules`}
      mode="array"
      children={(transformationsForField: AnyFieldApi) => {
        return (
          <div className="space-y-4 border-4 p-6 rounded-md">
            <Label>Transformation Rules</Label>
            {transformationsForField.state.value.length > 0 && (
              <div className="space-y-3">
                {transformationsForField.state.value.map(
                  (_: any, ruleIndex: number) => (
                    <TransformationRuleRow
                      key={ruleIndex}
                      form={form}
                      fieldIndex={fieldIndex}
                      ruleIndex={ruleIndex}
                      transformationsForField={transformationsForField}
                    />
                  ),
                )}
              </div>
            )}

            <Button
              type="button"
              onClick={() => {
                transformationsForField.pushValue({ type: "", parameters: {} });
              }}
            >
              Add transformation rule
            </Button>
          </div>
        );
      }}
    />
  );
}

function getAllowedTransformations(fieldType: string) {
  return listTransformationRuleTypes().filter((ruleType) => {
    const plugin = getTransformationRule(ruleType);
    const supported = plugin?.supportedTypes;
    return !supported || supported.includes(fieldType);
  });
}

interface TransformationRuleRowProps {
  form: any;
  fieldIndex: number;
  ruleIndex: number;
  transformationsForField: AnyFieldApi;
}

function TransformationRuleRow({
  form,
  transformationsForField,
  fieldIndex,
  ruleIndex,
}: TransformationRuleRowProps) {
  const currentFieldType = useStore(form.store, (state) => {
    const formState = state as { values: ContentTypeFormData };
    return formState?.values?.fields?.[fieldIndex]?.type;
  });

  const allowedTypes = getAllowedTransformations(currentFieldType);

  return (
    <form.Field
      name={`fields[${fieldIndex}].transformationRules[${ruleIndex}].type`}
      children={(fieldApi: AnyFieldApi) => {
        const ruleType = fieldApi.state.value;
        const plugin = getTransformationRule(ruleType);
        const RuleConfig = plugin?.ConfigComponent;

        return (
          <div className="space-y-4 border p-4 rounded-md">
            <Field data-invalid={false}>
              <FieldLabel>Rule Type</FieldLabel>
              <Select
                value={ruleType}
                onValueChange={(v) => fieldApi.handleChange(v)}
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
                value={
                  transformationsForField.state.value[ruleIndex].parameters ||
                  {}
                }
                onChange={(newParams) =>
                  transformationsForField.replaceValue(ruleIndex, {
                    ...transformationsForField.state.value[ruleIndex],
                    parameters: newParams,
                  })
                }
              />
            )}

            <Button
              type="button"
              variant="destructive"
              onClick={() => transformationsForField.removeValue(ruleIndex)}
            >
              Remove
            </Button>
          </div>
        );
      }}
    />
  );
}
