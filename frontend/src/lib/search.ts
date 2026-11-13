// NFD splits "á" into "a" plus a combining accent mark, which the regex then removes
export function normalizeForSearch(text: string): string {
  return text.normalize("NFD").replace(/\p{M}/gu, "").toLowerCase();
}

export function toSearchWords(search: string): string[] {
  return normalizeForSearch(search).split(/\s+/).filter(Boolean);
}

export function findProductByCode<T extends { code: string }>(
  products: T[] | null,
  text: string,
): T | null {
  const code = text.trim().toUpperCase();
  return products?.find((product) => product.code === code) ?? null;
}

export function matchesSearchWords(
  product: { name: string; code: string },
  words: string[],
): boolean {
  const name = normalizeForSearch(product.name);
  const code = normalizeForSearch(product.code);
  return words.every((word) => name.includes(word) || code.includes(word));
}
