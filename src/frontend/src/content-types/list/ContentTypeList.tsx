import { useNavigate } from "@tanstack/react-router";
import {
  flexRender,
  getCoreRowModel,
  getSortedRowModel,
  useReactTable,
  type ColumnDef,
  type SortingState,
} from "@tanstack/react-table";
import { useDebounce } from "@/hooks/use-debounce";
import React from "react";
import { useContentTypes } from "@/hooks/use-content-types";

import { ContentTypeActions } from "./ContentTypeListActions";
import { Loader2 } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Input } from "@/components/ui/input";

import { Button } from "@/components/ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { PagedContentType } from "../types";

const PAGE_SIZE = 10;

export default function ContentTypesList() {
  const navigate = useNavigate();

  const [search, setSearch] = React.useState("");
  const [statusFilter, setStatusFilter] = React.useState("all");
  const [sorting, setSorting] = React.useState<SortingState>([]);
  const [page, setPage] = React.useState(1);

  const debouncedSearch = useDebounce(search, 1000);

  // Reset to page 1 when filters change.
  React.useEffect(() => {
    setPage(1);
  }, [debouncedSearch]);

  const { data, isLoading, error, isFetching } = useContentTypes(
    page,
    PAGE_SIZE,
    sorting.length > 0
      ? `${sorting[0].id}.${sorting[0].desc ? "desc" : "asc"}`
      : undefined,
    debouncedSearch,
    statusFilter !== "all" ? statusFilter : undefined,
  );
  console.log(data);

  const columns = React.useMemo<ColumnDef<PagedContentType>[]>(
    () => [
      {
        accessorKey: "name",
        header: "Name",
      },
      {
        accessorKey: "status",
        header: "Status",
        cell: ({ row }) => {
          const status = row.getValue("status") as string;
          return (
            <span
              className={`inline-flex items-center rounded-full px-2 py-1 text-xs font-medium ${
                status === "published"
                  ? "bg-green-100 text-green-700"
                  : "bg-gray-100 text-gray-700"
              }`}
            >
              {status}
            </span>
          );
        },
      },
      {
        accessorKey: "createdAt",
        header: "Created At",
        cell: ({ row }) => {
          const value = row.getValue("createdAt") as string;
          const date = new Date(value);
          return date.toLocaleDateString(undefined, {
            year: "numeric",
            month: "short",
            day: "numeric",
            hour: "2-digit",
            minute: "2-digit",
          });
        },
      },
      {
        accessorKey: "version",
        header: "Version",
      },
      {
        id: "actions",
        header: "Actions",
        cell: ({ row }) => <ContentTypeActions contentType={row.original} />,
      },
    ],
    [],
  );

  const table = useReactTable<PagedContentType>({
    data: data?.data ?? [],
    columns,
    state: { sorting },
    onSortingChange: setSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    manualSorting: true,
    manualPagination: true,
  });

  const handleSort = (
    headerId: string,
    canSort: boolean,
    currentSort: false | "asc" | "desc",
  ) => {
    if (!canSort) return;
    const desc = currentSort === "asc";
    setSorting([{ id: headerId, desc }]);
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin text-gray-500" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-6 max-w-6xl mx-auto">
        <Alert variant="destructive">
          <AlertDescription>
            Failed to load content types. Please try again later.
          </AlertDescription>
        </Alert>
      </div>
    );
  }

  const isEmpty = !data?.data || data.data.length === 0;

  return (
    <div className="p-6 space-y-6 max-w-6xl mx-auto">
      {/* Toolbar */}
      <div className="flex items-center justify-between gap-4">
        <Input
          placeholder="Search by name..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <Select
          value={statusFilter}
          onValueChange={(value) => setStatusFilter(value)}
        >
          <SelectTrigger>
            <SelectValue placeholder="All statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectGroup>
              <SelectLabel>Status</SelectLabel>
              <SelectItem value="all">All</SelectItem>
              <SelectItem value="published">Published</SelectItem>
              <SelectItem value="draft">Draft</SelectItem>
            </SelectGroup>
          </SelectContent>
        </Select>

        <Button onClick={() => navigate({ to: "/content-types/new" })}>
          Create New
        </Button>
      </div>

      {/* Table */}
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((hg) => (
              <TableRow key={hg.id}>
                {hg.headers.map((header) => {
                  const canSort = header.column.getCanSort();
                  const sorted = header.column.getIsSorted();

                  return (
                    <TableHead
                      key={header.id}
                      className={canSort ? "cursor-pointer select-none" : ""}
                      onClick={() =>
                        handleSort(header.column.id, canSort, sorted)
                      }
                      aria-sort={
                        sorted === "asc"
                          ? "ascending"
                          : sorted === "desc"
                            ? "descending"
                            : "none"
                      }
                    >
                      <div className="flex items-center gap-2">
                        {flexRender(
                          header.column.columnDef.header,
                          header.getContext(),
                        )}
                        {sorted === "asc" && <span aria-hidden="true">↑</span>}
                        {sorted === "desc" && <span aria-hidden="true">↓</span>}
                      </div>
                    </TableHead>
                  );
                })}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {isEmpty ? (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className="h-24 text-center text-gray-500"
                >
                  {debouncedSearch || statusFilter !== "all"
                    ? "No content types found matching your filters."
                    : "No content types yet. Create your first one!"}
                </TableCell>
              </TableRow>
            ) : (
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
            )}
          </TableBody>
        </Table>
      </div>

      {/* Pagination */}
      {!isEmpty && (
        <div className="flex justify-between items-center">
          <Button
            disabled={page <= 1 || isFetching}
            onClick={() => setPage((p) => Math.max(p - 1, 1))}
          >
            Previous
          </Button>
          <span className="text-sm text-gray-700">
            Page {page} of {data?.totalPages ?? 1}
          </span>
          <Button
            disabled={isFetching || (data ? page >= data.totalPages : true)}
            onClick={() => setPage((p) => p + 1)}
          >
            Next
          </Button>
        </div>
      )}

      {/* Loading overlay */}
      {isFetching && !isLoading && (
        <div className="fixed bottom-4 right-4 bg-white rounded-lg shadow-lg p-3 flex items-center gap-2">
          <Loader2 className="h-4 w-4 animate-spin" />
          <span className="text-sm">Updating...</span>
        </div>
      )}
    </div>
  );
}
