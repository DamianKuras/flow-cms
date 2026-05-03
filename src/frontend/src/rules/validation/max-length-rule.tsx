import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { registerValidationRule } from "../../registry/validation-rule-registry";

registerValidationRule("MaximumLengthValidationRule", {
  label: "Maximum Length",
  supportedTypes: ["Text", "Richtext", "Markdown"],
  validate: (value, params) => {
    if (typeof value !== "string" || value.length > params["max-length"]) {
      return `Maximum length is ${params["max-length"]}`;
    }
    return null;
  },
  HintComponent: ({ params }) => (
    <p className="text-xs">Must be at most {params["max-length"]} characters</p>
  ),
  ConfigComponent: MaximumLengthConfig,
});

export function MaximumLengthConfig({
  value,
  onChange,
}: {
  value: Record<string, unknown>;
  onChange: (v: Record<string, unknown>) => void;
}) {
  return (
    <div className="flex gap-2 items-center">
      <Label>Maximum Length</Label>
      <Input
        type="number"
        min={1}
        value={(value["max-length"] as string | number) ?? ""}
        onChange={(e) => {
          const n = parseInt(e.target.value, 10);
          onChange({ ...value, "max-length": isNaN(n) ? undefined : n });
        }}
        placeholder="e.g. 100"
      />
    </div>
  );
}
