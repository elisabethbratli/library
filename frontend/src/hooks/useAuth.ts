import { useCallback, useEffect, useState } from "react";
import { api, setUnauthorizedHandler } from "../api/client";

export type AuthState = "loading" | "in" | "out";

export function useAuth() {
  const [state, setState] = useState<AuthState>("loading");

  useEffect(() => {
    setUnauthorizedHandler(() => setState("out"));
    api.me().then(() => setState("in")).catch(() => setState("out"));
  }, []);

  const loggedIn = useCallback(() => setState("in"), []);
  const logout = useCallback(async () => { await api.logout(); setState("out"); }, []);
  return { state, loggedIn, logout };
}
