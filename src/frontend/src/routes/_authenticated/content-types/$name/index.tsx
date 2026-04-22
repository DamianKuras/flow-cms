import { createFileRoute, Link } from "@tanstack/react-router";
import { useContentTypeSummaryByName } from "@/hooks/use-content-type-summaries";
import { useContentType } from "@/hooks/use-content-type";
import { usePublishContentType, type MigrationMode } from "@/hooks/use-publish-content-type";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { ArrowLeft, Pencil, List, Plus, Upload, Loader2 } from "lucide-react";
import { useState } from "react";
import type { ContentTypeDto, FieldDto } from "@/hooks/use-content-type";
import { useQueryClient } from "@tanstack/react-query";

export const Route = createFileRoute("/_authenticated/content-types/$name/")({
  component: RouteComponent,
});

function FieldList({ fields }: { fields: FieldDto[] }) {
  if (fields.length === 0) {
    return <p className="text-sm text-muted-foreground">No fields defined.</p>;
  }
  return (
    <div className="space-y-2">
      {fields.map((f) => (
        <div key={f.id} className="flex items-center gap-2 text-sm">
          <span className="font-medium">{f.name}</span>
          <Badge variant="outline" className="text-xs">{f.type}</Badge>
          {f.isRequired && <Badge variant="secondary" className="text-xs">required</Badge>}
        </div>
      ))}
    </div>
  );
}

function PublishedCard({
  ct,
  name,
}: {
  ct: ContentTypeDto;
  name: string;
}) {
  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              Published
              <Badge variant="default" className="text-xs">v{ct.version}</Badge>
            </CardTitle>
            <CardDescription>Live schema served to API consumers</CardDescription>
          </div>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" asChild>
              <Link to="/content-types/$name/items" params={{ name }}>
                <List className="mr-2 h-4 w-4" />
                Items
              </Link>
            </Button>
            <Button size="sm" asChild>
              <Link to="/content-types/$name/items/new" params={{ name }}>
                <Plus className="mr-2 h-4 w-4" />
                New Item
              </Link>
            </Button>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <FieldList fields={ct.fields} />
      </CardContent>
    </Card>
  );
}

function DraftCard({
  ct,
  name,
  publishedVersion,
}: {
  ct: ContentTypeDto;
  name: string;
  publishedVersion: number | null;
}) {
  const [publishDialogOpen, setPublishDialogOpen] = useState(false);
  const [migrationMode, setMigrationMode] = useState<MigrationMode>("Eager");
  const publishMutation = usePublishContentType(name);
  const queryClient = useQueryClient();

  const handlePublish = () => {
    publishMutation.mutate(migrationMode, {
      onSuccess: () => {
        setPublishDialogOpen(false);
        queryClient.invalidateQueries({ queryKey: ["content-type-summaries"] });
      },
    });
  };

  return (
    <>
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="flex items-center gap-2">
                Draft
                <Badge variant="outline" className="text-xs">unpublished</Badge>
              </CardTitle>
              <CardDescription>
                {publishedVersion
                  ? `Pending changes for v${publishedVersion + 1}`
                  : "Not yet published"}
              </CardDescription>
            </div>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" asChild>
                <Link to="/content-types/$name/edit" params={{ name }}>
                  <Pencil className="mr-2 h-4 w-4" />
                  Edit
                </Link>
              </Button>
              <Button size="sm" onClick={() => setPublishDialogOpen(true)}>
                <Upload className="mr-2 h-4 w-4" />
                Publish
              </Button>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <FieldList fields={ct.fields} />
        </CardContent>
      </Card>

      <Dialog open={publishDialogOpen} onOpenChange={(o) => !o && setPublishDialogOpen(false)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Publish "{name}"</DialogTitle>
            <DialogDescription>
              {publishedVersion
                ? `This will create v${publishedVersion + 1} and migrate existing content items.`
                : "This will publish the first version of this content type."}
            </DialogDescription>
          </DialogHeader>

          {publishedVersion !== null && (
            <div className="space-y-3 py-2">
              <div
                className={`border rounded-lg p-4 cursor-pointer transition-colors ${migrationMode === "Eager" ? "border-primary bg-primary/5" : "hover:bg-muted/50"}`}
                onClick={() => setMigrationMode("Eager")}
              >
                <div className="flex items-center gap-2">
                  <div className={`w-4 h-4 rounded-full border-2 ${migrationMode === "Eager" ? "border-primary bg-primary" : "border-muted-foreground"}`} />
                  <Label className="cursor-pointer font-semibold">Eager Migration</Label>
                </div>
                <p className="text-sm text-muted-foreground mt-1 ml-6">
                  All items are migrated immediately in a background job.
                </p>
              </div>
              <div
                className={`border rounded-lg p-4 cursor-pointer transition-colors ${migrationMode === "Lazy" ? "border-primary bg-primary/5" : "hover:bg-muted/50"}`}
                onClick={() => setMigrationMode("Lazy")}
              >
                <div className="flex items-center gap-2">
                  <div className={`w-4 h-4 rounded-full border-2 ${migrationMode === "Lazy" ? "border-primary bg-primary" : "border-muted-foreground"}`} />
                  <Label className="cursor-pointer font-semibold">Lazy Migration</Label>
                </div>
                <p className="text-sm text-muted-foreground mt-1 ml-6">
                  Items are migrated on-the-fly the next time they are read.
                </p>
              </div>
            </div>
          )}

          <DialogFooter>
            <Button variant="outline" onClick={() => setPublishDialogOpen(false)} disabled={publishMutation.isPending}>
              Cancel
            </Button>
            <Button onClick={handlePublish} disabled={publishMutation.isPending}>
              {publishMutation.isPending ? (
                <><Loader2 className="mr-2 h-4 w-4 animate-spin" />Publishing...</>
              ) : "Publish"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

function RouteComponent() {
  const { name } = Route.useParams();
  const { summary, isLoading: summaryLoading, error: summaryError } = useContentTypeSummaryByName(name);

  const publishedQuery = useContentType(summary?.publishedId ?? "");
  const draftQuery = useContentType(summary?.draftId ?? "");

  const isLoading = summaryLoading || publishedQuery.isLoading || draftQuery.isLoading;

  if (isLoading) {
    return (
      <div className="flex-1 space-y-6 p-6">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-48 w-full" />
        <Skeleton className="h-48 w-full" />
      </div>
    );
  }

  if (summaryError) {
    return (
      <div className="flex-1 p-6">
        <Alert variant="destructive">
          <AlertDescription>{summaryError.message}</AlertDescription>
        </Alert>
      </div>
    );
  }

  if (!summary) {
    return (
      <div className="flex-1 p-6">
        <Alert variant="destructive">
          <AlertDescription>Content type "{name}" not found.</AlertDescription>
        </Alert>
      </div>
    );
  }

  const published = publishedQuery.data?.contentType ?? null;
  const draft = draftQuery.data?.contentType ?? null;

  return (
    <div className="flex-1 space-y-6 p-6 max-w-4xl mx-auto">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="sm" asChild>
          <Link to="/content-types">
            <ArrowLeft className="mr-2 h-4 w-4" />
            All Types
          </Link>
        </Button>
      </div>

      <h1 className="text-3xl font-bold">{name}</h1>

      {published && (
        <PublishedCard ct={published} name={name} />
      )}

      {!published && !draft && (
        <Alert>
          <AlertDescription>No versions found for this content type.</AlertDescription>
        </Alert>
      )}

      {draft && (
        <DraftCard
          ct={draft}
          name={name}
          publishedVersion={summary.publishedVersion ?? null}
        />
      )}

      {!published && !draft && null}
    </div>
  );
}
