import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it } from "vitest";
import { App } from "./App";
import { AddBook } from "./pages/AddBook";
import { BookPage } from "./pages/BookPage";
import { BookList } from "./pages/BookList";
import { Route, Routes } from "react-router-dom";
import { calls, installFetchMock, route } from "./test/mockApi";

const book = (over = {}) => ({
  id: 1, title: "Dune", author: "Frank Herbert", addedAt: "2026-01-01T00:00:00Z",
  averageRating: null, reviewCount: 0, ...over,
});

const flags = { v7_startTransition: true, v7_relativeSplatPath: true } as const;

beforeEach(() => installFetchMock());

describe("login gate", () => {
  it("shows the login form when the session is missing and rejects a wrong password", async () => {
    route("GET /me", { status: 401 });
    route("POST /login", { status: 401, body: { error: "Wrong password" } });
    render(<App />);

    await userEvent.type(await screen.findByLabelText("Password"), "nope");
    await userEvent.click(screen.getByRole("button", { name: "Log in" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Wrong password");
    expect(screen.queryByText("+ Add book")).not.toBeInTheDocument();
  });

  it("logs in, shows the library, and logs out again", async () => {
    let loggedIn = false;
    route("GET /me", () => ({ status: loggedIn ? 204 : 401 }));
    route("POST /login", () => { loggedIn = true; return { status: 204 }; });
    route("POST /logout", () => { loggedIn = false; return { status: 204 }; });
    route("GET /books", { body: [book()] });
    render(<App />);

    await userEvent.type(await screen.findByLabelText("Password"), "secret");
    await userEvent.click(screen.getByRole("button", { name: "Log in" }));
    expect(await screen.findByText("Dune")).toBeInTheDocument();
    expect(calls.find((c) => c.key === "POST /login")?.body).toEqual({ password: "secret" });

    await userEvent.click(screen.getByRole("button", { name: "Log out" }));
    expect(await screen.findByLabelText("Password")).toBeInTheDocument();
  });

  it("returns to the login form when a request reports an expired session", async () => {
    route("GET /me", { status: 204 });
    route("GET /books", { status: 401 });
    render(<App />);
    expect(await screen.findByLabelText("Password")).toBeInTheDocument();
  });
});

describe("book list", () => {
  it("lists books and searches by the typed text", async () => {
    route("GET /books", (_b, url) => ({
      body: url.searchParams.get("q") === "herb" ? [book()] : [book(), book({ id: 2, title: "Emma", author: "Jane Austen" })],
    }));
    render(<MemoryRouter future={flags}><BookList /></MemoryRouter>);
    expect(await screen.findByText("Emma")).toBeInTheDocument();

    await userEvent.type(screen.getByLabelText("Search"), "herb");
    await waitFor(() => expect(screen.queryByText("Emma")).not.toBeInTheDocument());
    expect(screen.getByText("Dune")).toBeInTheDocument();
  });

  it("shows an empty state", async () => {
    route("GET /books", { body: [] });
    render(<MemoryRouter future={flags}><BookList /></MemoryRouter>);
    expect(await screen.findByText(/No books yet/)).toBeInTheDocument();
  });
});

describe("add book", () => {
  it("posts the form and opens the new book", async () => {
    route("POST /books", () => ({ status: 201, body: book({ id: 7, title: "Emma" }) }));
    route("GET /books/7", { body: { book: book({ id: 7, title: "Emma" }), reviews: [] } });
    render(
      <MemoryRouter initialEntries={["/add"]} future={flags}>
        <Routes><Route path="/add" element={<AddBook />} /><Route path="/books/:id" element={<BookPage />} /></Routes>
      </MemoryRouter>,
    );

    await userEvent.type(screen.getByLabelText(/Title/), "Emma");
    await userEvent.type(screen.getByLabelText(/Author/), "Jane Austen");
    await userEvent.type(screen.getByLabelText(/Published year/), "1815");
    await userEvent.click(screen.getByRole("button", { name: "Save" }));

    expect(await screen.findByRole("heading", { name: "Emma" })).toBeInTheDocument();
    expect(calls.find((c) => c.key === "POST /books")?.body).toMatchObject({
      title: "Emma", author: "Jane Austen", publishedYear: 1815, isbn: null,
    });
  });

  it("shows the server's validation error", async () => {
    route("POST /books", { status: 400, body: { error: "Cover URL must be an http(s) URL" } });
    render(<MemoryRouter future={flags}><AddBook /></MemoryRouter>);
    await userEvent.type(screen.getByLabelText(/Title/), "x");
    await userEvent.type(screen.getByLabelText(/Author/), "y");
    await userEvent.click(screen.getByRole("button", { name: "Save" }));
    expect(await screen.findByRole("alert")).toHaveTextContent("Cover URL must be an http(s) URL");
  });
});

describe("book page", () => {
  const renderPage = () => render(
    <MemoryRouter initialEntries={["/books/1"]} future={flags}>
      <Routes><Route path="/books/:id" element={<BookPage />} /></Routes>
    </MemoryRouter>);

  it("posts a review and reloads the book", async () => {
    let reviews: any[] = [];
    route("GET /books/1", () => ({
      body: { book: book({ reviewCount: reviews.length, averageRating: reviews.length ? 4 : null }), reviews },
    }));
    route("POST /books/1/reviews", (body) => {
      reviews = [{ id: 5, rating: body.rating, text: body.text, createdAt: "2026-01-02T00:00:00Z" }];
      return { status: 201, body: reviews[0] };
    });
    renderPage();
    expect(await screen.findByText("No reviews")).toBeInTheDocument();

    await userEvent.selectOptions(screen.getByLabelText("Rating"), "4");
    await userEvent.type(screen.getByLabelText("Review"), "Loved it");
    await userEvent.click(screen.getByRole("button", { name: "Post review" }));

    expect(await screen.findByText("Loved it")).toBeInTheDocument();
    expect(calls.find((c) => c.key === "POST /books/1/reviews")?.body).toEqual({ rating: 4, text: "Loved it" });
  });

  it("deletes a review", async () => {
    let reviews = [{ id: 5, rating: 3, text: "Meh", createdAt: "2026-01-02T00:00:00Z" }];
    route("GET /books/1", () => ({ body: { book: book(), reviews } }));
    route("DELETE /reviews/5", () => { reviews = []; return { status: 204 }; });
    renderPage();
    await userEvent.click(await screen.findByRole("button", { name: "Delete review" }));
    await waitFor(() => expect(screen.queryByText("Meh")).not.toBeInTheDocument());
  });

  it("shows an error for a missing book", async () => {
    route("GET /books/1", { status: 404, body: {} });
    renderPage();
    expect(await screen.findByRole("alert")).toHaveTextContent("Request failed (404)");
  });
});
