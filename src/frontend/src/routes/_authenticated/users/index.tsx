import { createFileRoute } from "@tanstack/react-router";

import * as React from "react";
import {
  flexRender,
  getCoreRowModel,
  getPaginationRowModel,
  useReactTable,
  type Table as TanstackTable,
} from "@tanstack/react-table";

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";

import { useUsers, type PagedUser } from "@/hooks/use-users";
import { pagedUserColumns } from "@/dataTablesColumns/pagedUserColumn";

interface UsersToolbarProps {
  selectedCount: number;
  selectedIds: string[];
  table: TanstackTable<PagedUser>;
}

function UsersToolbar({
  selectedCount,
  selectedIds,
  table,
}: UsersToolbarProps) {
  const navigate = Route.useNavigate();
  return (
    <div className="flex items-center justify-between mb-4">
      {selectedCount === 0 && (
        <>
          <div className="flex gap-2">
            <Button onClick={() => navigate({ to: "/users/new" })}>
              Create User
            </Button>
            <Button variant="outline" onClick={() => table.setGlobalFilter("")}>
              Clear Filters
            </Button>
          </div>
          <div>
            <input
              type="text"
              placeholder="Search users…"
              className="border rounded p-1 text-sm"
              onChange={(e) => {
                table.setGlobalFilter(e.target.value);
              }}
            />
          </div>
        </>
      )}

      {selectedCount === 1 && (
        <div className="flex gap-2">
          <span>{selectedCount} selected</span>
          <Button
            onClick={() =>
              navigate({ to: `/users/${selectedIds[0]}/edit` })
            }
          >
            Edit User
          </Button>
        </div>
      )}

      {selectedCount > 1 && (
        <div className="flex gap-2">
          <span>{selectedCount} selected</span>
        </div>
      )}
    </div>
  );
}

export function UsersTable() {
  const [pageIndex, setPageIndex] = React.useState(0); // react-table is 0-based
  const [pageSize, setPageSize] = React.useState(10);
  const pageNumber = pageIndex + 1; // API is 1-based

  const { data, isLoading, isError } = useUsers(pageNumber, pageSize);
  const [rowSelection, setRowSelection] = React.useState({});
  const table = useReactTable({
    data: data?.pagedList ?? [],
    columns: pagedUserColumns,
    rowCount: data ? data.totalCount : -1,
    state: {
      pagination: {
        pageIndex,
        pageSize,
      },
      rowSelection,
    },
    manualPagination: true,
    onPaginationChange: (updater) => {
      const next =
        typeof updater === "function"
          ? updater({ pageIndex, pageSize })
          : updater;

      setPageIndex(next.pageIndex);
      setPageSize(next.pageSize);
    },
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getRowId: (row) => row.id,
  });

  if (isLoading) {
    return <div className="p-4">Loading users…</div>;
  }

  if (isError) {
    return <div className="p-4 text-red-600">Failed to load users.</div>;
  }
  const selectedRows = table.getSelectedRowModel().rows;
  const selectedIds = selectedRows.map((r) => r.original.id);
  return (
    <div className="space-y-4">
      <UsersToolbar
        selectedCount={selectedRows.length}
        selectedIds={selectedIds}
        table={table}
      />
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id}>
                    {header.isPlaceholder
                      ? null
                      : flexRender(
                          header.column.columnDef.header,
                          header.getContext(),
                        )}
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
                      {flexRender(
                        cell.column.columnDef.cell,
                        cell.getContext(),
                      )}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            ) : (
              <TableRow>
                <TableCell
                  colSpan={pagedUserColumns.length}
                  className="h-24 text-center"
                >
                  No users found.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      {/* Pagination controls */}
      <div className="flex items-center justify-between px-2">
        <div className="text-sm text-muted-foreground">
          Page {pageIndex + 1} of {table.getPageCount()}
        </div>
        <div className="flex items-center space-x-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => table.previousPage()}
            disabled={!table.getCanPreviousPage()}
          >
            Previous
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => table.nextPage()}
            disabled={!table.getCanNextPage()}
          >
            Next
          </Button>
        </div>
      </div>
    </div>
  );
}

export const Route = createFileRoute("/_authenticated/users/")({
  component: RouteComponent,
});

function RouteComponent() {
  return <UsersTable />;
}
