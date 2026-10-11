import { request } from "./request.ts";

export type CreateProductRequest = {
  code: string;
  name: string;
  salePrice: number | null;
};

export type ProductResponse = {
  id: number;
  code: string;
  name: string;
  salePrice: number;
};

type NextInternalCodeResponse = { code: string };

export function createProduct(
  product: CreateProductRequest,
): Promise<ProductResponse> {
  return request("/api/products", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(product),
  });
}

export function getNextInternalCode(): Promise<NextInternalCodeResponse> {
  return request("/api/products/next-internal-code");
}
