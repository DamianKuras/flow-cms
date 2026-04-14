import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/_authenticated/content-items/")({
  component: RouteComponent,
});

function RouteComponent() {
  return <div>Hello "/content/"!</div>;
}
