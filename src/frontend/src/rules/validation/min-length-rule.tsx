import { Label } from "@/components/ui/label";
import { registerValidationRule } from "../../registry/validation-rule-registry";
import { Input } from "@/components/ui/input";

registerValidationRule("MinimumLengthValidationRule", {
  supportedTypes: ["Text", "Richtext", "Markdown"],
  validate: (value, params) => {
    if (typeof value !== "string" || value.length < params["min-length"]) {
      return params.message || `Minimum length is ${params["min-length"]}`;
    }
    return null;
  },
  HintComponent: ({ params }) => (
    <p className="text-xs">
      Must be at least {params["min-length"]} characters
    </p>
  ),
  ConfigComponent: MinimumLengthConfig,
});
export function MinimumLengthConfig({ value, onChange }) {
  return (
    <div className="flex gap-2 items-center">
      <Label>Minimum Length</Label>
      <Input
        type="number"
        min={1}
        value={value["min-length"] ?? ""}
        onChange={(e) => {
          const n = parseInt(e.target.value, 10);
          onChange({ ...value, "min-length": isNaN(n) ? undefined : n });
        }}
        placeholder="e.g. 5"
      />
    </div>
  );
}
