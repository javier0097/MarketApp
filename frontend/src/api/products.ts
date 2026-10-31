import { request } from "./request.ts";

export type SaveProductRequest = {
  code: string;
  name: string;
  salePrice: number | null;
};

export type ProductResponse = {
  id: number;
  code: string;
  name: string;
  salePrice: number;
  updatedAt: string;
};

type NextInternalCodeResponse = { code: string };

export function getProducts(signal?: AbortSignal): Promise<ProductResponse[]> {
  return request("/api/products", { signal });
}

export function getProduct(
  id: number,
  signal?: AbortSignal,
): Promise<ProductResponse> {
  return request(`/api/products/${id}`, { signal });
}

export function createProduct(
  product: SaveProductRequest,
): Promise<ProductResponse> {
  return request("/api/products", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(product),
  });
}

export function updateProduct(
  id: number,
  product: SaveProductRequest,
): Promise<ProductResponse> {
  return request(`/api/products/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(product),
  });
}

export function getNextInternalCode(): Promise<NextInternalCodeResponse> {
  return request("/api/products/next-internal-code");
}
