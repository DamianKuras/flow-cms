import { registerTransformationRule } from "../../registry/transformation-rule-registry";

registerTransformationRule("LowercaseTransformationRule", {
  label: "Lowercase",
  supportedTypes: ["Text", "Richtext", "Markdown"],
  apply: (value) => (typeof value === "string" ? value.toLowerCase() : value),
  HintComponent: () => <p className="text-xs">Converted to lowercase</p>,
});
