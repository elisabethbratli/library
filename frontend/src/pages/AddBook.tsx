import { FormEvent, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api";

export function AddBook() {
  const nav = useNavigate();
  const [f, setF] = useState({ title: "", author: "", isbn: "", year: "", description: "", coverUrl: "" });
  const [error, setError] = useState("");
  const set = (k: keyof typeof f) => (e: { target: { value: string } }) => setF({ ...f, [k]: e.target.value });

  async function submit(e: FormEvent) {
    e.preventDefault();
    try {
      const b = await api.add({
        title: f.title, author: f.author, isbn: f.isbn || null,
        publishedYear: f.year ? Number(f.year) : null,
        description: f.description || null, coverUrl: f.coverUrl || null,
      });
      nav(`/books/${b.id}`);
    } catch (err) {
      setError((err as Error).message);
    }
  }

  return (
    <form onSubmit={submit} className="form">
      <h2>Add a book</h2>
      <label>Title* <input required value={f.title} onChange={set("title")} /></label>
      <label>Author* <input required value={f.author} onChange={set("author")} /></label>
      <label>ISBN <input value={f.isbn} onChange={set("isbn")} /></label>
      <label>Published year <input type="number" value={f.year} onChange={set("year")} /></label>
      <label>Cover image URL <input type="url" value={f.coverUrl} onChange={set("coverUrl")} /></label>
      <label>Description <textarea rows={4} value={f.description} onChange={set("description")} /></label>
      {error && <p className="error">{error}</p>}
      <button className="btn" type="submit">Save</button>
    </form>
  );
}
