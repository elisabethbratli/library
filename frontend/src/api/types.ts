export interface Book {
  id: number;
  title: string;
  author: string;
  isbn?: string | null;
  publishedYear?: number | null;
  description?: string | null;
  coverUrl?: string | null;
  addedAt: string;
  averageRating: number | null;
  reviewCount: number;
}
export interface Review { id: number; rating: number; text?: string | null; createdAt: string }
export interface BookDetail { book: Book; reviews: Review[] }
export interface BookInput {
  title: string;
  author: string;
  isbn?: string | null;
  publishedYear?: number | null;
  description?: string | null;
  coverUrl?: string | null;
}
