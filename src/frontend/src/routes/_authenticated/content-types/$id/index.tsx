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
import { useContentType } from "@/hooks/use-content-type";
import { useApiClient } from "@/hooks/use-api-client";
import { createFileRoute, Link } from "@tanstack/react-router";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  AlertCircle,
  FileX,
  ArrowLeft,
  Pencil,
  Trash2,
  List,
  Plus,
  CheckCircle2,
} from "lucide-react";

export const Route = createFileRoute("/_authenticated/content-types/$id/")({
  component: RouteComponent,
});

function useArchiveContentType() {
  const api = useApiClient();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await api.post(`/content-types/${id}/archive`);
    },
    onSuccess: (_, id) => {
      queryClient.invalidateQueries({ queryKey: ["content-type", id] });
    },
  });
}

function RouteComponent() {
  const { id } = Route.useParams();
  const { data, error, isLoading } = useContentType(id);
  const archiveMutation = useArchiveContentType();

  const handleArchive = () => {
    if (
      window.confirm(
        `Are you sure you want to archive "${data?.contentType?.name || "this content type"}"?`,
      )
    ) {
      archiveMutation.mutate(id);
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
          <AlertTitle>Error Loading Content Type</AlertTitle>
          <AlertDescription>
            {error.message ||
              "An unexpected error occurred while loading the content type."}
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

  if (!data?.contentType) {
    return (
      <div className="flex-1 p-6">
        <div className="flex flex-col items-center justify-center min-h-[400px] space-y-4">
          <FileX className="h-16 w-16 text-muted-foreground" />
          <div className="text-center space-y-2">
            <h2 className="text-2xl font-semibold">Content Type Not Found</h2>
            <p className="text-muted-foreground">
              The content type with ID <span className="font-mono">{id}</span>{" "}
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

  const contentType = data.contentType;

  return (
    <div className="flex-1 space-y-6 p-6">
      <Button variant="ghost" size="sm" asChild>
        <Link to="/content-types">
          <ArrowLeft className="mr-2 h-4 w-4" />
          Back to Content Types
        </Link>
      </Button>

      <div className="space-y-4">
        <div className="flex items-start justify-between gap-4">
          <div className="space-y-2 flex-1">
            <div className="flex items-center gap-3">
              <h1 className="text-4xl font-bold">{contentType.name}</h1>
              <Badge variant="outline">{contentType.status}</Badge>
            </div>
            <p className="text-sm text-muted-foreground">
              ID: <span className="font-mono">{contentType.id}</span> • Version:{" "}
              {contentType.version}
            </p>
          </div>

          <div className="flex gap-2">
            <Button variant="outline" size="sm" asChild>
              <Link to="/content-types/$id/items" params={{ id }}>
                <List className="mr-2 h-4 w-4" />
                View Items
              </Link>
            </Button>
            <Button variant="outline" size="sm">
              <Pencil className="mr-2 h-4 w-4" />
              Edit
            </Button>
            <Button
              variant="destructive"
              size="sm"
              onClick={handleArchive}
              disabled={archiveMutation.isPending}
            >
              <Trash2 className="mr-2 h-4 w-4" />
              {archiveMutation.isPending ? "Archiving..." : "Archive"}
            </Button>
          </div>
        </div>

        <div className="flex gap-6 text-sm">
          <div>
            <span className="font-semibold">{contentType.fields.length}</span>{" "}
            <span className="text-muted-foreground">
              field{contentType.fields.length !== 1 ? "s" : ""}
            </span>
          </div>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Quick Actions</CardTitle>
          <CardDescription>
            Manage content and content type settings
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-3">
            <Button asChild>
              <Link to="/content-types/$id/items/new" params={{ id }}>
                <Plus className="mr-2 h-4 w-4" />
                Create Content Item
              </Link>
            </Button>
            <Button variant="outline" asChild>
              <Link to="/content-types/$id/items" params={{ id }}>
                <List className="mr-2 h-4 w-4" />
                View All Items
              </Link>
            </Button>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Fields</CardTitle>
          <CardDescription>
            All fields defined for this content type
          </CardDescription>
        </CardHeader>
        <CardContent>
          {contentType.fields.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 space-y-4">
              <div className="rounded-full bg-muted p-4">
                <FileX className="h-8 w-8 text-muted-foreground" />
              </div>
              <div className="text-center space-y-2">
                <h3 className="font-semibold text-lg">No fields defined</h3>
                <p className="text-sm text-muted-foreground max-w-md">
                  This content type doesn't have any fields yet.
                </p>
              </div>
            </div>
          ) : (
            <div className="space-y-3">
              {contentType.fields.map((field) => (
                <div
                  key={field.id}
                  className="border rounded-lg p-4 hover:bg-muted/50 transition-colors"
                >
                  <div className="space-y-3">
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex-1 space-y-1">
                        <div className="flex items-center gap-2">
                          <h3 className="font-semibold">{field.name}</h3>
                          {field.isRequired && (
                            <Badge variant="secondary" className="text-xs">
                              Required
                            </Badge>
                          )}
                        </div>
                        <p className="text-xs text-muted-foreground">
                          Type: <span className="font-mono">{field.type}</span>{" "}
                          • ID: <span className="font-mono">{field.id}</span>
                        </p>
                      </div>
                    </div>

                    {field.validationRules &&
                      field.validationRules.length > 0 && (
                        <div className="space-y-2">
                          <p className="text-xs font-medium text-muted-foreground">
                            Validation Rules:
                          </p>
                          <div className="space-y-1">
                            {field.validationRules.map((rule, idx) => (
                              <div
                                key={idx}
                                className="bg-muted rounded p-2 text-xs"
                              >
                                <div className="flex items-center gap-2">
                                  <CheckCircle2 className="h-3 w-3 text-green-600" />
                                  <span className="font-mono">{rule.type}</span>
                                </div>
                                {rule.parameters &&
                                  Object.keys(rule.parameters).length > 0 && (
                                    <div className="mt-1 ml-5 text-muted-foreground">
                                      {Object.entries(rule.parameters).map(
                                        ([key, value]) => (
                                          <div key={key}>
                                            {key}:{" "}
                                            <span className="font-mono">
                                              {value}
                                            </span>
                                          </div>
                                        ),
                                      )}
                                    </div>
                                  )}
                              </div>
                            ))}
                          </div>
                        </div>
                      )}

                    {field.transformationRules &&
                      field.transformationRules.length > 0 && (
                        <div className="space-y-2">
                          <p className="text-xs font-medium text-muted-foreground">
                            Transformation Rules:
                          </p>
                          <div className="space-y-1">
                            {field.transformationRules.map((rule, idx) => (
                              <div
                                key={idx}
                                className="bg-muted rounded p-2 text-xs"
                              >
                                <div className="flex items-center gap-2">
                                  <span className="font-mono">{rule.type}</span>
                                </div>
                                {rule.parameters &&
                                  Object.keys(rule.parameters).length > 0 && (
                                    <div className="mt-1 text-muted-foreground">
                                      {Object.entries(rule.parameters).map(
                                        ([key, value]) => (
                                          <div key={key}>
                                            {key}:{" "}
                                            <span className="font-mono">
                                              {value}
                                            </span>
                                          </div>
                                        ),
                                      )}
                                    </div>
                                  )}
                              </div>
                            ))}
                          </div>
                        </div>
                      )}

                    {(!field.validationRules ||
                      field.validationRules.length === 0) &&
                      (!field.transformationRules ||
                        field.transformationRules.length === 0) && (
                        <p className="text-xs text-muted-foreground italic">
                          No validation or transformation rules
                        </p>
                      )}
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
            Complete content type data structure
          </CardDescription>
        </CardHeader>
        <CardContent>
          <pre className="bg-muted p-4 rounded-lg text-xs overflow-x-auto">
            {JSON.stringify(contentType, null, 2)}
          </pre>
        </CardContent>
      </Card>
    </div>
  );
}
