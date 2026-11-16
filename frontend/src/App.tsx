import { BrowserRouter, Link, Route, Routes } from "react-router-dom";
import { useAuth } from "./hooks/useAuth";
import { AddBook } from "./pages/AddBook";
import { BookList } from "./pages/BookList";
import { BookPage } from "./pages/BookPage";
import { Login } from "./pages/Login";

export function App() {
  const { state, loggedIn, logout } = useAuth();
  if (state === "loading") return null;
  if (state === "out") return <Login onLogin={loggedIn} />;

  return (
    <BrowserRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
      <header className="top">
        <Link to="/" className="brand">📚 Home Library</Link>
        <span>
          <Link to="/add" className="btn">+ Add book</Link>{" "}
          <button className="btn" onClick={logout}>Log out</button>
        </span>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<BookList />} />
          <Route path="/add" element={<AddBook />} />
          <Route path="/books/:id" element={<BookPage />} />
        </Routes>
      </main>
    </BrowserRouter>
  );
}
