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
import { useContent } from "@/hooks/use-content";
import { createFileRoute, Link } from "@tanstack/react-router";
import { AlertCircle, FileX, Plus, Settings } from "lucide-react";

export const Route = createFileRoute(
  "/_authenticated/content-types/$id/items/",
)({
  component: RouteComponent,
});

function RouteComponent() {
  const { id } = Route.useParams();
  const { data, error, isLoading } = useContent(id);
  if (isLoading) {
    return (
      <div className="flex-1 space-y-6 p-6">
        <div className="space-y-3">
          <Skeleton className="h-10 w-3/4" />
          <Skeleton className="h-4 w-1/2" />
        </div>
        <Skeleton className="h-64 w-full" />
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
        <div className="mt-4">
          <Button variant="outline" onClick={() => window.location.reload()}>
            Try Again
          </Button>
        </div>
      </div>
    );
  }
  if (!data?.Type) {
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
          <Button variant="outline" onClick={() => window.history.back()}>
            Go Back
          </Button>
        </div>
      </div>
    );
  }
  const contentType = data.Type;
  const contentItems = data.Content;
  return (
    <div className="flex-1 space-y-6 p-6">
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
            <Button variant="outline" asChild>
              <Link to="/content-types/$id" params={{ id }}>
                <Settings className="mr-2 h-4 w-4" />
                Manage Type
              </Link>
            </Button>
            <Button asChild>
              <Link to="/content-types/$id/items/new" params={{ id }}>
                <Plus className="mr-2 h-4 w-4" />
                New Content Item
              </Link>
            </Button>
          </div>
        </div>

        <div className="flex gap-6 text-sm">
          <div>
            <span className="font-semibold">{contentItems.length}</span>{" "}
            <span className="text-muted-foreground">
              content item{contentItems.length !== 1 ? "s" : ""}
            </span>
          </div>
        </div>
      </div>
      <Card>
        <CardHeader>
          <CardTitle>Content Items</CardTitle>
          <CardDescription>
            All content items of type {contentType.name}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {contentItems.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 space-y-4">
              <div className="rounded-full bg-muted p-4">
                <FileX className="h-8 w-8 text-muted-foreground" />
              </div>
              <div className="text-center space-y-2">
                <h3 className="font-semibold text-lg">No content items yet</h3>
                <p className="text-sm text-muted-foreground max-w-md">
                  Get started by creating your first content item for this
                  content type.
                </p>
              </div>
              <Button asChild>
                <Link to="/content-types/$id/items/new" params={{ id }}>
                  <Plus className="mr-2 h-4 w-4" />
                  Create First Item
                </Link>
              </Button>
            </div>
          ) : (
            <div className="space-y-3">
              {contentItems.map((item) => (
                <Link
                  key={item.id}
                  to="/content-items/$id"
                  params={{ id: item.id }}
                  className="block"
                >
                  <div className="border rounded-lg p-4 hover:bg-muted/50 transition-colors">
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex-1 space-y-2">
                        <h3 className="font-semibold">
                          {item.title || "Untitled"}
                        </h3>
                        {item.fields_values &&
                          Object.keys(item.fields_values).length > 0 && (
                            <div className="text-xs text-muted-foreground">
                              {Object.keys(item.fields_values).length} field
                              value
                              {Object.keys(item.fields_values).length !== 1
                                ? "s"
                                : ""}
                            </div>
                          )}
                      </div>
                      <Badge variant="secondary" className="text-xs">
                        View
                      </Badge>
                    </div>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
      {contentItems.length > 0 && contentItems[0].fields_values && (
        <Card>
          <CardHeader>
            <CardTitle>Sample Field Values</CardTitle>
            <CardDescription>
              Field values from "{contentItems[0].title || "Untitled"}"
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {Object.entries(contentItems[0].fields_values).map(
                ([key, value]) => (
                  <div key={key} className="border rounded-lg p-3">
                    <div className="flex items-start justify-between gap-4">
                      <div className="flex-1">
                        <p className="text-xs text-muted-foreground mb-1">
                          Field: <span className="font-mono">{key}</span>
                        </p>
                        <div className="bg-muted p-2 rounded text-sm font-mono">
                          {value || (
                            <span className="text-muted-foreground italic">
                              Empty
                            </span>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                ),
              )}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
