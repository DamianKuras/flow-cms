import { Button } from "@/components/ui/button";
import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";

export const Route = createFileRoute("/_authenticated/dashboard")({
  component: DashboardComponent,
});

function DashboardComponent() {
  const { auth } = Route.useRouteContext();
  const navigate = Route.useNavigate();
  const [isLoggingOut, setIsLoggingOut] = useState(false);

  const handleLogout = async () => {
    try {
      setIsLoggingOut(true);
      await auth.logout();
      navigate({ to: "/login", search: { redirect: "/" } });
    } catch (error) {
      console.error("Logout failed:", error);
      navigate({ to: "/login", search: { redirect: "/" } });
    } finally {
      setIsLoggingOut(false);
    }
  };

  return (
    <div className="p-6">
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold">Dashboard</h1>
        <Button
          onClick={handleLogout}
          disabled={isLoggingOut}
          className="bg-red-600 text-white px-4 py-2 rounded hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {isLoggingOut ? "Signing Out..." : "Sign Out"}
        </Button>
      </div>

      <div className="bg-white p-6 rounded-lg shadow">
        <h2 className="text-xl font-semibold mb-2">Welcome back!</h2>
        <p className="text-gray-600">
          Hello, <strong>{auth.user?.username}</strong>! You are successfully
          authenticated.
        </p>
        <p className="text-sm text-gray-500 mt-2">Email: {auth.user?.email}</p>
      </div>
    </div>
  );
}
