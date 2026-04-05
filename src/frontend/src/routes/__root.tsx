import { AppSidebar } from "@/components/AppSidebar";
import { ThemeToggle } from "@/components/ThemeToogle";
import { Button } from "@/components/ui/button";
import { SidebarProvider, SidebarTrigger } from "@/components/ui/sidebar";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import {
  createRootRouteWithContext,
  Link,
  Outlet,
} from "@tanstack/react-router";
import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";
import { useAuth, type AuthState } from "@/contexts/AuthContext";

const RootLayout = () => {
  const auth = useAuth();
  const navigate = Route.useNavigate();
  const handleLogout = async () => {
    try {
      await auth.logout();
      navigate({ to: "/login", search: { redirect: "/" } });
    } catch (error) {
      navigate({ to: "/login", search: { redirect: "/" } });
    }
  };

  return (
    <div className="w-full">
      {auth.isAuthenticated ? (
        <SidebarProvider>
          <MainLayoutWithSidebar auth={auth} handleLogout={handleLogout} />
        </SidebarProvider>
      ) : (
        <MainLayoutWithoutSidebar auth={auth} handleLogout={handleLogout} />
      )}
      <TanStackRouterDevtools />
      <ReactQueryDevtools initialIsOpen={false} />
    </div>
  );
};

function MainLayoutWithSidebar({
  auth,
  handleLogout,
}: {
  auth: AuthState;
  handleLogout: () => void;
}) {
  return (
    <>
      <AppSidebar />
      <div className="w-full">
        <Header auth={auth} handleLogout={handleLogout} />
        <main className="p-6 w-full">
          <SidebarTrigger className="text-red-50 position-absolute" />
          <Outlet />
        </main>
      </div>
    </>
  );
}

function MainLayoutWithoutSidebar({
  auth,
  handleLogout,
}: {
  auth: AuthState;
  handleLogout: () => void;
}) {
  return (
    <>
      <Header auth={auth} handleLogout={handleLogout} />
      <main className="p-6">
        <Outlet />
      </main>
    </>
  );
}

function Header({
  auth,
  handleLogout,
}: {
  auth: AuthState;
  handleLogout: () => void;
}) {
  return (
    <header className="w-full sticky top-0 bg-white dark:bg-gray-900 border-b flex justify-between items-center p-4 shadow-sm z-10">
      <div className="flex items-center gap-4">
        <h1 className="text-lg font-semibold">Flow CMS</h1>
      </div>
      <div className="flex items-center gap-4">
        {!auth.isAuthenticated && (
          <Link to="/login" search={{ redirect: "/" }}>
            Sign in
          </Link>
        )}
        {auth.isAuthenticated && (
          <Button
            className="rounded bg-red-600 px-4 py-2 text-white"
            onClick={handleLogout}
          >
            Sing out
          </Button>
        )}
        <ThemeToggle />
      </div>
    </header>
  );
}

interface MyRouterContext {
  auth: AuthState;
}

export const Route = createRootRouteWithContext<MyRouterContext>()({
  component: () => <RootLayout />,
});
