import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/_authenticated/users/$id/edit")({
  component: RouteComponent,
});

function RouteComponent() {
  return <div>Hello "/users/$id/edit"!</div>;
}
