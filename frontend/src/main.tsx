import React, { useEffect, useState } from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter, Link, Route, Routes } from "react-router-dom";
import { api, setUnauthorizedHandler } from "./api";
import { BookList } from "./pages/BookList";
import { AddBook } from "./pages/AddBook";
import { BookPage } from "./pages/BookPage";
import { Login } from "./pages/Login";
import "./styles.css";

function App() {
  const [auth, setAuth] = useState<"loading" | "in" | "out">("loading");

  useEffect(() => {
    setUnauthorizedHandler(() => setAuth("out"));
    api.me().then(() => setAuth("in")).catch(() => setAuth("out"));
  }, []);

  if (auth === "loading") return null;
  if (auth === "out") return <Login onLogin={() => setAuth("in")} />;

  return (
    <BrowserRouter>
      <header className="top">
        <Link to="/" className="brand">📚 Home Library</Link>
        <span>
          <Link to="/add" className="btn">+ Add book</Link>{" "}
          <button className="btn" onClick={() => api.logout().then(() => setAuth("out"))}>Log out</button>
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

ReactDOM.createRoot(document.getElementById("root")!).render(<React.StrictMode><App /></React.StrictMode>);
