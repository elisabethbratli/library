import type { Book, BookDetail, BookInput, Review } from "./types";

// Same-origin by default (nginx proxies /api). Override with VITE_API_BASE for a separate API host.
const BASE = import.meta.env.VITE_API_BASE ?? "";

export class ApiError extends Error {
  constructor(message: string, readonly status: number) { super(message); }
}

// Called when a request (other than login) returns 401, i.e. the session is missing or expired.
let onUnauthorized: () => void = () => {};
export const setUnauthorizedHandler = (fn: () => void) => { onUnauthorized = fn; };

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const res = await fetch(`${BASE}/api${path}`, {
    ...init,
    credentials: "include",
    headers: { "Content-Type": "application/json" },
  });
  if (res.status === 401 && path !== "/login") onUnauthorized();
  if (!res.ok) {
    const body = await res.json().catch(() => ({}));
    throw new ApiError(body.error ?? `Request failed (${res.status})`, res.status);
  }
  return res.status === 204 ? (undefined as T) : res.json();
}

const send = (method: string, body?: unknown): RequestInit => ({
  method,
  body: body === undefined ? undefined : JSON.stringify(body),
});

export const api = {
  me: () => request<void>("/me"),
  login: (password: string) => request<void>("/login", send("POST", { password })),
  logout: () => request<void>("/logout", send("POST")),
  listBooks: (q = "") => request<Book[]>(`/books${q ? `?q=${encodeURIComponent(q)}` : ""}`),
  getBook: (id: number) => request<BookDetail>(`/books/${id}`),
  addBook: (b: BookInput) => request<Book>("/books", send("POST", b)),
  deleteBook: (id: number) => request<void>(`/books/${id}`, send("DELETE")),
  addReview: (bookId: number, rating: number, text: string) =>
    request<Review>(`/books/${bookId}/reviews`, send("POST", { rating, text })),
  deleteReview: (id: number) => request<void>(`/reviews/${id}`, send("DELETE")),
};
