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
} from "@/components/ui/sidebar";
import { useContentTypes } from "@/hooks/use-content-types";
import { Link, useNavigate } from "@tanstack/react-router";

import { BookOpen } from "lucide-react";
import { Button } from "./ui/button";

const SidebarData = {
  header: {
    title: "Flow CMS",
    icon: BookOpen,
  },
};

export function AppSidebar() {
  const { status, data, error, isFetching } = useContentTypes();
  const navigate = useNavigate();
  function logout() {
    // authStore.clear();
    // navigate({ to: "/login" });
  }
  return (
    <Sidebar>
      {/* <SidebarHeader>
        <p className="flex items-center gap-2 group-data-[collapsible=icon]:justify-center">
          <BookOpen className="group-data-[collapsible=icon]:w-full" />
          <span className="group-data-[collapsible=icon]:hidden">Flow CMS</span>
        </p>
      </SidebarHeader> */}
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupLabel>Main</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              <SidebarMenuItem>
                <SidebarMenuButton asChild>
                  <Link to="/" className="[&.active]:font-bold">
                    Dashboard
                  </Link>
                </SidebarMenuButton>
              </SidebarMenuItem>
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarGroup>
          <SidebarGroupLabel>Content Types</SidebarGroupLabel>

          <SidebarGroupContent>
            <SidebarMenu>
              {status === "pending" ? (
                "Loading..."
              ) : status === "error" ? (
                <span>Error: {error.message}</span>
              ) : (
                <>
                  {data.data.map((content_type) => (
                    <SidebarMenuItem key={content_type.name}>
                      <SidebarMenuButton>
                        <Link to={`/content-types/${content_type.id}/items`}>
                          {content_type.name}
                        </Link>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  ))}
                </>
              )}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>

        <SidebarGroup>
          <SidebarMenuItem>
            <Button>
              <Link to="/content-types"> See all content types</Link>
            </Button>
          </SidebarMenuItem>
        </SidebarGroup>

        <SidebarGroup>
          <SidebarMenuItem>
            <Button>
              <Link to="/content-types/new">Add new content type.</Link>
            </Button>
          </SidebarMenuItem>
        </SidebarGroup>

        <SidebarGroup>
          <SidebarMenuItem>
            <Button>
              <Link to="/users"> See all users.</Link>
            </Button>
          </SidebarMenuItem>
        </SidebarGroup>

        <SidebarGroup>
          <SidebarMenuItem>
            <Button>
              <Link to="/users/new"> Add new user.</Link>
            </Button>
          </SidebarMenuItem>
        </SidebarGroup>
      </SidebarContent>
    </Sidebar>
  );
}
