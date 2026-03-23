import type { ValidationRuleModule } from "@/lib/validation-rule";

const ruleRegistry: Record<string, ValidationRuleModule> = {};

export const registerValidationRule = (
  type: string,
  module: ValidationRuleModule,
) => {
  ruleRegistry[type] = module;
};

export const getValidationRule = (type: string) => ruleRegistry[type];

export const listValidationRuleTypes = () => Object.keys(ruleRegistry);
