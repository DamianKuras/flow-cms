import * as React from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
  type ColumnDef,
} from "@tanstack/react-table";
import { ShieldCheck, Plus, ChevronRight, Trash2 } from "lucide-react";

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Separator } from "@/components/ui/separator";

import { useRoles, useDeleteRole, type RoleListItem } from "@/hooks/use-roles";

export const Route = createFileRoute("/_authenticated/roles/")({
  component: RolesPage,
});

function RolesPage() {
  const navigate = useNavigate();
  const { data, isLoading, isError } = useRoles();
  const deleteRole = useDeleteRole();

  const columns = React.useMemo<ColumnDef<RoleListItem>[]>(
    () => [
      {
        accessorKey: "name",
        header: "Name",
        cell: ({ row }) => (
          <button
            className="font-medium hover:underline underline-offset-2 text-foreground"
            onClick={() => navigate({ to: `/roles/${row.original.id}` })}
          >
            {row.original.name}
          </button>
        ),
      },
      {
        id: "actions",
        header: "",
        cell: ({ row }) => (
          <div className="flex items-center gap-1 justify-end">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => navigate({ to: `/roles/${row.original.id}` })}
              className="text-muted-foreground"
            >
              Manage
              <ChevronRight className="ml-1 h-3.5 w-3.5" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 text-muted-foreground hover:text-destructive"
              onClick={() => {
                if (confirm(`Delete role "${row.original.name}"?`)) {
                  deleteRole.mutate({ id: row.original.id });
                }
              }}
              disabled={deleteRole.isPending}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        ),
      },
    ],
    [navigate, deleteRole],
  );

  const table = useReactTable({
    data: data?.roles ?? [],
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  return (
    <div className="flex flex-col gap-6 p-6 max-w-3xl">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="flex h-9 w-9 items-center justify-center rounded-lg border bg-muted">
            <ShieldCheck className="h-5 w-5 text-muted-foreground" />
          </div>
          <div>
            <h1 className="text-lg font-semibold leading-none">Roles</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Manage roles and their permissions.
            </p>
          </div>
        </div>
        <Button size="sm" onClick={() => navigate({ to: "/roles/new" })}>
          <Plus className="mr-1.5 h-4 w-4" />
          New Role
        </Button>
      </div>

      <Separator />

      {/* Table */}
      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-10 w-full" />
          ))}
        </div>
      ) : isError ? (
        <p className="text-sm text-destructive">Failed to load roles.</p>
      ) : (
        <div className="rounded-lg border">
          <Table>
            <TableHeader>
              {table.getHeaderGroups().map((hg) => (
                <TableRow key={hg.id} className="hover:bg-transparent">
                  {hg.headers.map((header) => (
                    <TableHead key={header.id} className="text-xs uppercase tracking-wide">
                      {header.isPlaceholder
                        ? null
                        : flexRender(header.column.columnDef.header, header.getContext())}
                    </TableHead>
                  ))}
                </TableRow>
              ))}
            </TableHeader>
            <TableBody>
              {table.getRowModel().rows.length ? (
                table.getRowModel().rows.map((row) => (
                  <TableRow key={row.id}>
                    {row.getVisibleCells().map((cell) => (
                      <TableCell key={cell.id}>
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell colSpan={columns.length} className="h-32 text-center">
                    <div className="flex flex-col items-center gap-2 text-muted-foreground">
                      <ShieldCheck className="h-8 w-8 opacity-30" />
                      <p className="text-sm">No roles yet.</p>
                    </div>
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      )}

      {data && (
        <p className="text-xs text-muted-foreground">
          {data.roles.length} {data.roles.length === 1 ? "role" : "roles"} total
        </p>
      )}
    </div>
  );
}
