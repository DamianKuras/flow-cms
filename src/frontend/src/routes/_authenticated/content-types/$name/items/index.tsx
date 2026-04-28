import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { useContent } from "@/hooks/use-content";
import { useContentTypeSummaryByName } from "@/hooks/use-content-type-summaries";
import { createFileRoute, Link } from "@tanstack/react-router";
import { ArrowLeft, FileX, Plus } from "lucide-react";

export const Route = createFileRoute(
  "/_authenticated/content-types/$name/items/",
)({
  component: RouteComponent,
});

function RouteComponent() {
  const { name } = Route.useParams();
  const { summary, isLoading: summaryLoading } = useContentTypeSummaryByName(name);
  const publishedId = summary?.publishedId ?? "";
  const { data, error, isLoading: contentLoading } = useContent(publishedId);

  const isLoading = summaryLoading || (!!publishedId && contentLoading);

  if (isLoading) {
    return (
      <div className="flex-1 space-y-6 p-6">
        <Skeleton className="h-10 w-3/4" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!summary?.publishedId) {
    return (
      <div className="flex-1 space-y-6 p-6 max-w-4xl mx-auto">
        <Button variant="ghost" size="sm" asChild>
          <Link to="/content-types/$name" params={{ name }}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to {name}
          </Link>
        </Button>
        <Alert>
          <AlertDescription>
            Publish the schema first before creating items.
          </AlertDescription>
        </Alert>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex-1 p-6">
        <Alert variant="destructive">
          <AlertDescription>{error.message}</AlertDescription>
        </Alert>
      </div>
    );
  }

  const contentItems = data?.Content ?? [];

  return (
    <div className="flex-1 space-y-6 p-6 max-w-4xl mx-auto">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="sm" asChild>
          <Link to="/content-types/$name" params={{ name }}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to {name}
          </Link>
        </Button>
      </div>

      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">{name}</h1>
          <p className="text-sm text-muted-foreground mt-1">
            {contentItems.length} item{contentItems.length !== 1 ? "s" : ""}
          </p>
        </div>
        <Button asChild>
          <Link to="/content-types/$name/items/new" params={{ name }}>
            <Plus className="mr-2 h-4 w-4" />
            New Item
          </Link>
        </Button>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Content Items</CardTitle>
          <CardDescription>All content items of type {name}</CardDescription>
        </CardHeader>
        <CardContent>
          {contentItems.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 space-y-4">
              <div className="rounded-full bg-muted p-4">
                <FileX className="h-8 w-8 text-muted-foreground" />
              </div>
              <div className="text-center space-y-2">
                <h3 className="font-semibold text-lg">No content items yet</h3>
                <p className="text-sm text-muted-foreground">
                  Get started by creating your first content item.
                </p>
              </div>
              <Button asChild>
                <Link to="/content-types/$name/items/new" params={{ name }}>
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
                    <div className="flex items-center justify-between gap-4">
                      <h3 className="font-semibold">{item.title || "Untitled"}</h3>
                      <Badge variant="secondary" className="text-xs">View</Badge>
                    </div>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
