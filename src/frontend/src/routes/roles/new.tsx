import * as React from "react";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { ArrowLeft } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { useCreateRole } from "@/hooks/use-roles";

export const Route = createFileRoute("/roles/new")({
  component: NewRolePage,
});

function NewRolePage() {
  const navigate = useNavigate();
  const createRole = useCreateRole();
  const [name, setName] = React.useState("");
  const [error, setError] = React.useState("");

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError("");
    if (!name.trim()) {
      setError("Role name is required.");
      return;
    }
    createRole.mutate(
      { name: name.trim() },
      {
        onSuccess: (data) => {
          navigate({ to: `/roles/${data.id}` });
        },
        onError: () => {
          setError("Failed to create role. The name may already be taken.");
        },
      },
    );
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-lg">
      <Button
        variant="ghost"
        size="sm"
        className="-ml-2 w-fit text-muted-foreground"
        onClick={() => navigate({ to: "/roles" })}
      >
        <ArrowLeft className="mr-1.5 h-4 w-4" />
        Back to Roles
      </Button>

      <Card>
        <CardHeader>
          <CardTitle>Create Role</CardTitle>
          <CardDescription>
            Give the role a unique name. You can assign permissions after creation.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="name">Role Name</Label>
              <Input
                id="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g. Editor"
                autoFocus
              />
              {error && <p className="text-sm text-destructive">{error}</p>}
            </div>
            <div className="flex gap-2 pt-1">
              <Button type="submit" disabled={createRole.isPending}>
                {createRole.isPending ? "Creating…" : "Create Role"}
              </Button>
              <Button
                type="button"
                variant="outline"
                onClick={() => navigate({ to: "/roles" })}
              >
                Cancel
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
