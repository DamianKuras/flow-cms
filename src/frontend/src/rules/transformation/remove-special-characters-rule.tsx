import { registerTransformationRule } from "../../registry/transformation-rule-registry";

registerTransformationRule("RemoveSpecialCharactersTransformationRules", {
  label: "Remove Special Characters",
  supportedTypes: ["Text", "Richtext", "Markdown"],
  apply: (value) => {
    if (typeof value !== "string") return value;
    return value.replace(/[^a-zA-Z0-9\s]/g, "");
  },
  HintComponent: () => <p className="text-xs">Special characters removed</p>,
});
