export interface TransformationRuleModule {
  label?: string;

  /**
   * Which content field types this rule supports
   * e.g. ["Text","Richtext","Markdown"]
   */
  supportedTypes?: string[];

  /**
   * Applies the transformation on the frontend
   * (used for preview / client-side transformation)
   */
  apply: (value: any, params: Record<string, any>) => any;

  /**
   * Optional UI hint shown to users
   */
  HintComponent?: React.FC<{ params: Record<string, any> }>;

  /**
   * UI used to configure rule parameters
   */
  ConfigComponent?: React.FC<{
    value: Record<string, any>;
    onChange: (value: Record<string, any>) => void;
  }>;
}
