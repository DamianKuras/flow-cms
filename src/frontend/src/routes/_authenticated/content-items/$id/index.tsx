import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { useApiClient } from "@/hooks/use-api-client";
import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  AlertCircle,
  FileX,
  ArrowLeft,
  Pencil,
  Trash2,
  Calendar,
} from "lucide-react";

export const Route = createFileRoute("/_authenticated/content-items/$id/")({
  component: RouteComponent,
});

type ContentItem = {
  id: string;
  name: string;
  contentTypeId: string;
  contentTypeName?: string;
  values?: Record<string, { value: any }>;
  createdAt?: string;
  updatedAt?: string;
  status?: string;
};

function useContentItem(id: string) {
  const api = useApiClient();

  return useQuery({
    queryKey: ["content-item", id],
    queryFn: async (): Promise<ContentItem> => {
      const response = await api.get<ContentItem>(`/content-items/${id}`);
      return response.data;
    },
    enabled: !!id,
  });
}

function useDeleteContentItem() {
  const api = useApiClient();
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/content-items/${id}`);
    },
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ["content-item", id] });
      queryClient.invalidateQueries({ queryKey: ["content"] });
      navigate({ to: "/" });
    },
  });
}

function RouteComponent() {
  const { id } = Route.useParams();
  const { data: item, error, isLoading } = useContentItem(id);
  const deleteMutation = useDeleteContentItem();

  const handleDelete = () => {
    if (
      window.confirm(
        `Are you sure you want to delete "${item?.name || "this item"}"? This action cannot be undone.`,
      )
    ) {
      deleteMutation.mutate(id);
    }
  };

  if (isLoading) {
    return (
      <div className="flex-1 space-y-6 p-6">
        <Skeleton className="h-8 w-32" />
        <div className="space-y-3">
          <Skeleton className="h-12 w-3/4" />
          <Skeleton className="h-4 w-1/2" />
        </div>
        <Skeleton className="h-96 w-full" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex-1 p-6">
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error Loading Content Item</AlertTitle>
          <AlertDescription>
            {error.message ||
              "An unexpected error occurred while loading the content item."}
          </AlertDescription>
        </Alert>
        <div className="mt-4 flex gap-2">
          <Button variant="outline" onClick={() => window.location.reload()}>
            Try Again
          </Button>
          <Button variant="outline" asChild>
            <Link to="/">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Go Back
            </Link>
          </Button>
        </div>
      </div>
    );
  }

  if (!item) {
    return (
      <div className="flex-1 p-6">
        <div className="flex flex-col items-center justify-center min-h-[400px] space-y-4">
          <FileX className="h-16 w-16 text-muted-foreground" />
          <div className="text-center space-y-2">
            <h2 className="text-2xl font-semibold">Content Item Not Found</h2>
            <p className="text-muted-foreground">
              The content item with ID <span className="font-mono">{id}</span>{" "}
              could not be found.
            </p>
          </div>
          <Button variant="outline" asChild>
            <Link to="/">
              <ArrowLeft className="mr-2 h-4 w-4" />
              Go Back
            </Link>
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 space-y-6 p-6">
      <Button variant="ghost" size="sm" asChild>
        <Link
          to={item.contentTypeId ? "/content-types/$id/items" : "/"}
          params={item.contentTypeId ? { id: item.contentTypeId } : undefined}
        >
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back to {item.contentTypeName || "Content Items"}
        </Link>
      </Button>

      <div className="space-y-4">
        <div className="flex items-start justify-between gap-4">
          <div className="space-y-2 flex-1">
            <div className="flex items-center gap-3">
              <h1 className="text-4xl font-bold">{item.name || "Untitled"}</h1>
              {item.status && <Badge variant="outline">{item.status}</Badge>}
            </div>
            <p className="text-sm text-muted-foreground">
              ID: <span className="font-mono">{item.id}</span>
            </p>
            {item.contentTypeName && (
              <p className="text-sm text-muted-foreground">
                Type: {item.contentTypeName}
              </p>
            )}
          </div>

          <div className="flex gap-2">
            <Button variant="outline" size="sm" asChild>
              <Link to="/content-items/$id/edit" params={{ id }}>
                <Pencil className="mr-2 h-4 w-4" />
                Edit
              </Link>
            </Button>
            <Button
              variant="destructive"
              size="sm"
              onClick={handleDelete}
              disabled={deleteMutation.isPending}
            >
              <Trash2 className="mr-2 h-4 w-4" />
              {deleteMutation.isPending ? "Deleting..." : "Delete"}
            </Button>
          </div>
        </div>

        {(item.createdAt || item.updatedAt) && (
          <div className="flex gap-6 text-sm text-muted-foreground">
            {item.createdAt && (
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4" />
                <span>
                  Created: {new Date(item.createdAt).toLocaleDateString()}
                </span>
              </div>
            )}
            {item.updatedAt && (
              <div className="flex items-center gap-2">
                <Calendar className="h-4 w-4" />
                <span>
                  Updated: {new Date(item.updatedAt).toLocaleDateString()}
                </span>
              </div>
            )}
          </div>
        )}
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Field Values</CardTitle>
          <CardDescription>
            All field values for this content item
          </CardDescription>
        </CardHeader>
        <CardContent>
          {!item.values || Object.keys(item.values).length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 space-y-2">
              <FileX className="h-12 w-12 text-muted-foreground" />
              <p className="text-sm text-muted-foreground">
                No field values available
              </p>
            </div>
          ) : (
            <div className="space-y-4">
              {Object.entries(item.values).map(([fieldName, fieldData]) => (
                <div key={fieldName} className="border rounded-lg p-4">
                  <div className="space-y-2">
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex-1">
                        <p className="text-sm font-semibold mb-2">
                          {fieldName}
                        </p>
                        <div className="bg-muted rounded-lg p-4">
                          {typeof fieldData.value === "string" ? (
                            <div className="whitespace-pre-wrap break-words">
                              {fieldData.value || (
                                <span className="text-muted-foreground italic">
                                  Empty
                                </span>
                              )}
                            </div>
                          ) : (
                            <pre className="text-xs overflow-x-auto">
                              {JSON.stringify(fieldData.value, null, 2)}
                            </pre>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Raw Data</CardTitle>
          <CardDescription>
            Complete content item data structure
          </CardDescription>
        </CardHeader>
        <CardContent>
          <pre className="bg-muted p-4 rounded-lg text-xs overflow-x-auto">
            {JSON.stringify(item, null, 2)}
          </pre>
        </CardContent>
      </Card>
    </div>
  );
}
