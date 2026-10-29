import { useState } from "react";
import { getToken } from "./api";
import Login from "./components/Login";
import Register from "./components/Register";
import Dashboard from "./components/Dashboard";
import Punishments from "./components/Punishments";
import Goals from "./components/Goals";

type View = "dashboard" | "punishments" | "goals";

export default function App() {
  const [authed, setAuthed] = useState<boolean>(() => getToken() !== null);
  const [view, setView] = useState<View>("dashboard");
  const [authView, setAuthView] = useState<"login" | "register">("login");

  function handleLogout() {
    setAuthed(false);
    setView("dashboard");
    setAuthView("login");
  }

  if (!authed) {
    if (authView === "register") {
      return (
        <Register
          onRegistered={() => setAuthed(true)}
          onBackToLogin={() => setAuthView("login")}
        />
      );
    }
    return <Login onLogin={() => setAuthed(true)} onRegister={() => setAuthView("register")} />;
  }

  if (view === "punishments") {
    return <Punishments onLogout={handleLogout} onBack={() => setView("dashboard")} />;
  }

  if (view === "goals") {
    return <Goals onLogout={handleLogout} onBack={() => setView("dashboard")} />;
  }

  return (
    <Dashboard
      onLogout={handleLogout}
      onManagePunishments={() => setView("punishments")}
      onManageGoals={() => setView("goals")}
    />
  );
}
