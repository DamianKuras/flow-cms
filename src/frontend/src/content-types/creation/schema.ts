import * as z from "zod";

const fieldSchema = z.object({
  name: z
    .string()
    .min(1, "Field name is required")
    .max(50, "Field name must be at most 50 characters"),
  type: z.string().min(1, "Field type is required"),
  isRequired: z.boolean(),
  validationRules: z.array(
    z.object({
      type: z.string(),
      parameters: z.record(z.union([z.string(), z.number(), z.boolean()])),
    }),
  ),
  transformationRules: z.array(
    z.object({
      type: z.string(),
      parameters: z.record(z.union([z.string(), z.number(), z.boolean()])),
    }),
  ),
});

export const formSchema = z.object({
  name: z
    .string()
    .min(5, "Name must be at least 5 characters.")
    .max(32, "Name must be at most 32 characters."),
  fields: z.array(fieldSchema),
});
