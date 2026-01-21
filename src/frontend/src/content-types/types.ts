import type { formSchema } from "./creation/schema";
import * as z from "zod";

export const FIELD_TYPES = [
  "Text",
  "Numeric",
  "Boolean",
  "Richtext",
  "Markdown",
];

export type ContentTypeCreateFormData = z.infer<typeof formSchema>;

export type PagedContentType = {
  id: string;
  name: string;
  status: string;
  version: string;
};
