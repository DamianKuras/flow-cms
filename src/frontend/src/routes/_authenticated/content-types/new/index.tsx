import { CreateContentTypeForm } from "@/content-types/creation/form";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/_authenticated/content-types/new/")({
  component: CreateContentTypeForm,
});
