import { registerTransformationRule } from "../../registry/transformation-rule-registry";

registerTransformationRule("CapitalizeTransformationRules", {
  label: "Capitalize (Title Case)",
  supportedTypes: ["Text", "Richtext", "Markdown"],
  apply: (value) => {
    if (typeof value !== "string") return value;
    return value
      .toLowerCase()
      .replace(/(?:^|\s)\S/g, (c) => c.toUpperCase());
  },
  HintComponent: () => <p className="text-xs">Capitalized (title case)</p>,
});
