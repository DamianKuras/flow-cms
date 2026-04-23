import { useNavigate } from "@tanstack/react-router";
import { useContentTypeSummaries } from "@/hooks/use-content-type-summaries";
import { Loader2 } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

export default function ContentTypesList() {
  const navigate = useNavigate();
  const { data, isLoading, error } = useContentTypeSummaries();

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-6 max-w-6xl mx-auto">
        <Alert variant="destructive">
          <AlertDescription>Failed to load content types. Please try again later.</AlertDescription>
        </Alert>
      </div>
    );
  }

  const summaries = data ?? [];

  return (
    <div className="p-6 space-y-6 max-w-6xl mx-auto">
      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold">Content Types</h1>
        <Button onClick={() => navigate({ to: "/content-types/new" })}>
          Create New
        </Button>
      </div>

      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Name</TableHead>
              <TableHead>Published</TableHead>
              <TableHead>Draft</TableHead>
              <TableHead />
            </TableRow>
          </TableHeader>
          <TableBody>
            {summaries.length === 0 ? (
              <TableRow>
                <TableCell colSpan={4} className="h-24 text-center text-muted-foreground">
                  No content types yet. Create your first one!
                </TableCell>
              </TableRow>
            ) : (
              summaries.map((s) => (
                <TableRow key={s.name}>
                  <TableCell className="font-medium">{s.name}</TableCell>
                  <TableCell>
                    {s.publishedId ? (
                      <Badge variant="default">v{s.publishedVersion}</Badge>
                    ) : (
                      <span className="text-sm text-muted-foreground">—</span>
                    )}
                  </TableCell>
                  <TableCell>
                    {s.draftId ? (
                      <Badge variant="outline">Draft</Badge>
                    ) : (
                      <span className="text-sm text-muted-foreground">—</span>
                    )}
                  </TableCell>
                  <TableCell className="text-right">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => navigate({ to: "/content-types/$name", params: { name: s.name } })}
                    >
                      View
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
