import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter, Link, Route, Routes } from "react-router-dom";
import { BookList } from "./pages/BookList";
import { AddBook } from "./pages/AddBook";
import { BookPage } from "./pages/BookPage";
import "./styles.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <BrowserRouter>
      <header className="top">
        <Link to="/" className="brand">📚 Home Library</Link>
        <Link to="/add" className="btn">+ Add book</Link>
      </header>
      <main>
        <Routes>
          <Route path="/" element={<BookList />} />
          <Route path="/add" element={<AddBook />} />
          <Route path="/books/:id" element={<BookPage />} />
        </Routes>
      </main>
    </BrowserRouter>
  </React.StrictMode>,
);
