import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/_authenticated/users/$id/')({
  component: RouteComponent,
})

function RouteComponent() {
  return <div>Hello "/users/$id/"!</div>
}
