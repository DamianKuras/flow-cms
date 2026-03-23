export interface ValidationRuleModule {
  /**
   * Which content field types this rule supports
   * e.g. ["Text","Richtext","Markdown"]
   */
  supportedTypes?: string[];

  validate: (
    value: any,
    params: Record<string, any>,
    allValues: Record<string, any>,
  ) => string | null;

  HintComponent?: React.FC<{ params: Record<string, any> }>;

  ConfigComponent?: React.FC<{
    value: Record<string, any>;
    onChange: (changes: Record<string, any>) => void;
  }>;
}
