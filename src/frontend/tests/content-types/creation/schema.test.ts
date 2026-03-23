import { describe, it, expect } from "vitest";
import { formSchema } from "@/content-types/creation/schema";

const validForm = {
  name: "Blog Posts",
  fields: [],
};

describe("formSchema", () => {
  it("accepts a form with no fields", () => {
    expect(formSchema.safeParse(validForm).success).toBe(true);
  });

  it("rejects a name shorter than 5 characters", () => {
    const result = formSchema.safeParse({ ...validForm, name: "ab" });
    expect(result.success).toBe(false);
  });

  it("rejects a name longer than 32 characters", () => {
    const result = formSchema.safeParse({
      ...validForm,
      name: "a".repeat(33),
    });
    expect(result.success).toBe(false);
  });

  it("accepts a field with string parameters", () => {
    const result = formSchema.safeParse({
      name: "Blog Posts",
      fields: [
        {
          name: "title",
          type: "Text",
          isRequired: true,
          validationRules: [{ type: "SomeRule", parameters: { key: "value" } }],
          transformationRules: [],
        },
      ],
    });
    expect(result.success).toBe(true);
  });

  // Regression: z.record(z.string()) was previously used, which rejected numeric
  // parameter values like { "min-length": 5 } and silently blocked form submission.
  it("accepts numeric values in validation rule parameters", () => {
    const result = formSchema.safeParse({
      name: "Blog Posts",
      fields: [
        {
          name: "title",
          type: "Text",
          isRequired: true,
          validationRules: [
            {
              type: "MinimumLengthValidationRule",
              parameters: { "min-length": 5 },
            },
          ],
          transformationRules: [],
        },
      ],
    });
    expect(result.success).toBe(true);
  });

  it("accepts numeric values in transformation rule parameters", () => {
    const result = formSchema.safeParse({
      name: "Blog Posts",
      fields: [
        {
          name: "title",
          type: "Text",
          isRequired: false,
          validationRules: [],
          transformationRules: [
            {
              type: "SomeTransformation",
              parameters: { maxLength: 100, enabled: true },
            },
          ],
        },
      ],
    });
    expect(result.success).toBe(true);
  });

  it("accepts boolean values in rule parameters", () => {
    const result = formSchema.safeParse({
      name: "Blog Posts",
      fields: [
        {
          name: "body",
          type: "Text",
          isRequired: false,
          validationRules: [
            { type: "SomeRule", parameters: { strict: true } },
          ],
          transformationRules: [],
        },
      ],
    });
    expect(result.success).toBe(true);
  });
});
