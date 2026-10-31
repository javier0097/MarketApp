// NFD splits "á" into "a" plus a combining accent mark, which the regex then removes
export function normalizeForSearch(text: string): string {
  return text.normalize("NFD").replace(/\p{M}/gu, "").toLowerCase();
}
