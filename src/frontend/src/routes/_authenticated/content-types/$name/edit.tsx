import { createFileRoute, Link } from "@tanstack/react-router";
import { useContentType } from "@/hooks/use-content-type";
import { useContentTypeSummaryByName } from "@/hooks/use-content-type-summaries";
import { EditContentTypeForm } from "@/content-types/edit/form";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { AlertCircle, ArrowLeft } from "lucide-react";

export const Route = createFileRoute("/_authenticated/content-types/$name/edit")({
  component: RouteComponent,
});

function RouteComponent() {
  const { name } = Route.useParams();
  const { summary, isLoading: summaryLoading } = useContentTypeSummaryByName(name);
  const draftId = summary?.draftId ?? "";
  const { data, error, isLoading: typeLoading } = useContentType(draftId);

  const isLoading = summaryLoading || (!!draftId && typeLoading);

  if (isLoading) {
    return (
      <div className="flex-1 space-y-6 p-6">
        <Skeleton className="h-8 w-32" />
        <Skeleton className="h-96 w-full" />
      </div>
    );
  }

  if (!summary?.draftId) {
    return (
      <div className="flex-1 p-6">
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>No Draft</AlertTitle>
          <AlertDescription>
            There is no draft version of "{name}" to edit.
          </AlertDescription>
        </Alert>
        <Button variant="outline" className="mt-4" asChild>
          <Link to="/content-types/$name" params={{ name }}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back
          </Link>
        </Button>
      </div>
    );
  }

  if (error) {
    return (
      <div className="flex-1 p-6">
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertTitle>Error Loading Content Type</AlertTitle>
          <AlertDescription>{error.message}</AlertDescription>
        </Alert>
        <Button variant="outline" className="mt-4" asChild>
          <Link to="/content-types/$name" params={{ name }}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back
          </Link>
        </Button>
      </div>
    );
  }

  const contentType = data?.contentType;

  if (!contentType) {
    return null;
  }

  return (
    <div className="flex-1">
      <div className="p-6 pb-0">
        <Button variant="ghost" size="sm" asChild>
          <Link to="/content-types/$name" params={{ name }}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to {name}
          </Link>
        </Button>
      </div>
      <EditContentTypeForm contentType={contentType} />
    </div>
  );
}
