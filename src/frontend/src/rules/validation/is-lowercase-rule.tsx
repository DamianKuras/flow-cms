import { registerValidationRule } from "../../registry/validation-rule-registry";

registerValidationRule("IsLowercaseRule", {
  label: "Lowercase Only",
  supportedTypes: ["Text"],
  validate: (value) => {
    if (typeof value !== "string") return "Value must be a string";
    if (value.split("").some((c) => c !== c.toLowerCase())) {
      return "Value must be lowercase";
    }
    return null;
  },
  HintComponent: () => <p className="text-xs">Must be lowercase</p>,
});
