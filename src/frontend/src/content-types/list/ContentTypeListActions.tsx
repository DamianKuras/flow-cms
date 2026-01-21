import { useNavigate } from "@tanstack/react-router";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Button } from "@/components/ui/button";
import { MoreHorizontal } from "lucide-react";
import type { PagedContentType } from "../types";
import {
  useArchiveContentType,
  usePublishContentType,
  useUnpublishContentType,
} from "@/hooks/use-content-types";
import { useQueryClient } from "@tanstack/react-query";

export function ContentTypeActions({
  contentType,
}: {
  contentType: PagedContentType;
}) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const publishMutation = usePublishContentType(() => {
    queryClient.invalidateQueries({ queryKey: ["contentTypes"] });
  });

  const unpublishMutation = useUnpublishContentType(() => {
    queryClient.invalidateQueries({ queryKey: ["contentTypes"] });
  });

  const archiveMutation = useArchiveContentType(() => {
    queryClient.invalidateQueries({ queryKey: ["contentTypes"] });
  });

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button variant="ghost" size="sm" aria-label="Open actions menu">
          <MoreHorizontal className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        <DropdownMenuItem
          onClick={() => navigate({ to: `/content-types/${contentType.id}` })}
        >
          View Details
        </DropdownMenuItem>

        <DropdownMenuItem
          onClick={() =>
            navigate({ to: `/content-types/${contentType.id}/edit` })
          }
        >
          Edit
        </DropdownMenuItem>

        {contentType.status !== "published" ? (
          <DropdownMenuItem
            onClick={() => publishMutation.mutate(contentType.id)}
            disabled={publishMutation.isPending}
          >
            Publish
          </DropdownMenuItem>
        ) : (
          <DropdownMenuItem
            onClick={() => unpublishMutation.mutate(contentType.id)}
            disabled={unpublishMutation.isPending}
          >
            Unpublish
          </DropdownMenuItem>
        )}

        <DropdownMenuItem
          onClick={() => archiveMutation.mutate(contentType.id)}
          disabled={archiveMutation.isPending}
        >
          Archive
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
