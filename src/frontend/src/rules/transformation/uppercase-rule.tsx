import { registerTransformationRule } from "../../registry/transformation-rule-registry";

registerTransformationRule("UppercaseTransformationRule", {
  label: "Uppercase",
  supportedTypes: ["Text", "Richtext", "Markdown"],
  apply: (value) => (typeof value === "string" ? value.toUpperCase() : value),
  HintComponent: () => <p className="text-xs">Converted to uppercase</p>,
});
