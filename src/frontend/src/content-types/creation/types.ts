import type { formSchema } from "./schema";
import * as z from "zod";

export const FIELD_TYPES = [
  "Text",
  "Numeric",
  "Boolean",
  "Richtext",
  "Markdown",
];

export type ContentTypeFormData = z.infer<typeof formSchema>;
