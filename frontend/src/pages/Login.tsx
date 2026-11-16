import { FormEvent, useState } from "react";
import { api } from "../api/client";

export function Login({ onLogin }: { onLogin: () => void }) {
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");

  async function submit(e: FormEvent) {
    e.preventDefault();
    setError("");
    try { await api.login(password); onLogin(); }
    catch (err) { setError((err as Error).message); }
  }

  return (
    <form onSubmit={submit} className="form login">
      <h2>📚 Home Library</h2>
      <label>Password
        <input type="password" autoFocus value={password} onChange={(e) => setPassword(e.target.value)} />
      </label>
      {error && <p role="alert" className="error">{error}</p>}
      <button className="btn" type="submit">Log in</button>
    </form>
  );
}
