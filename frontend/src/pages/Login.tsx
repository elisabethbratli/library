import { FormEvent, useState } from "react";
import { api } from "../api";

export function Login({ onLogin }: { onLogin: () => void }) {
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");

  async function submit(e: FormEvent) {
    e.preventDefault();
    try { await api.login(password); onLogin(); }
    catch (err) { setError((err as Error).message); }
  }

  return (
    <form onSubmit={submit} className="form" style={{ margin: "4rem auto" }}>
      <h2>📚 Home Library</h2>
      <label>Password
        <input type="password" autoFocus value={password} onChange={(e) => setPassword(e.target.value)} />
      </label>
      {error && <p className="error">{error}</p>}
      <button className="btn" type="submit">Log in</button>
    </form>
  );
}
