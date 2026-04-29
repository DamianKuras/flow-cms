import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { registerTransformationRule } from "../../registry/transformation-rule-registry";

registerTransformationRule("TruncateByLength", {
  supportedTypes: ["Text", "Richtext", "Markdown"],
  apply: (value, params) => {
    if (typeof value !== "string") return value;

    const max = Number(params.truncationLength);
    if (Number.isNaN(max)) return value;

    if (max === 0) return "";
    if (value.length <= max) return value;

    return value.slice(0, max);
  },

  HintComponent: ({ params }) => (
    <p className="text-xs">
      Will be truncated to {params.truncationLength} characters
    </p>
  ),

  ConfigComponent: TruncateByLengthConfig,
});

function TruncateByLengthConfig({ value, onChange }: { value: Record<string, unknown>; onChange: (v: Record<string, unknown>) => void }) {
  return (
    <div className="flex gap-2 items-center">
      <Label>Max Length</Label>
      <Input
        type="number"
        value={(value.truncationLength as string | number) ?? ""}
        onChange={(e) => {
          const n = parseInt(e.target.value, 10);
          onChange({ ...value, truncationLength: isNaN(n) ? undefined : n });
        }}
      />
    </div>
  );
}
