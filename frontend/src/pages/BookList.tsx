import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api/client";
import type { Book } from "../api/types";
import { Stars } from "../components/Stars";

export function BookList() {
  const [books, setBooks] = useState<Book[] | null>(null);
  const [q, setQ] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    let current = true; // ignore responses that arrive after a newer search
    const t = setTimeout(() => {
      api.listBooks(q)
        .then((b) => { if (current) { setBooks(b); setError(""); } })
        .catch((e) => { if (current) setError(e.message); });
    }, 200);
    return () => { current = false; clearTimeout(t); };
  }, [q]);

  return (
    <>
      <input className="search" type="search" placeholder="Search title or author…" aria-label="Search"
        value={q} onChange={(e) => setQ(e.target.value)} />
      {error && <p role="alert" className="error">{error}</p>}
      {books?.length === 0 && <p className="muted">{q ? "No matching books." : "No books yet. Add your first one!"}</p>}
      <ul className="grid">
        {books?.map((b) => (
          <li key={b.id}>
            <Link to={`/books/${b.id}`} className="card">
              {b.coverUrl ? <img src={b.coverUrl} alt="" /> : <div className="cover-ph">📖</div>}
              <strong>{b.title}</strong>
              <span className="muted">{b.author}</span>
              <Stars value={b.averageRating} />
            </Link>
          </li>
        ))}
      </ul>
    </>
  );
}
