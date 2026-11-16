import { FormEvent, useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { api } from "../api/client";
import type { BookDetail } from "../api/types";
import { Stars } from "../components/Stars";

export function BookPage() {
  const id = Number(useParams().id);
  const nav = useNavigate();
  const [data, setData] = useState<BookDetail | null>(null);
  const [rating, setRating] = useState(5);
  const [text, setText] = useState("");
  const [error, setError] = useState("");

  const load = useCallback(
    () => api.getBook(id).then(setData).catch((e) => setError(e.message)), [id]);
  useEffect(() => { load(); }, [load]);

  async function run(action: () => Promise<unknown>) {
    setError("");
    try { await action(); } catch (err) { setError((err as Error).message); }
  }

  const submitReview = (e: FormEvent) => {
    e.preventDefault();
    return run(async () => { await api.addReview(id, rating, text); setText(""); await load(); });
  };

  if (!data) return error ? <p role="alert" className="error">{error}</p> : <p className="muted">Loading…</p>;
  const { book, reviews } = data;

  return (
    <article>
      <div className="detail">
        {book.coverUrl && <img src={book.coverUrl} alt="" />}
        <div>
          <h2>{book.title}</h2>
          <p>
            {book.author}
            {book.publishedYear ? ` · ${book.publishedYear}` : ""}
            {book.isbn ? ` · ISBN ${book.isbn}` : ""}
          </p>
          <p><Stars value={book.averageRating} /> <span className="muted">({book.reviewCount})</span></p>
          {book.description && <p>{book.description}</p>}
          <button className="btn danger" onClick={() => {
            if (confirm("Delete this book and its reviews?")) run(async () => { await api.deleteBook(id); nav("/"); });
          }}>Delete book</button>
        </div>
      </div>

      <h3>Reviews</h3>
      <form onSubmit={submitReview} className="form">
        <label>Rating
          <select value={rating} onChange={(e) => setRating(Number(e.target.value))}>
            {[5, 4, 3, 2, 1].map((n) => <option key={n} value={n}>{n} {n === 1 ? "star" : "stars"}</option>)}
          </select>
        </label>
        <label>Review <textarea rows={3} value={text} onChange={(e) => setText(e.target.value)} /></label>
        {error && <p role="alert" className="error">{error}</p>}
        <button className="btn" type="submit">Post review</button>
      </form>
      <ul className="reviews">
        {reviews.map((r) => (
          <li key={r.id}>
            <Stars value={r.rating} /> <span className="muted">{new Date(r.createdAt).toLocaleDateString()}</span>
            {r.text && <p>{r.text}</p>}
            <button className="link" onClick={() => run(async () => { await api.deleteReview(r.id); await load(); })}>
              Delete review
            </button>
          </li>
        ))}
      </ul>
    </article>
  );
}
