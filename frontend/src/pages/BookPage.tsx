import { FormEvent, useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api, BookDetail } from "../api";
import { Stars } from "../components/Stars";

export function BookPage() {
  const id = Number(useParams().id);
  const nav = useNavigate();
  const [data, setData] = useState<BookDetail | null>(null);
  const [rating, setRating] = useState(5);
  const [text, setText] = useState("");
  const [error, setError] = useState("");

  const load = useCallback(() => api.get(id).then(setData).catch((e) => setError(e.message)), [id]);
  useEffect(() => { load(); }, [load]);

  async function submit(e: FormEvent) {
    e.preventDefault();
    try { await api.review(id, rating, text); setText(""); await load(); }
    catch (err) { setError((err as Error).message); }
  }

  if (error && !data) return <p className="error">{error}</p>;
  if (!data) return <p className="muted">Loading…</p>;
  const { book, reviews } = data;

  return (
    <article>
      <div className="detail">
        {book.coverUrl && <img src={book.coverUrl} alt="" />}
        <div>
          <h2>{book.title}</h2>
          <p>{book.author}{book.publishedYear ? ` · ${book.publishedYear}` : ""}{book.isbn ? ` · ISBN ${book.isbn}` : ""}</p>
          <p><Stars value={book.averageRating} /> <span className="muted">({book.reviewCount})</span></p>
          {book.description && <p>{book.description}</p>}
          <button className="btn danger" onClick={async () => {
            if (confirm("Delete this book?")) { await api.remove(id); nav("/"); }
          }}>Delete book</button>
        </div>
      </div>

      <h3>Reviews</h3>
      <form onSubmit={submit} className="form">
        <label>Rating
          <select value={rating} onChange={(e) => setRating(Number(e.target.value))}>
            {[5, 4, 3, 2, 1].map((n) => <option key={n} value={n}>{"★".repeat(n)}</option>)}
          </select>
        </label>
        <label>Review <textarea rows={3} value={text} onChange={(e) => setText(e.target.value)} /></label>
        {error && <p className="error">{error}</p>}
        <button className="btn" type="submit">Post review</button>
      </form>
      <ul className="reviews">
        {reviews.map((r) => (
          <li key={r.id}>
            <Stars value={r.rating} /> <span className="muted">{new Date(r.createdAt).toLocaleDateString()}</span>
            {r.text && <p>{r.text}</p>}
            <button className="link" onClick={async () => { await api.removeReview(r.id); load(); }}>Delete</button>
          </li>
        ))}
      </ul>
    </article>
  );
}
