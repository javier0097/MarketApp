import { ValidationError } from "./errors.ts";

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

export async function createProduct(
  request: CreateProductRequest,
): Promise<ProductResponse> {
  const response = await fetch("/api/products", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  if (response.status === 400) {
    const problem = await response.json();
    throw new ValidationError(problem.errors);
  }
  if (!response.ok) {
    throw new Error(`Create product failed with status ${response.status}`);
  }
  return response.json();
}

export async function getNextInternalCode(): Promise<NextInternalCodeResponse> {
  const response = await fetch("/api/products/next-internal-code");
  if (!response.ok) {
    throw new Error(`Next internal code failed with status ${response.status}`);
  }
  return response.json();
}
