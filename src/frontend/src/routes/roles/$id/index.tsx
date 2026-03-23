import * as React from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { ArrowLeft, ShieldCheck, Trash2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

import {
  useRole,
  useAddPermission,
  useRemovePermission,
  type CmsAction,
  type ResourceType,
  type PermissionScope,
  type PermissionDto,
} from "@/hooks/use-roles";
import { ResourcePicker } from "@/components/ResourcePicker";

export const Route = createFileRoute("/roles/$id/")({
  component: RoleDetailPage,
});

const CMS_ACTIONS: CmsAction[] = ["read", "list", "create", "update", "delete"];
const RESOURCE_TYPES: ResourceType[] = ["contentType", "contentItem", "field"];
const SCOPES: PermissionScope[] = ["allow", "deny"];

const ACTION_LABELS: Record<CmsAction, string> = {
  read: "Read",
  list: "List",
  create: "Create",
  update: "Update",
  delete: "Delete",
};

const RESOURCE_TYPE_LABELS: Record<ResourceType, string> = {
  contentType: "Content Type",
  contentItem: "Content Item",
  field: "Field",
};

function ScopeBadge({ scope }: { scope: PermissionScope }) {
  return (
    <Badge
      variant={scope === "allow" ? "default" : "destructive"}
      className="text-xs font-medium"
    >
      {scope === "allow" ? "Allow" : "Deny"}
    </Badge>
  );
}

function ActionBadge({ action }: { action: CmsAction }) {
  return (
    <Badge variant="secondary" className="font-mono text-xs">
      {ACTION_LABELS[action]}
    </Badge>
  );
}

function AddPermissionForm({ roleId }: { roleId: string }) {
  const addPermission = useAddPermission();
  const [action, setAction] = React.useState<CmsAction>("read");
  const [resourceType, setResourceType] = React.useState<ResourceType>("contentType");
  const [resourceId, setResourceId] = React.useState<string | null>(null);
  const [scope, setScope] = React.useState<PermissionScope>("allow");
  const [typeLevelOnly, setTypeLevelOnly] = React.useState(false);
  const [error, setError] = React.useState("");

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    addPermission.mutate(
      {
        roleId,
        action,
        resourceType,
        resourceId: typeLevelOnly ? null : resourceId,
        scope,
      },
      {
        onSuccess: () => setResourceId(null),
        onError: () => setError("Failed to add permission."),
      },
    );
  }

  return (
    <Card>
      <CardHeader className="pb-4">
        <CardTitle className="text-base">Add Permission</CardTitle>
        <CardDescription>
          Grant or deny an action on a specific resource or the entire resource type.
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="grid grid-cols-3 gap-4">
            <div className="space-y-1.5">
              <Label className="text-xs text-muted-foreground uppercase tracking-wide">
                Action
              </Label>
              <Select value={action} onValueChange={(v) => setAction(v as CmsAction)}>
                <SelectTrigger className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {CMS_ACTIONS.map((a) => (
                    <SelectItem key={a} value={a}>
                      {ACTION_LABELS[a]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label className="text-xs text-muted-foreground uppercase tracking-wide">
                Resource Type
              </Label>
              <Select
                value={resourceType}
                onValueChange={(v) => {
                  setResourceType(v as ResourceType);
                  setResourceId(null);
                }}
              >
                <SelectTrigger className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {RESOURCE_TYPES.map((r) => (
                    <SelectItem key={r} value={r}>
                      {RESOURCE_TYPE_LABELS[r]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label className="text-xs text-muted-foreground uppercase tracking-wide">
                Scope
              </Label>
              <Select value={scope} onValueChange={(v) => setScope(v as PermissionScope)}>
                <SelectTrigger className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {SCOPES.map((s) => (
                    <SelectItem key={s} value={s}>
                      {s === "allow" ? "Allow" : "Deny"}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <Separator />

          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <Label className="text-xs text-muted-foreground uppercase tracking-wide">
                Resource
              </Label>
              <label className="flex items-center gap-2 text-sm text-muted-foreground cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={typeLevelOnly}
                  onChange={(e) => {
                    setTypeLevelOnly(e.target.checked);
                    setResourceId(null);
                  }}
                  className="rounded"
                />
                Apply to all (type-level)
              </label>
            </div>
            {typeLevelOnly ? (
              <p className="text-sm text-muted-foreground italic">
                This permission applies to every {RESOURCE_TYPE_LABELS[resourceType].toLowerCase()}.
              </p>
            ) : (
              <ResourcePicker
                resourceType={resourceType}
                value={resourceId}
                onChange={setResourceId}
              />
            )}
          </div>

          {error && <p className="text-sm text-destructive">{error}</p>}

          <div className="flex justify-end">
            <Button type="submit" size="sm" disabled={addPermission.isPending}>
              {addPermission.isPending ? "Adding…" : "Add Permission"}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}

function PermissionsTable({
  roleId,
  permissions,
}: {
  roleId: string;
  permissions: PermissionDto[];
}) {
  const removePermission = useRemovePermission();

  return (
    <div className="rounded-lg border">
      <Table>
        <TableHeader>
          <TableRow className="hover:bg-transparent">
            <TableHead className="text-xs uppercase tracking-wide w-28">Action</TableHead>
            <TableHead className="text-xs uppercase tracking-wide w-36">Resource Type</TableHead>
            <TableHead className="text-xs uppercase tracking-wide">Resource</TableHead>
            <TableHead className="text-xs uppercase tracking-wide w-24">Scope</TableHead>
            <TableHead className="w-16" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {permissions.length ? (
            permissions.map((perm, i) => (
              <TableRow key={i}>
                <TableCell>
                  <ActionBadge action={perm.action} />
                </TableCell>
                <TableCell className="text-sm text-muted-foreground">
                  {RESOURCE_TYPE_LABELS[perm.resourceType]}
                </TableCell>
                <TableCell>
                  {perm.resourceId ? (
                    <div className="space-y-0.5">
                      <span className="text-sm font-medium">
                        {perm.resourceName ?? perm.resourceId.slice(0, 8) + "…"}
                      </span>
                      <div className="font-mono text-xs text-muted-foreground">
                        {perm.resourceId}
                      </div>
                    </div>
                  ) : (
                    <span className="text-xs text-muted-foreground italic">all</span>
                  )}
                </TableCell>
                <TableCell>
                  <ScopeBadge scope={perm.scope} />
                </TableCell>
                <TableCell className="text-right">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-7 w-7 text-muted-foreground hover:text-destructive"
                    onClick={() =>
                      removePermission.mutate({
                        roleId,
                        action: perm.action,
                        resourceType: perm.resourceType,
                        resourceId: perm.resourceId,
                        scope: perm.scope,
                      })
                    }
                    disabled={removePermission.isPending}
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                </TableCell>
              </TableRow>
            ))
          ) : (
            <TableRow>
              <TableCell colSpan={5} className="h-24 text-center">
                <div className="flex flex-col items-center gap-2 text-muted-foreground">
                  <ShieldCheck className="h-7 w-7 opacity-30" />
                  <p className="text-sm">No permissions assigned yet.</p>
                </div>
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );
}

function RoleDetailPage() {
  const { id } = Route.useParams();
  const navigate = useNavigate();
  const { data, isLoading, isError } = useRole(id);

  if (isLoading) {
    return (
      <div className="flex flex-col gap-6 p-6 max-w-4xl">
        <div className="h-4 w-32 animate-pulse rounded bg-muted" />
        <div className="h-6 w-48 animate-pulse rounded bg-muted" />
      </div>
    );
  }

  if (isError || !data) {
    return (
      <div className="p-6 text-sm text-destructive">Role not found.</div>
    );
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-4xl">
      {/* Back nav */}
      <Button
        variant="ghost"
        size="sm"
        className="-ml-2 w-fit text-muted-foreground"
        onClick={() => navigate({ to: "/roles" })}
      >
        <ArrowLeft className="mr-1.5 h-4 w-4" />
        Back to Roles
      </Button>

      {/* Title */}
      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg border bg-muted">
          <ShieldCheck className="h-5 w-5 text-muted-foreground" />
        </div>
        <div>
          <h1 className="text-xl font-semibold leading-none">{data.name}</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {data.permissions.length} permission{data.permissions.length !== 1 ? "s" : ""}
          </p>
        </div>
      </div>

      <Separator />

      {/* Permissions */}
      <section className="space-y-4">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted-foreground">
          Permissions
        </h2>
        <PermissionsTable roleId={id} permissions={data.permissions} />
      </section>

      {/* Add permission */}
      <section>
        <AddPermissionForm roleId={id} />
      </section>
    </div>
  );
}
