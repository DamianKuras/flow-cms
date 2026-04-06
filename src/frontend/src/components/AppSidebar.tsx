import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarSeparator,
} from "@/components/ui/sidebar";
import { useContentTypes } from "@/hooks/use-content-types";
import { Link } from "@tanstack/react-router";
import {
  BookOpen,
  FileText,
  LayoutDashboard,
  Layers,
  Plus,
  Shield,
  Users,
} from "lucide-react";

export function AppSidebar() {
  const { status, data, error } = useContentTypes();

  return (
    <Sidebar>
      <SidebarHeader className="border-b px-4 py-3">
        <div className="flex items-center gap-2">
          <div className="flex h-8 w-8 items-center justify-center rounded-md bg-primary text-primary-foreground">
            <BookOpen className="h-4 w-4" />
          </div>
          <span className="font-semibold tracking-tight">Flow CMS</span>
        </div>
      </SidebarHeader>

      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton asChild>
                  <Link
                    to="/"
                    className="[&.active]:bg-sidebar-accent [&.active]:font-medium"
                  >
                    <LayoutDashboard className="h-4 w-4" />
                    Dashboard
                  </Link>
                </SidebarMenuButton>
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarSeparator />

        <SidebarGroup>
          <SidebarGroupLabel className="flex items-center justify-between pr-2">
            Content Types
            <SidebarMenuButton
              asChild
              className="h-5 w-5 p-0 hover:bg-sidebar-accent rounded"
            >
              <Link to="/content-types/new" title="New content type">
                <Plus className="h-3.5 w-3.5" />
              </Link>
            </SidebarMenuButton>
          </SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {status === "pending" ? (
                <div className="space-y-1 px-2 py-1">
                  {[1, 2, 3].map((i) => (
                    <div
                      key={i}
                      className="h-8 animate-pulse rounded-md bg-sidebar-accent"
                    />
                  ))}
                </div>
              ) : status === "error" ? (
                <p className="px-2 py-1 text-xs text-destructive">
                  {error.message}
                </p>
              ) : (
                <>
                  {data.data.map((content_type) => (
                    <SidebarMenuItem key={content_type.id}>
                      <SidebarMenuButton asChild>
                        <Link
                          to="/content-types/$id/items"
                          params={{ id: content_type.id }}
                          className="[&.active]:bg-sidebar-accent [&.active]:font-medium"
                        >
                          <FileText className="h-4 w-4 shrink-0" />
                          {content_type.name}
                        </Link>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  ))}
                  <SidebarMenuItem>
                    <SidebarMenuButton asChild>
                      <Link
                        to="/content-types"
                        className="[&.active]:bg-sidebar-accent [&.active]:font-medium"
                      >
                        <Layers className="h-4 w-4 shrink-0" />
                        All content types
                      </Link>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                </>
              )}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarSeparator />

        <SidebarGroup>
          <SidebarGroupLabel>Administration</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton asChild>
                  <Link
                    to="/users"
                    className="[&.active]:bg-sidebar-accent [&.active]:font-medium"
                  >
                    <Users className="h-4 w-4" />
                    Users
                  </Link>
                </SidebarMenuButton>
              </SidebarMenuItem>
              <SidebarMenuItem>
                <SidebarMenuButton asChild>
                  <Link
                    to="/roles"
                    className="[&.active]:bg-sidebar-accent [&.active]:font-medium"
                  >
                    <Shield className="h-4 w-4" />
                    Roles
                  </Link>
                </SidebarMenuButton>
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      </SidebarContent>
    </Sidebar>
  );
}
