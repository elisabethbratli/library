import { vi } from "vitest";

type Handler = (body: any, url: URL) => { status?: number; body?: unknown };
const routes = new Map<string, Handler>();

/** Register a fake response for `METHOD /path` (path without the /api prefix). */
export function route(key: string, handler: Handler | { status?: number; body?: unknown }) {
  routes.set(key, typeof handler === "function" ? handler : () => handler);
}

export const calls: { key: string; body: any }[] = [];

export function installFetchMock() {
  routes.clear();
  calls.length = 0;
  vi.stubGlobal("fetch", vi.fn(async (input: string, init: RequestInit = {}) => {
    const url = new URL(input, "http://localhost");
    const key = `${init.method ?? "GET"} ${url.pathname.replace(/^\/api/, "")}`;
    const body = init.body ? JSON.parse(init.body as string) : undefined;
    calls.push({ key, body });
    const h = routes.get(key);
    if (!h) throw new Error(`Unmocked request: ${key}`);
    const { status = 200, body: resBody } = h(body, url);
    return {
      ok: status < 400,
      status,
      json: async () => resBody,
    } as Response;
  }));
}
