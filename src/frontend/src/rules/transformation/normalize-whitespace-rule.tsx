import { registerTransformationRule } from "../../registry/transformation-rule-registry";

registerTransformationRule("NormalizeWhitespaceTransformationRules", {
  label: "Normalize Whitespace",
  supportedTypes: ["Text", "Richtext", "Markdown"],
  apply: (value) => {
    if (typeof value !== "string") return value;
    return value.replace(/\s+/g, " ").trim();
  },
  HintComponent: () => <p className="text-xs">Whitespace normalized</p>,
});
