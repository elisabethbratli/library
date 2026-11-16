import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api, Book } from "../api";
import { Stars } from "../components/Stars";

export function BookList() {
  const [books, setBooks] = useState<Book[]>([]);
  const [q, setQ] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    const t = setTimeout(() => api.list(q).then(setBooks).catch((e) => setError(e.message)), 200);
    return () => clearTimeout(t);
  }, [q]);

  return (
    <>
      <input className="search" placeholder="Search title or author…" value={q} onChange={(e) => setQ(e.target.value)} />
      {error && <p className="error">{error}</p>}
      {books.length === 0 && !error && <p className="muted">No books yet.</p>}
      <ul className="grid">
        {books.map((b) => (
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
