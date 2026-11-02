import type { ProductResponse } from "../api/products.ts";

const savedProductKey = "savedProduct";

export function rememberSavedProduct(product: ProductResponse) {
  sessionStorage.setItem(savedProductKey, JSON.stringify(product));
}

export function readSavedProduct(): ProductResponse | null {
  const saved = sessionStorage.getItem(savedProductKey);
  return saved === null ? null : JSON.parse(saved);
}

export function clearSavedProduct() {
  sessionStorage.removeItem(savedProductKey);
}
