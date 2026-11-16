export interface Book {
  id: number; title: string; author: string; isbn?: string | null; publishedYear?: number | null;
  description?: string | null; coverUrl?: string | null; addedAt: string;
  averageRating: number | null; reviewCount: number;
}
export interface Review { id: number; rating: number; text?: string | null; createdAt: string }
export interface BookDetail { book: Book; reviews: Review[] }
export type BookInput = Pick<Book, "title" | "author"> &
  Partial<Pick<Book, "isbn" | "publishedYear" | "description" | "coverUrl">>;

// API_BASE is empty by default (same origin via nginx /api proxy). Override with VITE_API_BASE.
const BASE = import.meta.env.VITE_API_BASE ?? "";

// Called when any request returns 401 (session missing/expired)
let onUnauthorized: () => void = () => {};
export const setUnauthorizedHandler = (fn: () => void) => { onUnauthorized = fn; };

async function req<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}/api${path}`, {
    ...init,
    headers: { "Content-Type": "application/json" },
    credentials: "include",
  });
  if (res.status === 401 && path !== "/login") onUnauthorized();
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new Error(body.error ?? `Request failed (${res.status})`);
  }
  return res.status === 204 ? (undefined as T) : res.json();
}

export const api = {
  me: () => req<void>("/me"),
  login: (password: string) => req<void>("/login", { method: "POST", body: JSON.stringify({ password }) }),
  logout: () => req<void>("/logout", { method: "POST" }),
  list: (q = "") => req<Book[]>(`/books${q ? `?q=${encodeURIComponent(q)}` : ""}`),
  get: (id: number) => req<BookDetail>(`/books/${id}`),
  add: (b: BookInput) => req<Book>("/books", { method: "POST", body: JSON.stringify(b) }),
  remove: (id: number) => req<void>(`/books/${id}`, { method: "DELETE" }),
  review: (id: number, rating: number, text: string) =>
    req<Review>(`/books/${id}/reviews`, { method: "POST", body: JSON.stringify({ rating, text }) }),
  removeReview: (id: number) => req<void>(`/reviews/${id}`, { method: "DELETE" }),
};
