import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ProductResponse } from "../api/products.ts";
import {
  clearFromSession,
  readFromSession,
  rememberInSession,
  savedProductKey,
} from "./session.ts";

const STORAGE_KEY = "savedProduct";

const rice: ProductResponse = {
  id: 7,
  code: "7501234567890",
  name: "Arroz grano largo 1 kg",
  salePrice: 1250,
  updatedAt: "2026-10-15T14:30:00Z",
};

const sugar: ProductResponse = {
  id: 8,
  code: "7509876543210",
  name: "Azúcar blanca 1 kg",
  salePrice: 899,
  updatedAt: "2026-10-16T09:05:00Z",
};

function createFakeStorage() {
  const items = new Map<string, string>();

  return {
    get length() {
      return items.size;
    },
    clear: () => items.clear(),
    getItem: (key: string) => items.get(key) ?? null,
    key: (index: number) => [...items.keys()][index] ?? null,
    removeItem: (key: string) => {
      items.delete(key);
    },
    setItem: (key: string, value: string) => {
      items.set(key, String(value));
    },
  };
}

let storage: ReturnType<typeof createFakeStorage>;

// The tests run in node, which has no sessionStorage. The fake is reachable both
// as sessionStorage and as window.sessionStorage, so the tests do not depend on
// which of the two the code uses.
beforeEach(() => {
  storage = createFakeStorage();
  vi.stubGlobal("sessionStorage", storage);
  vi.stubGlobal("window", { sessionStorage: storage });
});

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("rememberInSession", () => {
  it("stores the complete product under the savedProduct key", () => {
    rememberInSession(savedProductKey, rice);

    const stored = storage.getItem(STORAGE_KEY);

    expect(JSON.parse(stored ?? "null")).toEqual(rice);
  });

  it("replaces the product that was already stored", () => {
    rememberInSession(savedProductKey, rice);

    rememberInSession(savedProductKey, sugar);

    const stored = storage.getItem(STORAGE_KEY);
    expect(JSON.parse(stored ?? "null")).toEqual(sugar);
  });
});

describe("readFromSession", () => {
  it("returns null when no product is stored", () => {
    const product = readFromSession<ProductResponse>(savedProductKey);

    expect(product).toBeNull();
  });

  it("returns null when the storage only holds other keys", () => {
    storage.setItem("otherKey", JSON.stringify(rice));

    const product = readFromSession<ProductResponse>(savedProductKey);

    expect(product).toBeNull();
  });

  it("returns the product stored under the savedProduct key", () => {
    storage.setItem(STORAGE_KEY, JSON.stringify(rice));

    const product = readFromSession<ProductResponse>(savedProductKey);

    expect(product).toEqual(rice);
  });
});

describe("clearFromSession", () => {
  it("removes the product stored under the savedProduct key", () => {
    storage.setItem(STORAGE_KEY, JSON.stringify(rice));

    clearFromSession(savedProductKey);

    expect(storage.getItem(STORAGE_KEY)).toBeNull();
  });

  it("keeps the other keys of the storage", () => {
    storage.setItem(STORAGE_KEY, JSON.stringify(rice));
    storage.setItem("otherKey", "otherValue");

    clearFromSession(savedProductKey);

    expect(storage.getItem("otherKey")).toBe("otherValue");
  });

  it("does not fail when no product is stored", () => {
    expect(() => clearFromSession(savedProductKey)).not.toThrow();
  });
});

describe("session functions together", () => {
  it("reads back the last product remembered", () => {
    rememberInSession(savedProductKey, rice);
    rememberInSession(savedProductKey, sugar);

    const product = readFromSession<ProductResponse>(savedProductKey);

    expect(product).toEqual(sugar);
  });

  it("reads null after the remembered product is cleared", () => {
    rememberInSession(savedProductKey, rice);

    clearFromSession(savedProductKey);

    expect(readFromSession<ProductResponse>(savedProductKey)).toBeNull();
  });

  it.each([
    { label: "a sale price of zero", change: { salePrice: 0 } },
    { label: "the smallest sale price", change: { salePrice: 1 } },
    {
      label: "quotes and accents in the name",
      change: { name: 'Café "Ñandú" 250 g' },
    },
    { label: "a code with leading zeros", change: { code: "0012345678905" } },
    {
      label: "the exact text of updatedAt",
      change: { updatedAt: "2026-10-15T14:30:00.1234567+00:00" },
    },
  ])("reads back the product unchanged with $label", ({ change }) => {
    const saved: ProductResponse = { ...rice, ...change };

    rememberInSession(savedProductKey, saved);

    expect(readFromSession<ProductResponse>(savedProductKey)).toEqual(saved);
  });
});
