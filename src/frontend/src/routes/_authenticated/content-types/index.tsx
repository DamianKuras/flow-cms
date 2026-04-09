import ContentTypesList from "@/content-types/list/ContentTypeList";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/_authenticated/content-types/")({
  component: ContentTypesList,
});
