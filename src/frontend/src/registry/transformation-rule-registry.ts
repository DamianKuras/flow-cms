import type { TransformationRuleModule } from "@/lib/transformation-rule";

const ruleRegistry: Record<string, TransformationRuleModule> = {};

export const registerTransformationRule = (
  type: string,
  module: TransformationRuleModule,
) => {
  ruleRegistry[type] = module;
};

export const getTransformationRule = (type: string) => ruleRegistry[type];

export const listTransformationRuleTypes = () => Object.keys(ruleRegistry);
